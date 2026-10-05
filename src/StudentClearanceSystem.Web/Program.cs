using System.Net;
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
        SslMode = SslMode.Require
    }.ConnectionString;
}
else if (!builder.Environment.IsDevelopment() && !string.IsNullOrWhiteSpace(connectionString))
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

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await DbInitializer.SeedAsync(scope.ServiceProvider);
}

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
