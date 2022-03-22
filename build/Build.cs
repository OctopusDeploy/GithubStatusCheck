using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.OctoVersion;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[CheckBuildProjectConfigurations]
[ShutdownDotNetAfterServerBuild]
class Build : NukeBuild
{
    const string CiBranchNameEnvVariable = "OCTOVERSION_CurrentBranch";

    public static int Main () => Execute<Build>(x => x.Default);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution Solution;

    [Parameter("Whether to auto-detect the branch name - this is okay for a local build, but should not be used under CI.")]
    readonly bool AutoDetectBranch = IsLocalBuild;

    [Parameter("Branch name for OctoVersion to use to calculate the version number. Can be set via the environment variable " + CiBranchNameEnvVariable + ".", Name = CiBranchNameEnvVariable)]
    string BranchName { get; set; }

    [OctoVersion(BranchParameter = nameof(BranchName), AutoDetectBranchParameter = nameof(AutoDetectBranch), Framework = "net6.0")]
    public OctoVersionInfo OctoVersionInfo;

    AbsolutePath SourceDirectory => RootDirectory / "source";
    AbsolutePath PublishDirectory => RootDirectory / "publish";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    Target Clean => _ => _
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj", "**/TestResults").ForEach(DeleteDirectory);
            EnsureCleanDirectory(PublishDirectory);
            EnsureCleanDirectory(ArtifactsDirectory);
        });

    Target Restore => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetVersion(OctoVersionInfo.FullSemVer)
                .EnableNoRestore());
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .AddLoggers("trx")
                .SetTestAdapterPath(SourceDirectory / "GitHubStatusChecksWebApp.Tests" / "bin" / Configuration.ToString() / "net6.0")
                .EnableNoBuild()
                .EnableNoRestore());
        });

    Target Publish => _ => _
        .DependsOn(Test)
        .Executes(() =>
        {
            DotNetPublish(s => s
                .SetProject(SourceDirectory / "GitHubStatusChecksWebApp")
                .SetConfiguration(Configuration)
                .SetOutput(PublishDirectory / "Octopus.GithubStatusChecks")
                .EnableNoRestore()
                .EnableNoBuild()
                .SetVersion(OctoVersionInfo.FullSemVer));
        });

    Target ZipPackages => _ => _
        .DependsOn(Publish)
        .Executes(() =>
        {
            var webAppPackage = ArtifactsDirectory / $"Octopus.GithubStatusChecks.Web.{OctoVersionInfo.FullSemVer}.zip";
            CompressionTasks.Compress(PublishDirectory / "Octopus.GithubStatusChecks", webAppPackage);

            var terraformPackage =
                ArtifactsDirectory / $"Octopus.GithubStatusChecks.Terraform.{OctoVersionInfo.FullSemVer}.zip";
            CompressionTasks.Compress(RootDirectory / "terraform", terraformPackage);
        });

    Target Default => _ => _
        .DependsOn(ZipPackages);
}
