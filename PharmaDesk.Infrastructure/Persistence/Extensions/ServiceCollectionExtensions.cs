using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PharmaDesk.Application.Common.Interfaces;
using PharmaDesk.Infrastructure.Identity;
using PharmaDesk.Infrastructure.Services;

namespace PharmaDesk.Infrastructure.Persistence.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, bool isProduction = false)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<PharmaDeskDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IDatabaseHealthService, DatabaseHealthService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IBackupService, BackupService>();
        services.AddScoped<IdentitySeeder>();

        services.Configure<BackupSettings>(configuration.GetSection("BackupSettings"));
        services.AddHostedService<BackupBackgroundService>();

        services
            .AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;

                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

                options.User.RequireUniqueEmail = false;
            })
            .AddEntityFrameworkStores<PharmaDeskDbContext>()
            .AddClaimsPrincipalFactory<ApplicationUserClaimsPrincipalFactory>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;

            // Always-https-only in Production; left as the request's own scheme in
            // other environments so the auth cookie still works before TLS is set
            // up on a fresh install being tested over plain HTTP.
            options.Cookie.SecurePolicy = isProduction ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
        });

        return services;
    }
}
