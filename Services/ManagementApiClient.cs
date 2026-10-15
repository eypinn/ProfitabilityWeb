namespace ProfitabilityWeb.Services;
public sealed class ManagementApiClient(HttpClient client):IManagementApiClient {
 public Task<HttpResponseMessage> GetAsync(string entity,CancellationToken ct) => client.GetAsync("api/"+entity,ct);
 public Task<HttpResponseMessage> SaveAsync(string entity,HttpContent body,CancellationToken ct) => client.PostAsync("api/"+entity,body,ct);
}
