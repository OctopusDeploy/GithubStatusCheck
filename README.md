# GitHub Status Checks

This is the glue between TeamCity and GitHub that allows us to run different chains based on what files we've committed (eg. Only run the frontend chain if you have changes only for the frontend) and still be able to use [GitHub Branch Protection Status Checks](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/defining-the-mergeability-of-pull-requests/managing-a-branch-protection-rule).

## How to build

The build configuration for Github Status Checks can be found in [TeamCity](https://build.octopushq.com/admin/editBuild.html?id=buildType:OctopusDeploy_LIbraries_CommitStatusRules) and consists of 3 build steps:
1. **Build the solution**: Github Status Checks is built using nuke with predominately 6 targets:
   1. **Clean**: Cleans `**/bin`, `**/obj`, `**/TestResults`, `/publish` & `/artifacts` directories of any resources from previous builds to ensure clean working directories for a fresh new build.
   2. **Restore**: Uses NuGet to restore dependencies as well as project-specific tools that are specified in the project file.
   3. **Compile**: Builds the project and its dependencies.
   4. **Test**: Executes the test adapter tests in the `GitHubStatusChecksWebApp.Tests` project
   5. **Publish**: Compiles the application, reads through its dependencies specified in the project file, and publishes the resulting set of files to the `publish` directory as `Octopus.GithubStatusChecks.<semver>`
   6. **ZipPackages**: Creates compressed `.zip` of the published files to the `artifacts` directory as `Octopus.GithubStatusChecks.Web.<semver>.zip` and of the terraform project files as `Octopus.GithubStatusChecks.Terraform.<semver>.zip`
   The final Default target runs the entire nuke target chain.
2. **Push the Packages to Octopus Deploy**: Pushes all artifacts created in the `artifacts` directory to OctopusDeploy in preparation for a release deployment
3. **Create a release in Octopus Deploy**: Triggers a release creation of the Github Status Checks deploy project and is explained further in [How to Deploy](how-to-deploy)

## How to Deploy

The Github Status Checks project uses the process steps:
1. **Create Infrastructure**: Using the `Octopus.GithubStatusChecks.Terraform.<semver>.zip` package ensures the infrastructure required for a successful deployment is up and running via terraform apply.
   1. The files that were packaged in `Octopus.GithubStatusChecks.Terraform.<semvers>.zip` are located in the `./terraform` directory of this repository
2. **Create Targets in Octopus**: Creates the Azure Web App Deployment target in Octopus Deploy 
3. **Deploy Website**: Using the `Octopus.GithubStatusChecks.Web.<semver>.zip` package, deploys the azure web app service with variables defined in the configuration file `appsettings.json`.

## What uses this tool?

This tool is only used only for the OctopusDeploy repository with the implementation hard coded specifically for that repository.
