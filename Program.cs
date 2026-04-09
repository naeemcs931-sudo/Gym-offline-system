using GymOfflineSystem.Models;
using GymOfflineSystem.Services;

namespace GymOfflineSystem;

public class Program
{
    public static void Main(string[] args)
    {
        // Fix for running as EXE: Set content root to the EXE's directory
        // This MUST be done before creating the builder, otherwise .NET 8 throws NotSupportedException
        var exePath = AppDomain.CurrentDomain.BaseDirectory;
        var options = new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = exePath,
            WebRootPath = Path.Combine(exePath, "wwwroot")
        };
        var builder = WebApplication.CreateBuilder(options);

        // MVC
        builder.Services.AddControllersWithViews();

        // Offline services
        builder.Services.AddSingleton<JsonFileService>();
        builder.Services.AddSingleton<ClientService>();
        builder.Services.AddSingleton<PaymentService>();
        builder.Services.AddSingleton<PaymentService>();
        builder.Services.AddSingleton<ReportService>();
        builder.Services.AddSingleton<ReportService>();


        var app = builder.Build();

        // ===============================
        // STARTUP SYSTEM CHECKS
        // ===============================
        using (var scope = app.Services.CreateScope())
        {
            var json = scope.ServiceProvider.GetRequiredService<JsonFileService>();
            var clientService = scope.ServiceProvider.GetRequiredService<ClientService>();

            // 1. Load system config
            var sys = json.Read<SystemConfig>("system.json");

            // 2. Auto year switch + new payment file
            if (sys.CurrentYear != DateTime.Now.Year)
            {
                sys.CurrentYear = DateTime.Now.Year;
                json.Write("system.json", sys);
            }

            var paymentFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", $"payments_{sys.CurrentYear}.json");
            if (!System.IO.File.Exists(paymentFile))
            {
                System.IO.File.WriteAllText(paymentFile, "[]");
            }

            // 3. Auto deactivate inactive members
            clientService.AutoDeactivateInactiveMembers();
        }

        // ===============================
        // HTTP PIPELINE
        // ===============================

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();
    }
}
