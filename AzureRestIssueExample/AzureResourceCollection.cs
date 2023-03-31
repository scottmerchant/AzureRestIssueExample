using Newtonsoft.Json;

namespace AzureRestIssueExample
{
    public class AzureResourceCollection
    {
        [JsonProperty("value")]
        public AzureResource[] Resources { get; set; }
    }
}