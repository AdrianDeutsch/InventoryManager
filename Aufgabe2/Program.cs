using Aufgabe2.Services;
using BaseModel.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
public class Program
{
    private static void Main(string[] args)
    {
        // STEP 1: Configuring ASP.NET Core Services
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddControllers();

        // Configure Datebase with settings from appsettings.json (section ConnectionStrings)
        var databaseName = builder.Configuration.GetConnectionString("Default")
            ?? throw new Exception("Missing ConnectionString:Default in appsettings.json.");
        var connection = new SqliteConnection(databaseName);
        builder.Services.AddDbContextFactory<ReservationContext>(options =>
            options.UseSqlite(connection));

        builder.Services.AddSingleton<TimeProvider>((provider) => TimeProvider.System);
        builder.Services.AddScoped<InventoryService>();

        // STEP 2: Configuring ASP.NET Core request pipeline
        var app = builder.Build();
        if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
        {

            using (var scope = app.Services.CreateScope())
            using (var service = scope.ServiceProvider.GetRequiredService<ReservationContext>())
            {
                service.Database.EnsureDeleted();
                // To prevent EnsureCreated from closing the connection and losing the in-memory DB.
                service.Database.OpenConnection();
                service.Database.EnsureCreated();
                if (app.Environment.IsDevelopment()) service.Seed();
            }
        }
        // Assign controllers to routes.
        app.MapControllers();
        app.Run();
        app.Logger.LogInformation($"Close connection to {databaseName}...");
        connection.Close();
    }
}

