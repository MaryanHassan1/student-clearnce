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

// In production, if DefaultConnection is empty/whitespace, try Railway PostgreSQL variables
if (string.IsNullOrWhiteSpace(connectionString) && !builder.Environment.IsDevelopment())
{
    var postgresHost = builder.Configuration["PGHOST"];
    var postgresDatabase = builder.Configuration["PGDATABASE"];
    var postgresUsername = builder.Configuration["PGUSER"];
    var postgresPassword = builder.Configuration["PGPASSWORD"];
    var postgresPortValue = builder.Configuration["PGPORT"];

    var missingVars = new List<string>();
    if (string.IsNullOrWhiteSpace(postgresHost)) missingVars.Add("PGHOST");
    if (string.IsNullOrWhiteSpace(postgresDatabase)) missingVars.Add("PGDATABASE");
    if (string.IsNullOrWhiteSpace(postgresUsername)) missingVars.Add("PGUSER");
    if (string.IsNullOrWhiteSpace(postgresPassword)) missingVars.Add("PGPASSWORD");
    if (string.IsNullOrWhiteSpace(postgresPortValue)) missingVars.Add("PGPORT");

    if (missingVars.Count > 0)
    {
        throw new InvalidOperationException(
            $"Missing required Railway PostgreSQL environment variables: {string.Join(", ", missingVars)}. " +
            "Either configure ConnectionStrings:DefaultConnection or ensure Railway PostgreSQL is linked to this service.");
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

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "No database connection configured. Set ConnectionStrings:DefaultConnection (for manual config) " +
        "or link a PostgreSQL database in Railway.");
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
