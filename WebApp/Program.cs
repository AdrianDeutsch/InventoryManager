using Application.Services;
using BaseModel.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Radzen;
using WebApp.Components;
using WebApp.Components.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddRadzenComponents();
builder.Services.AddHealthChecks();

// A factory instead of a scoped DbContext: in Blazor Server a scope lives as long as the
// browser tab stays open, so a scoped context would be shared by every click of a user and
// keep serving the entities it loaded minutes ago. The services create one context per operation.
// The connection string is read when the first context is created, not here: the end-to-end
// tests start this program with their own configuration, which is only complete after Build().
builder.Services.AddDbContextFactory<ReservationContext>((services, options) =>
{
    var connectionString = new SqliteConnectionStringBuilder(
        services.GetRequiredService<IConfiguration>().GetConnectionString("Default")
        ?? throw new InvalidOperationException("Missing ConnectionStrings:Default in appsettings.json."));
    // A relative file name would depend on the directory the app happens to be started from.
    if (!Path.IsPathRooted(connectionString.DataSource))
    {
        connectionString.DataSource = Path.Combine(
            services.GetRequiredService<IHostEnvironment>().ContentRootPath, connectionString.DataSource);
    }

    options.UseSqlite(connectionString.ToString());
});

builder.Services.Configure<LoanOptions>(builder.Configuration.GetSection(LoanOptions.SectionName));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<LoanPolicy>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<ReservationService>();
builder.Services.AddScoped<LoanDialogs>();
builder.Services.AddScoped<TechnicalErrorNotifier>();

var app = builder.Build();

// Bring the schema up to date before the first request and, if configured, fill an empty
// database with demo data.
await using (var db = await app.Services.GetRequiredService<IDbContextFactory<ReservationContext>>().CreateDbContextAsync())
{
    await db.Database.MigrateAsync();
    if (app.Configuration.GetValue<bool>("DemoData:SeedOnEmptyDatabase"))
    {
        var today = app.Services.GetRequiredService<LoanPolicy>().Today;
        if (await DemoDataSeeder.SeedIfEmptyAsync(db, today))
            app.Logger.LogInformation("Empty database: demo data created.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

// The UI is English, so dates and numbers follow one English culture, whatever the
// language of the server or the browser is.
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("en-GB")
    .AddSupportedCultures("en-GB")
    .AddSupportedUICultures("en-GB"));

app.UseStaticFiles();
app.UseAntiforgery();

app.MapHealthChecks("/healthz");
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// Makes the entry point visible to the end-to-end tests, which host the app themselves.
public partial class Program;
