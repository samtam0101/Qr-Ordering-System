using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.ResponseCompression; 

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.Configure<FormOptions>(o =>                
{
    o.MultipartBodyLengthLimit = 100 * 1024 * 1024; // 100 MB
});
// IMPORTANT: Blazor/SignalR timeouts & sizes
builder.Services
    .AddServerSideBlazor()
    .AddHubOptions(o =>
    {
        o.ClientTimeoutInterval = TimeSpan.FromSeconds(60);   // client considered disconnected after this quiet period
        o.KeepAliveInterval = TimeSpan.FromSeconds(15);       // server pings client
        o.MaximumReceiveMessageSize = 64 * 1024 * 1024;       // 64 MB SignalR message (streams/chunks)
        o.HandshakeTimeout = TimeSpan.FromSeconds(15);
    })
    .AddCircuitOptions(o =>
    {
        o.DetailedErrors = true;
        o.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(3); // uploads via JS interop get more time
        o.DisconnectedCircuitMaxRetained = 200;
        o.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(5);
    });
// (optional) compress SignalR payloads
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});

// Kestrel request limits (before builder.Build())
builder.WebHost.ConfigureKestrel(k =>
{
    k.Limits.MaxRequestBodySize = 100 * 1024 * 1024; // 100 MB
    k.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(3);
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
var webRoot = app.Environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
Directory.CreateDirectory(Path.Combine(webRoot, "uploads"));

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
app.UseResponseCompression();
// If you’re running HTTPS, keep this. If you run HTTP-only, comment this out.
// app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Blazor endpoints
app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

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
app.MapPost("/api/upload", async (IWebHostEnvironment env, HttpRequest req) =>
{
    IFormFile? file = req.Form.Files.FirstOrDefault();
    if (file is null || file.Length == 0) return Results.BadRequest("No file");

    var uploads = Path.Combine(env.WebRootPath, "uploads");
    Directory.CreateDirectory(uploads);

    var name = $"menu_{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
    var dest = Path.Combine(uploads, name);

    await using var fs = System.IO.File.Create(dest);
    await file.CopyToAsync(fs);

    // Return web path used by GuestApp
    return Results.Ok(new { url = $"/uploads/{name}" });
})
.DisableAntiforgery()
.WithName("UploadFile")
.Accepts<IFormFile>("multipart/form-data")
.Produces<int>(StatusCodes.Status200OK);


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