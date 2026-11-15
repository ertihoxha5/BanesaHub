using BanesaHub.Web.Inventory;

namespace BanesaHub.Web;

public sealed class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddControllersWithViews();
        builder.Services.AddAntiforgery(options => options.Cookie.SecurePolicy = CookieSecurePolicy.Always);

        var apiAddress = new Uri(builder.Configuration["BanesaHubApi:BaseUrl"] ?? "https://localhost:7041/");
        if (apiAddress.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("BanesaHubApi:BaseUrl must use HTTPS.");
        }

        builder.Services.AddHttpClient<InventoryApi>(client => client.BaseAddress = apiAddress)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false
            });

        var app = builder.Build();
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.MapControllers();
        app.Run();
    }
}
