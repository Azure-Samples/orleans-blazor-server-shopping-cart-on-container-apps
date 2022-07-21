// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

namespace Orleans.ShoppingCart.Silo;

public sealed class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMudServices();

        services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApp(
            options =>
            {
                _configuration.Bind("AzureAdB2C", options);

                static void ConfigCookies(params CookieBuilder[] cookies)
                {
                    foreach (var cookie in cookies)
                    {
                        cookie.SameSite = SameSiteMode.None;
                        cookie.SecurePolicy = CookieSecurePolicy.Always;
                        cookie.IsEssential = true;
                    }
                }

                ConfigCookies(options.NonceCookie, options.CorrelationCookie);
            },
            subscribeToOpenIdConnectMiddlewareDiagnosticsEvents: true);

        services.AddControllersWithViews()
            .AddMicrosoftIdentityUI();

        services.AddRazorPages();
        services.AddServerSideBlazor()
            .AddMicrosoftIdentityConsentHandler();

        services.AddHttpContextAccessor();
        services.AddSingleton<ShoppingCartService>();
        services.AddSingleton<InventoryService>();
        services.AddSingleton<ProductService>();
        services.AddScoped<ComponentStateChangedObserver>();
        services.AddSingleton<ToastService>();
        services.AddLocalStorageServices();
        services.AddApplicationInsights("Silo");
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseForwardedHeaders();
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseRewriter(new RewriteOptions().Add(context =>
        {
            if (context.HttpContext.Request.Path == "/MicrosoftIdentity/Account/SignedOut")
            {
                var host = context.HttpContext.Request.Host;
                var url = $"{context.HttpContext.Request.Scheme}://{host}";
                context.HttpContext.Response.Redirect(url);
            }
        }));
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapBlazorHub();
            endpoints.MapFallbackToPage("/_Host");
        });
    }
}