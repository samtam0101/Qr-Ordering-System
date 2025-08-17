using Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

app.UseStaticFiles();
// app.UseHttpsRedirection(); // keep HTTP to avoid dev cert prompts

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
