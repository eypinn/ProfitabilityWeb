using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ProfitabilityWeb.Models;
using ProfitabilityWeb.Services;

namespace ProfitabilityWeb.Controllers;

[Route("manage")]
public sealed class ManageController(IManagementApiClient api, ICrmReferenceClient crm) : Controller
{
    private static readonly Dictionary<string, string[]> FieldMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["products"] = ["ProductCode", "ProductName", "Multiplier"],
        ["subproducts"] = ["ProductCode", "SubProductCode", "SubProductName", "Multiplier"],
        ["productsegments"] = ["ProductCode", "SegmentCode"],
        ["transactions"] = ["TransactionId", "BranchId", "TransactionCode", "PortfolioCode", "CustomerCode", "ProductCode", "SubProductCode", "TransactionDate", "YPProfit", "TLProfit", "CurrencyCode"],
        ["transactionexceptions"] = ["TransactionId", "OverrideYPProfit", "OverrideTLProfit", "Reason"],
    };
    private static readonly Dictionary<string, string> Titles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["products"] = "Ürün Tanımları",
        ["subproducts"] = "Alt Ürün Tanımları",
        ["productsegments"] = "Ürün–Segment Eşleştirmeleri",
        ["transactions"] = "İşlem Kayıtları",
        ["transactionexceptions"] = "İstisna İşlemleri",
    };

    [HttpGet("{entity}")] public async Task<IActionResult> Index(string entity, CancellationToken ct)
    {
        if (!FieldMap.TryGetValue(entity, out var fields)) return NotFound();
        var (rows, error) = await LoadAsync(entity, ct);
        return View(new ManagementPageViewModel
        {
            Entity = entity, Title = Titles[entity], Fields = fields,
            Rows = rows, Error = error, CrmOptions = await GetCrmOptionsAsync(entity, ct), Saved = TempData["Saved"] is true
        });
    }

    [HttpPost("{entity}/save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(string entity, CancellationToken ct)
    {
        if (!FieldMap.TryGetValue(entity, out var fields)) return NotFound();
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var body = new Dictionary<string, object?>();
        foreach (var field in fields)
        {
            var value = Request.Form[field].ToString().Trim();
            values[field] = value;
            if (string.IsNullOrWhiteSpace(value)) { body[field] = null; continue; }
            if (field.EndsWith("Date", StringComparison.OrdinalIgnoreCase))
            {
                if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                    return await InvalidAsync(entity, fields, values, $"{field} için geçerli tarih giriniz.", ct);
                body[field] = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
            else if (field is "TransactionId" or "BranchId" or "CustomerCode")
            {
                if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
                    return await InvalidAsync(entity, fields, values, $"{field} tam sayı olmalıdır.", ct);
                body[field] = number;
            }
            else if (field is "Multiplier" or "YPProfit" or "TLProfit" or "OverrideYPProfit" or "OverrideTLProfit")
            {
                if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
                    return await InvalidAsync(entity, fields, values, $"{field} sayısal olmalıdır (ondalık için nokta kullanın).", ct);
                body[field] = number;
            }
            else body[field] = value;
        }
        if (entity == "productsegments" && !await IsValidCrmValueAsync("segments", values.GetValueOrDefault("SegmentCode"), ct))
            return await InvalidAsync(entity, fields, values, "CRM üzerinde tanımlı bir segment seçilmelidir.", ct);
        if (entity == "transactions")
        {
            if (!await IsValidCrmValueAsync("portfolios", values.GetValueOrDefault("PortfolioCode"), ct))
                return await InvalidAsync(entity, fields, values, "CRM üzerinde tanımlı bir portföy seçilmelidir.", ct);
            if (!await IsValidCrmValueAsync("customers", values.GetValueOrDefault("CustomerCode"), ct))
                return await InvalidAsync(entity, fields, values, "CRM üzerinde tanımlı bir müşteri seçilmelidir.", ct);
        }
        // TransactionId is database-generated for new transactions.
        if (entity.Equals("transactions", StringComparison.OrdinalIgnoreCase) &&
            (!values.TryGetValue("TransactionId", out var id) || string.IsNullOrWhiteSpace(id)))
            body.Remove("TransactionId");
        try
        {
            using var content = JsonContent.Create(body);
            using var response = await api.SaveAsync(entity, content, ct);
            if (response.IsSuccessStatusCode)
            {
                TempData["Saved"] = true;
                return RedirectToAction(nameof(Index), new { entity });
            }
            return await InvalidAsync(entity, fields, values,
                $"API {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync(ct)}", ct);
        }
        catch (HttpRequestException ex)
        {
            return await InvalidAsync(entity, fields, values, $"API bağlantı hatası: {ex.Message}", ct);
        }
    }

    private async Task<IActionResult> InvalidAsync(string entity, string[] fields,
        Dictionary<string, string> values, string error, CancellationToken ct)
    {
        var (rows, _) = await LoadAsync(entity, ct);
        Response.StatusCode = 400;
        return View("Index", new ManagementPageViewModel
        { Entity = entity, Title = Titles[entity], Fields = fields,
           Rows = rows, FormValues = values, CrmOptions = await GetCrmOptionsAsync(entity, ct), Error = error });
    }

    private async Task<bool> IsValidCrmValueAsync(string entity, string? code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        try { return (await crm.GetOptionsAsync(entity, ct)).Any(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase)); }
        catch (Exception ex) when (ex is HttpRequestException or JsonException) { return false; }
    }

    private async Task<IReadOnlyDictionary<string, IReadOnlyList<CrmOption>>> GetCrmOptionsAsync(string entity, CancellationToken ct)
    {
        var entities = entity switch
        {
            "productsegments" => new[] { "segments" },
            "transactions" => new[] { "portfolios", "customers", "employees" },
            "transactionexceptions" => new[] { "portfolios", "customers", "employees", "segments" },
            _ => Array.Empty<string>()
        };
        var result = new Dictionary<string, IReadOnlyList<CrmOption>>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in entities)
        {
            try { result[name] = await crm.GetOptionsAsync(name, ct); }
            catch (Exception ex) when (ex is HttpRequestException or JsonException)
            { result[name] = []; }
        }
        return result;
    }

    private async Task<(List<Dictionary<string, string>> Rows, string? Error)> LoadAsync(string entity, CancellationToken ct)
    {
        try
        {
            using var response = await api.GetAsync(entity, ct);
            if (!response.IsSuccessStatusCode)
                return ([], $"API {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync(ct)}");
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var result = new List<Dictionary<string, string>>();
            if (document.RootElement.ValueKind != JsonValueKind.Array)
                return ([], "API beklenen liste biçiminde yanıt vermedi.");
            foreach (var item in document.RootElement.EnumerateArray())
            {
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var property in item.EnumerateObject())
                    row[property.Name] = property.Value.ValueKind is JsonValueKind.Null ? "" : property.Value.ToString();
                result.Add(row);
            }
            return (result, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            return ([], $"Veriler alınamadı: {ex.Message}");
        }
    }
}
