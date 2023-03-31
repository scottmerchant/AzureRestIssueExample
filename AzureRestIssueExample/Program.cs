using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using Newtonsoft.Json;

namespace AzureRestIssueExample;

public class Program
{
    private static string AustraliaEastEndpoint = "https://australiaeast.management.azure.com/";
    private static string AustraliaSouthEastEndpoint = "https://australiasoutheast.web.management.azure.com/";
    public static async Task Main()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        Console.WriteLine("Starting Test");

        var account = new ServicePrincipalAccount(
            Environment.GetEnvironmentVariable("TEST_AZURE_CLIENT_ID"),
            Environment.GetEnvironmentVariable("TEST_AZURE_TENANT_ID"),
            Environment.GetEnvironmentVariable("TEST_AZURE_SUBSCRIPTION_ID"),
            Environment.GetEnvironmentVariable("TEST_AZURE_CLIENT_PASSWORD"));

        Console.WriteLine("Authorising");
        var authToken = await RestClientAuth.Authorise(account, cancellationTokenSource.Token);

        var client = GetAuthorisedHttpClient(authToken);

        var resourcesPath = $"subscriptions/{account.SubscriptionNumber}/resources";

        var properties = new Dictionary<string, string>
        {
            { "api-version", "2022-09-01" },
            { "$filter", "resourceType eq 'Microsoft.web/sites' or resourceType eq 'Microsoft.web/sites/slots'" }
        };

        resourcesPath = AddQueryString(resourcesPath, properties);

        Console.WriteLine("Getting results from AustraliaEast");
        var east = await client.GetStringAsync($"{AustraliaEastEndpoint}{resourcesPath}", cancellationTokenSource.Token);

        var eastResources = JsonConvert.DeserializeObject<AzureResourceCollection>(east);

        Console.WriteLine("Getting results from AustraliaSouthEast");
        var southEast = await client.GetStringAsync($"{AustraliaSouthEastEndpoint}{resourcesPath}", cancellationTokenSource.Token);

        var southEastResources = JsonConvert.DeserializeObject<AzureResourceCollection>(southEast).Resources
                                            .ToDictionary(r => r.Id, r => r);

        foreach (var eastResource in eastResources.Resources)
        {
            var southEastResource = southEastResources[eastResource.Id];

            if (eastResource.Tags == null ||
                southEastResource.Tags == null)
                continue;

            if (eastResource.Tags.Any(
                t => !southEastResource.Tags.TryGetValue(t.Key, out var value) || value != t.Value))
            {
                Console.WriteLine("Not all tags match");
                foreach (var (key, value) in southEastResource.Tags)
                {
                    var eValue =
                        eastResource.Tags.TryGetValue(key, out var v) ? v : "<NULL>";
                    var mismatchString = eValue != value ? "#MISMATCH# " : "";
                    Console.WriteLine($"{mismatchString}Key: '{key}' SouthValue: '{value}' EastValue: '{eValue}'");
                }
            }
        }
    }

    private static string AddQueryString(string uri, IDictionary<string, string> properties)
    {
        var queryIndex = uri.IndexOf('?');
        var hasQuery = queryIndex != -1;

        var sb = new StringBuilder();
        sb.Append(uri);
        foreach (var parameter in properties)
        {
            sb.Append(hasQuery ? '&' : '?');
            sb.Append(UrlEncoder.Default.Encode(parameter.Key));
            sb.Append('=');
            sb.Append(UrlEncoder.Default.Encode(parameter.Value));
            hasQuery = true;
        }

        return sb.ToString();
    }

    private static HttpClient GetAuthorisedHttpClient(AzureADToken authToken)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(authToken.TokenType, authToken.AccessToken);
        return client;
    }
}