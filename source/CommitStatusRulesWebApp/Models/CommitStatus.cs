namespace CommitStatusRulesWebApp.Models
{
    public class CommitStatus
    {
        public string State { get; set; }
        public string Context { get; set; }
        // ReSharper disable once InconsistentNaming
        public string Target_Url { get; set; }
        public string Description { get; set; }
    }
}