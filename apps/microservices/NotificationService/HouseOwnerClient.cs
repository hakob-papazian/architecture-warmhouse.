using System.Net.Http.Json;

namespace NotificationService;

public record HouseOwnerResponse(Guid UserId);

public class HouseOwnerClient(HttpClient httpClient)
{
    public async Task<Guid?> GetOwnerAsync(Guid houseId, CancellationToken ct)
    {
        var response = await httpClient.GetAsync($"/internal/houses/{houseId}/owner", ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<HouseOwnerResponse>(cancellationToken: ct);
        return result?.UserId;
    }
}
