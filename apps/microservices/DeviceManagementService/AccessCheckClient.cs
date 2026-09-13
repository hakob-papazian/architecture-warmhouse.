using System.Net.Http.Json;

namespace DeviceManagementService;

public record AccessCheckResponse(bool HasAccess);

public class AccessCheckClient(HttpClient httpClient)
{
    public async Task<bool> HasAccessAsync(Guid userId, Guid houseId, CancellationToken ct)
    {
        var response = await httpClient.GetAsync($"/internal/access?userId={userId}&houseId={houseId}", ct);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var result = await response.Content.ReadFromJsonAsync<AccessCheckResponse>(cancellationToken: ct);
        return result?.HasAccess ?? false;
    }
}
