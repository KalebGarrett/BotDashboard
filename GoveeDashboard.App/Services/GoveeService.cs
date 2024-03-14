using System.Text.Json;
using GoveeDashboard.App.Secrets;
using GoveeDashboard.Models;

namespace GoveeDashboard.App.Services;

public class GoveeService
{
    private readonly HttpClient _httpClient;

    public GoveeService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Device>> Get()
    {
        _httpClient.DefaultRequestHeaders.Add("Govee-API-Key", Govee.ApiKey);
        var result = await _httpClient.GetAsync("https://openapi.api.govee.com/router/api/v1/user/devices");
        if (!result.IsSuccessStatusCode) return new List<Device>();
        var json = await result.Content.ReadAsStringAsync();
        var content = JsonSerializer.Deserialize<List<Device>>(json);
        Console.WriteLine(content);
        return content;
    }
}