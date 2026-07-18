using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using MarketLine.Data;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Required for session support on a single Render web service
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

// Render sits behind a secure HTTPS proxy.
// These settings allow ASP.NET Core to correctly understand
// the original HTTPS request before UseHttpsRedirection runs.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;

    // Render proxy addresses are not known in advance.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// Render runs the app behind a proxy.
// This must be before HTTPS redirection.
if (!app.Environment.IsDevelopment())
{
    app.UseForwardedHeaders();
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

// Welcome page is the landing page
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Welcome}/{action=Index}/{id?}");

app.Run();