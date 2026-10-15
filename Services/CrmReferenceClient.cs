using System.Text.Json;
using ProfitabilityWeb.Models;
namespace ProfitabilityWeb.Services;
public sealed class CrmReferenceClient(HttpClient client) : ICrmReferenceClient
{
    public async Task<string> GetAsync(string entity, CancellationToken ct)
    {
        using var response = await client.GetAsync("api/" + entity, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }
    public async Task<IReadOnlyList<CrmOption>> GetOptionsAsync(string entity, CancellationToken ct)
    {
        var json = await GetAsync(entity, ct);
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Array) throw new JsonException("CRM liste yanıtı bekleniyor.");
        var options = new List<CrmOption>();
        foreach (var row in document.RootElement.EnumerateArray())
        {
            string Read(string name) => row.TryGetProperty(name, out var v) ? v.ToString() : "";
            var code = entity switch
            {
                "segments" => Read("segmentCode"), "portfolios" => Read("portfolioCode"),
                "customers" => Read("customerCode"), "employees" => Read("employeeCode"),
                _ => throw new ArgumentOutOfRangeException(nameof(entity))
            };
            var name = entity switch
            {
                "segments" => Read("segmentName"),
                "portfolios" => $"Şube {Read("branchId")} / Çalışan {Read("employeeCode")}",
                "customers" => $"{Read("firstName")} {Read("lastName")}",
                "employees" => $"{Read("firstName")} {Read("lastName")}",
                _ => ""
            };
            if (!string.IsNullOrWhiteSpace(code)) options.Add(new CrmOption(code, $"{name} ({code})"));
        }
        return options;
    }
}
