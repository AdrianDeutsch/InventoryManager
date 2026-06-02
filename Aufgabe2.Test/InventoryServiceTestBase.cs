using Aufgabe2.Services;
using BaseModel.Infrastructure;
using BaseModel.Model;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Aufgabe2.Test;

/// <summary>
/// Base class for <see cref="InventoryService"/> tests.
/// <para>
/// We deliberately test against a real (in-memory) SQLite provider instead of mocking the
/// <c>DbContext</c>: the service relies on genuine EF Core query translation, change tracking
/// and <c>SaveChanges</c> semantics, none of which can be faithfully reproduced by a mock.
/// SQLite in-memory keeps the tests fast and fully isolated while exercising real SQL.
/// </para>
/// <para>
/// A single open connection is shared for the lifetime of each test instance so that the
/// in-memory database survives across the several short-lived contexts a test may create
/// (one to act, a fresh one to assert persistence independently of the change tracker).
/// </para>
/// </summary>
public abstract class InventoryServiceTestBase : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ReservationContext> _options;

    /// <summary>Id of the seeded "Notebooks" category.</summary>
    protected int NotebooksCategoryId { get; private set; }

    /// <summary>Id of the seeded available item that has one reservation.</summary>
    protected int AvailableItemId { get; private set; }

    /// <summary>Id of the seeded unavailable item without reservations.</summary>
    protected int UnavailableItemId { get; private set; }

    protected InventoryServiceTestBase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<ReservationContext>()
            .UseSqlite(_connection)
            .Options;

        using var db = CreateContext();
        db.Database.EnsureCreated();
        SeedFixtures(db);
    }

    /// <summary>Creates a fresh context bound to the shared in-memory database.</summary>
    protected ReservationContext CreateContext() => new(_options);

    /// <summary>Creates the system under test on top of the given context.</summary>
    protected static InventoryService CreateService(ReservationContext db) => new(db);

    /// <summary>
    /// Seeds a small, deterministic fixture: two categories, one available item with a single
    /// reservation and one unavailable item without reservations.
    /// </summary>
    private void SeedFixtures(ReservationContext db)
    {
        var notebooks = new Category("Notebooks");
        var cables = new Category("Cables");
        db.Categories.AddRange(notebooks, cables);

        var availableItem = new InventoryItem("SN-AVAILABLE", "Developer Laptop", notebooks, isAvailable: true);
        var unavailableItem = new InventoryItem("SN-UNAVAILABLE", "USB-C Cable", cables, isAvailable: false);
        db.InventoryItems.AddRange(availableItem, unavailableItem);

        var person = new Person("Ada", "Lovelace", "5AAIF", "ada@example.com", "+43 1 234567");
        var status = new Status("Issued");
        db.Persons.Add(person);
        db.States.Add(status);

        db.Reservations.Add(new Reservation(
            person, availableItem, new DateOnly(2026, 1, 5), status));

        db.SaveChanges();

        NotebooksCategoryId = notebooks.Id;
        AvailableItemId = availableItem.Id;
        UnavailableItemId = unavailableItem.Id;
    }

    public void Dispose() => _connection.Dispose();
}
