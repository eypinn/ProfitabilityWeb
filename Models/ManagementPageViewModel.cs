namespace ProfitabilityWeb.Models;
public sealed class ManagementPageViewModel
{
    public required string Entity { get; init; }
    public required string Title { get; init; }
    public required IReadOnlyList<string> Fields { get; init; }
    public IReadOnlyList<Dictionary<string, string>> Rows { get; init; } = [];
    public Dictionary<string, string> FormValues { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public string? Error { get; init; }
    public IReadOnlyDictionary<string, IReadOnlyList<CrmOption>> CrmOptions { get; init; } = new Dictionary<string, IReadOnlyList<CrmOption>>();
    public bool Saved { get; init; }
}
