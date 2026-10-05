using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using QuestPDF.Infrastructure;
using StudentClearanceSystem.Web.Data;
using StudentClearanceSystem.Web.Data.Seed;
using StudentClearanceSystem.Web.Models;
using StudentClearanceSystem.Web.Services;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

var postgresHost = builder.Configuration["PGHOST"];
if (!builder.Environment.IsDevelopment() && IsLoopbackHost(postgresHost))
{
    throw new InvalidOperationException(
        "Railway PGHOST points to localhost. Link the Railway PostgreSQL service and use its remote PGHOST value.");
}

if (!string.IsNullOrWhiteSpace(postgresHost))
{
    var postgresDatabase = builder.Configuration["PGDATABASE"];
    var postgresUsername = builder.Configuration["PGUSER"];
    var postgresPassword = builder.Configuration["PGPASSWORD"];
    var postgresPortValue = builder.Configuration["PGPORT"];

    var missingVars = new List<string>();
    if (string.IsNullOrWhiteSpace(postgresDatabase)) missingVars.Add("PGDATABASE");
    if (string.IsNullOrWhiteSpace(postgresUsername)) missingVars.Add("PGUSER");
    if (string.IsNullOrWhiteSpace(postgresPassword)) missingVars.Add("PGPASSWORD");
    if (string.IsNullOrWhiteSpace(postgresPortValue)) missingVars.Add("PGPORT");

    if (missingVars.Count > 0)
    {
        throw new InvalidOperationException(
            $"Railway PostgreSQL is partially configured. Missing variables: {string.Join(", ", missingVars)}.");
    }

    if (!int.TryParse(postgresPortValue, out var postgresPort))
    {
        throw new InvalidOperationException("Railway variable PGPORT must be a valid port number.");
    }

    connectionString = new NpgsqlConnectionStringBuilder
    {
        Host = postgresHost,
        Port = postgresPort,
        Database = postgresDatabase,
        Username = postgresUsername,
        Password = postgresPassword,
        // Railway's private network (*.railway.internal) does not need TLS; the public proxy supports it.
        SslMode = SslMode.Prefer
    }.ConnectionString;
}
else
{
    // Railway also exposes DATABASE_URL, and users often paste postgresql:// URLs, which Npgsql cannot parse.
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        connectionString = builder.Configuration["DATABASE_URL"];
    }

    connectionString = ConvertPostgresUrl(connectionString);
}

if (!string.IsNullOrWhiteSpace(connectionString) && !builder.Environment.IsDevelopment())
{
    var configuredHost = new NpgsqlConnectionStringBuilder(connectionString).Host;
    if (IsLoopbackHost(configuredHost))
    {
        throw new InvalidOperationException(
            "The production database connection points to localhost. Link the Railway PostgreSQL service " +
            "to provide PGHOST, PGPORT, PGDATABASE, PGUSER, and PGPASSWORD, or configure a remote " +
            "ConnectionStrings:DefaultConnection.");
    }
}

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "No database connection configured. In production, link a PostgreSQL database in Railway or set " +
        "ConnectionStrings:DefaultConnection. For development, configure ConnectionStrings:DefaultConnection.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireDigit = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IReportExportService, ReportExportService>();

// Railway terminates TLS at its proxy and forwards plain HTTP to the container.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try
    {
        await DbInitializer.SeedAsync(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        app.Logger.LogCritical(ex, "Database migration/seeding failed. Check the Railway PostgreSQL variables.");
        throw;
    }
}

app.UseForwardedHeaders();

// Lightweight endpoint for the Railway healthcheck ("/" redirects to the login page).
app.MapGet("/health", () => Results.Ok("Healthy"));

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static bool IsLoopbackHost(string? host) =>
    !string.IsNullOrWhiteSpace(host) &&
    (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
     (IPAddress.TryParse(host, out var address) && IPAddress.IsLoopback(address)));

static string? ConvertPostgresUrl(string? value)
{
    if (string.IsNullOrWhiteSpace(value) ||
        !(value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
          value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)))
    {
        return value;
    }

    var uri = new Uri(value);
    var userInfo = uri.UserInfo.Split(':', 2);
    return new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.IsDefaultPort || uri.Port <= 0 ? 5432 : uri.Port,
        Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
        Username = Uri.UnescapeDataString(userInfo[0]),
        Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null,
        SslMode = SslMode.Prefer
    }.ConnectionString;
}
