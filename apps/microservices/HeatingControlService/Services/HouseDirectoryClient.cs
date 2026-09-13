namespace HeatingControlService.Services;

// Confirms a houseId is real before this service creates a heating profile for
// it - backs the 404 responses documented in docs/api/heating-control-openapi.yaml.
// Only called once per house (when no profile exists yet), not on every request.
public class HouseDirectoryClient(HttpClient httpClient)
{
    public async Task<bool> HouseExistsAsync(Guid houseId, CancellationToken ct)
    {
        var response = await httpClient.GetAsync($"/internal/houses/{houseId}/owner", ct);
        return response.IsSuccessStatusCode;
    }
}
