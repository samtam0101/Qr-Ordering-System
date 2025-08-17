using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.Features; 

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpClient();
builder.Services.Configure<FormOptions>(o =>                
{
    o.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50 MB
});

builder.Services.AddScoped(sp =>
{
    var nav = sp.GetRequiredService<NavigationManager>();
    return new HttpClient { BaseAddress = new Uri(nav.BaseUri) };
});


builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default"))); // make sure "Default" exists in appsettings.json

// Cookie auth
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/login";
        o.AccessDeniedPath = "/login";
        o.ExpireTimeSpan = TimeSpan.FromDays(7);
    });
builder.Services.AddAuthorization();

var app = builder.Build();

try
{
    await using var scope = app.Services.CreateAsyncScope();
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    
    await context.Database.MigrateAsync();
    await DbSeeder.SeedAsync(context);

    app.Logger.LogInformation("Finished seeding default data");
}
catch (Exception e)
{
    app.Logger.LogInformation("An error occurred while seeding the db: {ExMessage}", e.Message);
}

// Middleware
app.UseStaticFiles();

// If you’re running HTTPS, keep this. If you run HTTP-only, comment this out.
// app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Blazor endpoints
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.MapControllers();

// --- Minimal login API (demo) ---
app.MapPost("/api/login", async (HttpContext ctx) =>
{
    var form = await ctx.Request.ReadFormAsync();
    var email = form["email"].ToString();
    var password = form["password"].ToString();

    if (email == "admin@demo.com" && password == "Admin@123")
    {
        var claims = new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, email),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Admin"),
        };
        var id = new System.Security.Claims.ClaimsIdentity(
            claims, Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);

        await ctx.SignInAsync(
            Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme,
            new System.Security.Claims.ClaimsPrincipal(id));

        // IMPORTANT: redirect so a normal <form> submit navigates back to the app
        return Results.Redirect("/");
    }

    // Optional: redirect back to login on failure (or keep Unauthorized)
    return Results.Unauthorized();
});
app.MapGet("/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});



app.MapPost("/api/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Ok();
});

app.Run();
