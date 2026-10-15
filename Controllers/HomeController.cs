using Microsoft.AspNetCore.Mvc;
using ProfitabilityWeb.Models;
namespace ProfitabilityWeb.Controllers;
public sealed class HomeController : Controller
{
    public IActionResult Index() => View(new DashboardViewModel
    {
        ApplicationName = "Karlılık Yönetimi",
        Pages =
        [
            ("/manage/products", "Ürün Tanımları", "products kayıtlarını görüntüle ve yönet"),
            ("/manage/subproducts", "Alt Ürün Tanımları", "subproducts kayıtlarını görüntüle ve yönet"),
            ("/manage/productsegments", "Ürün–Segment Eşleştirmeleri", "productsegments kayıtlarını görüntüle ve yönet"),
            ("/manage/transactions", "İşlem Kayıtları", "transactions kayıtlarını görüntüle ve yönet"),
            ("/manage/transactionexceptions", "İstisna İşlemleri", "transactionexceptions kayıtlarını görüntüle ve yönet"),
            ("/reference", "CRM Referansları", "CRM API kaynaklarını incele")
        ]
    });
}
