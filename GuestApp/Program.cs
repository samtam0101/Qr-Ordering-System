using Infrastructure;
using Microsoft.EntityFrameworkCore;
using GuestApp.Services; 

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddScoped<CartService>();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<OrderService>();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    await DbSeeder.SeedAsync(db);
}
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.Run();
