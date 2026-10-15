namespace ProfitabilityWeb.Services;
public sealed class CrmReferenceClient(HttpClient client):ICrmReferenceClient {
 public async Task<string> GetAsync(string entity,CancellationToken ct) {
  using var response=await client.GetAsync("api/"+entity,ct);
  response.EnsureSuccessStatusCode();return await response.Content.ReadAsStringAsync(ct);
 }
}
