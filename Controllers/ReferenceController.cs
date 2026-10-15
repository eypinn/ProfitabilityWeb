using Microsoft.AspNetCore.Mvc;
using ProfitabilityWeb.Models;
using ProfitabilityWeb.Services;
namespace ProfitabilityWeb.Controllers;
public sealed class ReferenceController(ICrmReferenceClient crm) : Controller
{
    [HttpGet("/reference")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var results = new Dictionary<string, string>();
        foreach (var entity in new[] { "segments", "portfolios", "employees", "customers" })
        {
            try { results[entity] = await crm.GetAsync(entity, ct); }
            catch (HttpRequestException ex) { results[entity] = $"CRM API hatası: {ex.Message}"; }
        }
        return View(new ReferencePageViewModel { Responses = results });
    }
}
