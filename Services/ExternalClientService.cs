

using Newtonsoft.Json;

namespace OkCoin.API.Services;

public class ExternalClientService : IExternalClientService
{
    private readonly HttpClient _httpClient;

    public ExternalClientService(HttpClient httpClient)
    {
        _httpClient = httpClient;   
    }

    public async Task<T> GetDataAsync<T>(string endpoint)
    {
        var response = await _httpClient.GetAsync(endpoint);
        
        response.EnsureSuccessStatusCode();

        // Read the response
        var contentStream = await response.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<T>(contentStream);
    }
}