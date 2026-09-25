using AutoTracker.Data;
using AutoTracker.Filters;
using AutoTracker.Models;
using AutoTracker.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<SessionAuthorizeFilter>();
    options.Filters.Add<TechnicianJobAccessFilter>();
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(12);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "AutoTracker.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Home/Index";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.EventsType = typeof(AutoTrackerCookieEvents);
    });
builder.Services.AddAuthorization();

builder.Services.AddScoped<CompanySettingService>();
builder.Services.AddScoped<CompletionService>();
builder.Services.AddScoped<RequirementService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserSessionService>();
builder.Services.AddScoped<JobAccessService>();
builder.Services.AddScoped<AutoTrackerCookieEvents>();
builder.Services.AddScoped<RfidService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<ChecklistTemplateService>();
builder.Services.AddScoped<VehicleDamageTemplateService>();
builder.Services.AddScoped<QuoteCalculationService>();
builder.Services.AddScoped<PartCalculationService>();
builder.Services.AddScoped<DatabaseMaintenanceService>();
builder.Services.AddScoped<JobFinanceService>();
builder.Services.AddScoped<WhatsappNotificationService>();
builder.Services.AddScoped<ClientTrackingService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var maintenance = scope.ServiceProvider.GetRequiredService<DatabaseMaintenanceService>();
    maintenance.EnsureUpdated();

    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!db.Companies.Any())
    {
        db.Companies.Add(new Company { Name = "Default Company", IsActive = true, CreatedAt = DateTime.Now });
        db.SaveChanges();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/uploads"))
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var segments = context.Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries) ?? [];
        if (context.User.IsInRole("Technician"))
        {
            if (segments.Length < 2)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }
            if (!string.Equals(segments[1], "company", StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(Uri.UnescapeDataString(segments[1]), out var repairJobId))
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }
                var access = context.RequestServices.GetRequiredService<JobAccessService>();
                if (!await access.CanAccessAsync(context.User, repairJobId))
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }
            }
        }
    }
    await next();
});
app.UseStaticFiles();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public partial class Program { }
