using Aufgabe2.Components;
using Aufgabe2.Services;
using BaseModel.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

// --- Blazor Server (InteractiveServer) ---
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// --- Radzen: notification, dialog, tooltip and context-menu services ---
builder.Services.AddRadzenComponents();

// --- Persistence ---
// The connection string points to an in-memory SQLite database (DataSource=:memory:).
// In-memory databases live exactly as long as their underlying connection, so we open a
// single shared connection up front and keep it alive for the whole application lifetime.
// EF Core never closes a connection it did not open itself, which is exactly what we need here.
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:Default in appsettings.json.");
var connection = new SqliteConnection(connectionString);
connection.Open();

builder.Services.AddDbContext<ReservationContext>(options => options.UseSqlite(connection));
builder.Services.AddScoped<InventoryService>();

var app = builder.Build();

// Create (and in development seed) the schema once on the shared connection.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ReservationContext>();
    db.Database.EnsureCreated();
    if (app.Environment.IsDevelopment())
        db.Seed();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
connection.Close();
