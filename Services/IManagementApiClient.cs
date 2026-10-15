namespace ProfitabilityWeb.Services;
public interface IManagementApiClient {
 Task<HttpResponseMessage> GetAsync(string entity,CancellationToken ct);
 Task<HttpResponseMessage> SaveAsync(string entity,HttpContent body,CancellationToken ct);
}
