using ProfitabilityWeb.Models;
namespace ProfitabilityWeb.Services;
public interface ICrmReferenceClient
{
    Task<string> GetAsync(string entity, CancellationToken ct);
    Task<IReadOnlyList<CrmOption>> GetOptionsAsync(string entity, CancellationToken ct);
}
