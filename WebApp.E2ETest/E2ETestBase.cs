using System.Runtime.CompilerServices;

namespace WebApp.E2ETest;

/// <summary>
/// Gives every test its own browser context (own cookies, own Blazor circuit) and keeps the
/// evidence when a test fails: a full-page screenshot and a Playwright trace.
/// </summary>
[Collection(AppCollection.Name)]
public abstract class E2ETestBase(AppFixture app)
{
    private static readonly string ArtifactsDirectory =
        Path.Combine(AppContext.BaseDirectory, "artifacts");

    private static int _sequence;

    protected string AppBaseUrl => app.BaseUrl;

    /// <summary>A serial number no other test uses, so tests can run in any order.</summary>
    protected static string UniqueSerialNumber(string prefix = "E2E")
        => $"{prefix}-{DateTime.UtcNow:HHmmssfff}-{Interlocked.Increment(ref _sequence)}";

    protected async Task RunAsync(Func<IPage, Task> test, [CallerMemberName] string testName = "")
    {
        await using var context = await app.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = app.BaseUrl,
            ViewportSize = new ViewportSize { Width = 1440, Height = 900 },
        });
        context.SetDefaultTimeout(15_000);
        await context.Tracing.StartAsync(new TracingStartOptions { Screenshots = true, Snapshots = true });
        var page = await context.NewPageAsync();

        try
        {
            await test(page);
            await context.Tracing.StopAsync();
        }
        catch
        {
            Directory.CreateDirectory(ArtifactsDirectory);
            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(ArtifactsDirectory, $"{testName}.png"),
                FullPage = true,
            });
            // Open with: pwsh bin/Debug/net10.0/playwright.ps1 show-trace <file>
            await context.Tracing.StopAsync(new TracingStopOptions
            {
                Path = Path.Combine(ArtifactsDirectory, $"{testName}.trace.zip"),
            });
            throw;
        }
    }
}
