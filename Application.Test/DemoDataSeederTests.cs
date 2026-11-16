using BaseModel.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Application.Test;

/// <summary>
/// The demo data is what a visitor sees first, so it has to obey the same rules as real data.
/// </summary>
public sealed class DemoDataSeederTests : IDisposable
{
    private static readonly DateOnly Today = new(2026, 3, 16);

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly DbContextOptions<ReservationContext> _options;

    public DemoDataSeederTests()
    {
        _connection.Open();
        _options = new DbContextOptionsBuilder<ReservationContext>().UseSqlite(_connection).Options;
        using var db = new ReservationContext(_options);
        db.Database.Migrate();
    }

    public void Dispose() => _connection.Dispose();

    private async Task<ReservationContext> SeededContextAsync()
    {
        var db = new ReservationContext(_options);
        await DemoDataSeeder.SeedIfEmptyAsync(db, Today);
        db.ChangeTracker.Clear();
        return db;
    }

    [Fact]
    public async Task SeedIfEmpty_FillsAnEmptyDatabase()
    {
        await using var db = await SeededContextAsync();

        Assert.Equal(4, await db.Categories.CountAsync());
        Assert.Equal(14, await db.InventoryItems.CountAsync());
        Assert.Equal(20, await db.Persons.CountAsync());
        Assert.Equal(3, await db.Staffs.CountAsync());
        Assert.True(await db.Reservations.AnyAsync(r => r.ReturnDate == null));
    }

    [Fact]
    public async Task SeedIfEmpty_SecondCall_ChangesNothing()
    {
        await using var db = await SeededContextAsync();
        var reservationsBefore = await db.Reservations.CountAsync();

        var seededAgain = await DemoDataSeeder.SeedIfEmptyAsync(db, Today);

        Assert.False(seededAgain);
        Assert.Equal(14, await db.InventoryItems.CountAsync());
        Assert.Equal(reservationsBefore, await db.Reservations.CountAsync());
    }

    [Fact]
    public async Task SeededData_ItemsOnLoanAreNotAvailable()
    {
        await using var db = await SeededContextAsync();

        var onLoanButAvailable = await db.InventoryItems
            .Where(item => item.IsAvailable && item.Reservations.Any(r => r.ReturnDate == null))
            .CountAsync();

        Assert.Equal(0, onLoanButAvailable);
    }

    [Fact]
    public async Task SeededData_LoansOfAnItemNeverOverlapAndNeverLieInTheFuture()
    {
        await using var db = await SeededContextAsync();
        var loansByItem = (await db.Reservations.Include(r => r.InventoryItem).ToListAsync())
            .GroupBy(r => r.InventoryItem.Id);

        foreach (var loans in loansByItem)
        {
            var ordered = loans.OrderBy(r => r.LoanDate).ToList();
            Assert.All(ordered, loan => Assert.True(loan.LoanDate <= Today));
            Assert.All(ordered, loan => Assert.True(loan.ReturnDate is null || loan.ReturnDate >= loan.LoanDate));
            for (var i = 1; i < ordered.Count; i++)
                Assert.True(ordered[i - 1].ReturnDate <= ordered[i].LoanDate, "A loan starts before the previous one ended.");
        }
    }

    [Fact]
    public async Task SeededData_CoversTheCasesTheScreensShow()
    {
        await using var db = await SeededContextAsync();
        var overdueSince = Today.AddDays(-14);

        Assert.True(await db.Reservations.AnyAsync(r => r.ReturnDate == null && r.LoanDate < overdueSince), "no overdue loan");
        Assert.True(await db.Reservations.AnyAsync(r => r.ReturnDate == null && r.LoanDate >= overdueSince), "no loan within the limit");
        Assert.True(await db.Reservations.AnyAsync(r => r.Status == BaseModel.Model.ReservationStatus.DamagedReturn), "no damaged return");
        Assert.True(await db.InventoryItems.AnyAsync(item => !item.Reservations.Any()), "no item that was never lent");
    }

    [Fact]
    public async Task SeededData_IsDeterministic()
    {
        await using var first = await SeededContextAsync();
        var firstSerials = await first.InventoryItems.OrderBy(item => item.Id).Select(item => item.SerialNumber).ToListAsync();

        using var otherConnection = new SqliteConnection("DataSource=:memory:");
        otherConnection.Open();
        var otherOptions = new DbContextOptionsBuilder<ReservationContext>().UseSqlite(otherConnection).Options;
        await using var second = new ReservationContext(otherOptions);
        await second.Database.MigrateAsync();
        await DemoDataSeeder.SeedIfEmptyAsync(second, Today);
        var secondSerials = await second.InventoryItems.OrderBy(item => item.Id).Select(item => item.SerialNumber).ToListAsync();

        Assert.Equal(firstSerials, secondSerials);
    }
}
