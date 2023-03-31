namespace AzureRestIssueExample
{
    public class NotAuthorisedException : Exception
    {
        public NotAuthorisedException(string message) : base(message)
        {
        }
    }
}