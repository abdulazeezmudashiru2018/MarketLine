using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using MarketLine.Data;

// Enable legacy DateTime behavior for PostgreSQL compatibility
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------
// Render provides the port via the PORT environment variable
// ---------------------------------------------------------
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// MVC
builder.Services.AddControllersWithViews();

// Bind Cloudinary settings
builder.Services.Configure<MarketLine.Models.CloudinarySettings>(
    builder.Configuration.GetSection("CloudinarySettings"));

// Register Cloudinary Photos Service
builder.Services.AddScoped<MarketLine.Services.IPhotoService, MarketLine.Services.PhotoService>();

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

// ---------------------------------------------------------
// Get database connection string from config OR environment
// ---------------------------------------------------------
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("DATABASE_URL");

if (string.IsNullOrWhiteSpace(connectionString)
    || connectionString.Contains("SET_VIA_USER_SECRETS", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException(
        "No database connection string found. " +
        "Set 'ConnectionStrings:DefaultConnection' in user-secrets (dev) " +
        "or DATABASE_URL environment variable (production).");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Required for session support on a single Render web service
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

// Render sits behind a secure HTTPS proxy.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;

    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// ---------------------------------------------------------
// AUTO-APPLY MIGRATIONS ON STARTUP
// This means Render will create/update your tables automatically
// ---------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
        Console.WriteLine("✅ Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Migration failed: {ex.Message}");
        throw;
    }
}

// Render runs the app behind a proxy — must be BEFORE HTTPS redirection
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