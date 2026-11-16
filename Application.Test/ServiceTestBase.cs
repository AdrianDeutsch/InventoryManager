using Application.Services;
using BaseModel.Infrastructure;
using BaseModel.Model;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;

namespace Application.Test;

/// <summary>
/// Base class for the service tests.
/// <para>
/// The services run against a real SQLite database (in memory), created by the same migrations
/// the app uses. A mocked <c>DbContext</c> would not exercise query translation, unique indexes
/// or concurrency tokens, and those are exactly the parts that carry the business rules.
/// </para>
/// <para>
/// Each test gets its own database. One open connection keeps the in-memory database alive
/// while the services create and dispose a context per operation, just like in the app.
/// </para>
/// </summary>
public abstract class ServiceTestBase : IDisposable
{
    /// <summary>The fixed "today" of all tests, a Monday.</summary>
    protected static readonly DateOnly Today = new(2026, 3, 16);

    protected const int MaxLoanDays = 14;

    private readonly SqliteConnection _connection;
    private readonly BeforeSaveInterceptor _beforeSave = new();

    protected IDbContextFactory<ReservationContext> DbFactory { get; }
    protected LoanPolicy LoanPolicy { get; }
    protected InventoryService InventoryService { get; }
    protected ReservationService ReservationService { get; }
    protected Fixture Data { get; }

    protected ServiceTestBase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ReservationContext>()
            .UseSqlite(_connection)
            .AddInterceptors(_beforeSave)
            .Options;
        DbFactory = new TestDbContextFactory(options);

        LoanPolicy = new LoanPolicy(
            Options.Create(new LoanOptions { MaxLoanDays = MaxLoanDays }),
            new FixedTimeProvider(Today));
        InventoryService = new InventoryService(DbFactory, LoanPolicy);
        ReservationService = new ReservationService(DbFactory, LoanPolicy);

        using var db = DbFactory.CreateDbContext();
        db.Database.Migrate();
        Data = Fixture.Seed(db);
    }

    /// <summary>A fresh context for arranging data or verifying what really was persisted.</summary>
    protected ReservationContext CreateContext() => DbFactory.CreateDbContext();

    /// <summary>
    /// Runs <paramref name="competingWrite"/> right before the next <c>SaveChanges</c> of a
    /// service. This reproduces a race deterministically: the service has already passed its
    /// checks when "the other user" commits first.
    /// </summary>
    protected void BeforeNextSave(Action<ReservationContext> competingWrite)
        => _beforeSave.Arm(() =>
        {
            using var db = CreateContext();
            competingWrite(db);
            db.SaveChanges();
        });

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed class TestDbContextFactory(DbContextOptions<ReservationContext> options)
        : IDbContextFactory<ReservationContext>
    {
        public ReservationContext CreateDbContext() => new(options);
    }

    private sealed class FixedTimeProvider(DateOnly today) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
            => new(today.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero);

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private sealed class BeforeSaveInterceptor : SaveChangesInterceptor
    {
        private Action? _armed;

        public void Arm(Action action) => _armed = action;

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            // Disarm first: the competing write saves too and must not trigger itself.
            var action = Interlocked.Exchange(ref _armed, null);
            action?.Invoke();
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}

/// <summary>
/// The data every test starts with. Small enough to keep in your head:
/// <list type="bullet">
/// <item>Laptop: available, was lent to Ada in January and returned.</item>
/// <item>Cable: on loan to Alan for 6 days (within the 14-day limit).</item>
/// <item>Dock: on loan to Ada for 24 days (overdue).</item>
/// <item>Beamer: blocked, never lent.</item>
/// <item>Category "Projectors" has no items; Linus never borrowed anything.</item>
/// </list>
/// </summary>
public sealed record Fixture(
    int NotebooksCategoryId,
    int LaptopId,
    int CableId,
    int DockId,
    int BeamerId,
    int AdaId,
    int AlanId,
    int LinusId,
    int GraceId,
    int LaptopClosedLoanId,
    int CableOpenLoanId,
    int DockOpenLoanId)
{
    public static readonly DateOnly LaptopLoanDate = new(2026, 1, 5);
    public static readonly DateOnly LaptopReturnDate = new(2026, 1, 9);
    public static readonly DateOnly CableLoanDate = new(2026, 3, 10);
    public static readonly DateOnly DockLoanDate = new(2026, 2, 20);

    public static Fixture Seed(ReservationContext db)
    {
        var notebooks = new Category("Notebooks");
        var cables = new Category("Cables");
        var projectors = new Category("Projectors");

        var laptop = new InventoryItem("SN-LAPTOP", "Developer Laptop", notebooks, isAvailable: true);
        var cable = new InventoryItem("SN-CABLE", "USB-C Cable", cables, isAvailable: false);
        var dock = new InventoryItem("SN-DOCK", "Docking Station", notebooks, isAvailable: false);
        var beamer = new InventoryItem("SN-BEAMER", "Old Beamer", cables, isAvailable: false);

        var ada = new Person("Ada", "Lovelace", "5AAIF", "ada@example.com", "+43 1 234567");
        var alan = new Person("Alan", "Turing", "5BAIF", "alan@example.com", "+43 1 345678");
        var linus = new Person("Linus", "Torvalds", "5CAIF", "linus@example.com", "+43 1 456789");
        var grace = new Staff("Grace", "Hopper");

        var laptopLoan = new Reservation(ada, laptop, LaptopLoanDate);
        laptopLoan.Return(LaptopReturnDate, grace, damaged: false);
        var cableLoan = new Reservation(alan, cable, CableLoanDate);
        var dockLoan = new Reservation(ada, dock, DockLoanDate);

        db.AddRange(notebooks, cables, projectors);
        db.AddRange(laptop, cable, dock, beamer);
        db.AddRange(ada, alan, linus);
        db.Add(grace);
        db.AddRange(laptopLoan, cableLoan, dockLoan);
        db.SaveChanges();

        return new Fixture(
            notebooks.Id, laptop.Id, cable.Id, dock.Id, beamer.Id,
            ada.Id, alan.Id, linus.Id, grace.Id,
            laptopLoan.Id, cableLoan.Id, dockLoan.Id);
    }
}
