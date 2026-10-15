using ProfitabilityWeb.Services;
var b=WebApplication.CreateBuilder(args);
b.Services.AddControllersWithViews();
b.Services.AddHttpClient<ICrmReferenceClient, CrmReferenceClient>(c => c.BaseAddress=new Uri(b.Configuration["CrmApi:BaseUrl"]!));
b.Services.AddHttpClient<IManagementApiClient, ManagementApiClient>(c => c.BaseAddress=new Uri(b.Configuration["Api:BaseUrl"]!));
b.Services.AddHttpClient("api",c=>c.BaseAddress=new Uri(b.Configuration["Api:BaseUrl"]!));
b.Services.AddHttpClient("crm",c=>c.BaseAddress=new Uri(b.Configuration["CrmApi:BaseUrl"]!));
var app=b.Build();app.UseStaticFiles();app.UseRouting();app.MapControllerRoute(name:"default",pattern:"{controller=Home}/{action=Index}/{id?}");app.Run();
