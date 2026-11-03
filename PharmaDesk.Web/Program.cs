using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using PharmaDesk.Application.Common.Interfaces;
using PharmaDesk.Infrastructure.Identity;
using PharmaDesk.Infrastructure.Persistence.Extensions;
using PharmaDesk.Web.Security;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true)
        .Build())
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services));

    builder.Services.AddControllersWithViews();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();
    builder.Services.AddScoped<ICurrentTenantContext, CurrentTenantContext>();
    builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.IsProduction());

    builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
    builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
    builder.Services.AddAuthorization();

    // Defense-in-depth beyond Identity's per-account lockout: this limits how many
    // login attempts the endpoint itself accepts per IP, regardless of which
    // username each attempt targets.
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddFixedWindowLimiter("login", limiterOptions =>
        {
            limiterOptions.Window = TimeSpan.FromMinutes(1);
            limiterOptions.PermitLimit = 10;
            limiterOptions.QueueLimit = 0;
        });
    });

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseSerilogRequestLogging();

    app.Use(async (context, next) =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        await next();
    });

    app.UseHttpsRedirection();
    app.UseRouting();

    app.UseRateLimiter();

    app.UseAuthentication();

    app.Use(async (context, next) =>
    {
        if (context.User.Identity?.IsAuthenticated == true &&
            context.User.HasClaim("MustChangePassword", "true"))
        {
            var path = context.Request.Path;
            var isAllowedWhileMustChangePassword =
                path.StartsWithSegments("/Account") ||
                path.StartsWithSegments("/css") ||
                path.StartsWithSegments("/js") ||
                path.StartsWithSegments("/lib");

            if (!isAllowedWhileMustChangePassword)
            {
                context.Response.Redirect("/Account/ChangePassword");
                return;
            }
        }

        await next();
    });

    app.UseAuthorization();

    app.MapStaticAssets();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
        .WithStaticAssets();

    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
            await seeder.SeedDefaultAdminUserAsync();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Identity seeding skipped — database may not be reachable yet.");
        }
    }

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "PharmaDesk.Web terminated unexpectedly during startup.");
}
finally
{
    Log.CloseAndFlush();
}
