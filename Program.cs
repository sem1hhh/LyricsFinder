using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using Polly;
using Polly.Extensions.Http;
using System.Net.Http;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using LyricsFinder.Data;
using LyricsFinder.Models;

try
{
    // Try to kill any previous instances of the application to avoid file locking issues
    var currentProcessName = Process.GetCurrentProcess().ProcessName;
    var processes = Process.GetProcessesByName(currentProcessName);
    
    foreach (var process in processes.Where(p => p.Id != Process.GetCurrentProcess().Id))
    {
        try
        {
            Console.WriteLine($"Killing previous process: {process.Id}");
            process.Kill();
            process.WaitForExit(3000);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error killing process {process.Id}: {ex.Message}");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error handling processes: {ex.Message}");
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Add OAuth Authentication
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
    })
    .AddFacebook(options =>
    {
        options.AppId = builder.Configuration["Authentication:Facebook:AppId"] ?? "";
        options.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"] ?? "";
    })
    .AddMicrosoftAccount(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"] ?? "";
    });

// Add Memory Cache
builder.Services.AddMemoryCache();

// Basit Rate Limiting - Middleware ile
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromSeconds(10);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 2;
    });
    
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsync("Çok fazla istek gönderildi. Lütfen tekrar deneyin.", token);
    };
});

// Polly ile HTTP Client
var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

builder.Services.AddHttpClient("LyricsApi")
    .AddPolicyHandler(retryPolicy);

// Add HttpClient and LyricsService
builder.Services.AddHttpClient();
builder.Services.AddScoped<LyricsFinder.Services.LyricsService>();

var app = builder.Build();

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

// Rate Limiting middleware'ini ekle
app.UseRateLimiter();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Lyrics}/{action=Index}/{id?}");

app.Run();
