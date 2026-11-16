using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace WebApp.E2ETest;

/// <summary>
/// Starts the real application on a free local port and one browser for all tests.
/// The app runs on Kestrel, not on the in-memory test server, because Blazor Server needs a
/// real connection for its SignalR circuit. The database is a throw-away file seeded with the
/// demo data, so the tests never touch a developer's own <c>inventory.db</c>.
/// </summary>
public sealed class AppFixture : IAsyncLifetime
{
    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"inventory-e2e-{Guid.NewGuid():N}.db");

    private InventoryApp? _app;
    private IPlaywright? _playwright;

    public IBrowser Browser { get; private set; } = null!;

    public string BaseUrl { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _app = new InventoryApp(_databasePath);
        _app.UseKestrel(0);
        _app.StartServer();
        BaseUrl = _app.ClientOptions.BaseAddress.ToString().TrimEnd('/');

        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            // HEADLESS=false shows the browser while a test runs.
            Headless = !string.Equals(Environment.GetEnvironmentVariable("HEADLESS"), "false", StringComparison.OrdinalIgnoreCase),
        });
    }

    public async Task DisposeAsync()
    {
        await Browser.DisposeAsync();
        _playwright?.Dispose();
        if (_app is not null)
            await _app.DisposeAsync();

        foreach (var file in new[] { _databasePath, _databasePath + "-shm", _databasePath + "-wal" })
            File.Delete(file);
    }

    private sealed class InventoryApp(string databasePath) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:Default", $"Data Source={databasePath};Pooling=False");
            builder.UseSetting("DemoData:SeedOnEmptyDatabase", "true");
        }
    }
}

[CollectionDefinition(Name)]
public sealed class AppCollection : ICollectionFixture<AppFixture>
{
    public const string Name = "Inventory app";
}
