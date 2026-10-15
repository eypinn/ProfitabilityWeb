namespace ProfitabilityWeb.Services;
public interface ICrmReferenceClient { Task<string> GetAsync(string entity,CancellationToken ct); }
