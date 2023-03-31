using Newtonsoft.Json;

namespace AzureRestIssueExample;

public class RestClientAuth
{
    private const string ActiveDirectoryEndpoint = @"https://login.windows.net/";
    private const string AuthorisationResource = "https://management.azure.com/";

    public static async Task<AzureADToken?> Authorise(ServicePrincipalAccount account, CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "client_id", account.ClientId },
            { "client_secret", account.Password },
            { "resource", AuthorisationResource }
        };
        var requestBody = new FormUrlEncodedContent(parameters);
        var baseUri = ActiveDirectoryEndpoint;

        var requestUri = $"{baseUri}{account.TenantId}/oauth2/token";

        using var client = new HttpClient();
        var response = await client.PostAsync(requestUri, requestBody, cancellationToken);

        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonConvert.DeserializeObject<AzureADToken>(responseContent);
    }
}