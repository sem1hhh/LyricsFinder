using System;
using System.Diagnostics;
using System.Linq;

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

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Lyrics}/{action=Index}/{id?}");

app.Run();
