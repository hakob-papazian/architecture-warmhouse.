using System.Net.Http.Json;

namespace SmartHome.Api.Services;

public class TemperatureService : ITemperatureService
{
    private readonly HttpClient _httpClient;

    public TemperatureService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<TemperatureResponse> GetTemperatureAsync(string location, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"/temperature?location={Uri.EscapeDataString(location)}", ct);
        return await ReadTemperatureResponseAsync(response, ct);
    }

    public async Task<TemperatureResponse> GetTemperatureByIdAsync(string sensorId, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"/temperature/{Uri.EscapeDataString(sensorId)}", ct);
        return await ReadTemperatureResponseAsync(response, ct);
    }

    private static async Task<TemperatureResponse> ReadTemperatureResponseAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"unexpected status code: {(int)response.StatusCode}");
        }

        var result = await response.Content.ReadFromJsonAsync<TemperatureResponse>(cancellationToken: ct);
        if (result is null)
        {
            throw new HttpRequestException("error decoding temperature response: empty body");
        }

        return result;
    }
}
