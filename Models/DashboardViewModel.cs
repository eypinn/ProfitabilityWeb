namespace ProfitabilityWeb.Models;
public sealed class DashboardViewModel
{
    public required string ApplicationName { get; init; }
    public required IReadOnlyList<(string Path, string Title, string Description)> Pages { get; init; }
}
