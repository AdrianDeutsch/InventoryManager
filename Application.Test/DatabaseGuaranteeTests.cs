using BaseModel.Infrastructure;
using BaseModel.Model;
using Microsoft.EntityFrameworkCore;

namespace Application.Test;

/// <summary>
/// The rules the database enforces on its own. They are the safety net below the services:
/// even code that bypasses the services cannot break them.
/// </summary>
public class DatabaseGuaranteeTests : ServiceTestBase
{
    [Fact]
    public void Migrations_AreInSyncWithTheModel()
    {
        using var db = CreateContext();

        Assert.False(db.Database.HasPendingModelChanges(),
            "The model changed without a migration. Run: dotnet ef migrations add <Name> --project BaseModel");
    }

    [Fact]
    public void SecondOpenLoanForTheSameItem_IsRejectedByTheDatabase()
    {
        using var db = CreateContext();
        var cable = db.InventoryItems.Single(item => item.Id == Data.CableId);
        var linus = db.Persons.Single(person => person.Id == Data.LinusId);
        db.Add(new Reservation(linus, cable, Today));

        var exception = Assert.Throws<DbUpdateException>(() => db.SaveChanges());

        Assert.True(exception.IsUniqueConstraintViolation());
    }

    [Fact]
    public void AnotherLoanForAnItemWithOnlyClosedLoans_IsAllowed()
    {
        using var db = CreateContext();
        var laptop = db.InventoryItems.Single(item => item.Id == Data.LaptopId);
        var linus = db.Persons.Single(person => person.Id == Data.LinusId);
        db.Add(new Reservation(linus, laptop, Today));

        db.SaveChanges();

        Assert.Equal(2, db.Reservations.Count(r => r.InventoryItem.Id == Data.LaptopId));
    }

    [Fact]
    public void DuplicateSerialNumber_IsRejectedByTheDatabase()
    {
        using var db = CreateContext();
        var category = db.Categories.Single(c => c.Id == Data.NotebooksCategoryId);
        db.Add(new InventoryItem("SN-LAPTOP", "Clone", category, isAvailable: true));

        var exception = Assert.Throws<DbUpdateException>(() => db.SaveChanges());

        Assert.True(exception.IsUniqueConstraintViolation());
    }

    [Fact]
    public void ClosingTheSameLoanFromTwoContexts_SecondSaveFails()
    {
        using var first = CreateContext();
        using var second = CreateContext();
        var loanInFirst = first.Reservations.Single(r => r.Id == Data.CableOpenLoanId);
        var loanInSecond = second.Reservations.Single(r => r.Id == Data.CableOpenLoanId);

        loanInFirst.Return(Today, first.Staffs.Single(), damaged: false);
        first.SaveChanges();
        loanInSecond.Return(Today, second.Staffs.Single(), damaged: true);

        Assert.Throws<DbUpdateConcurrencyException>(() => second.SaveChanges());
    }

    [Fact]
    public void PersonWithLoans_CannotBeDeleted()
    {
        using var db = CreateContext();
        db.Remove(db.Persons.Single(person => person.Id == Data.AdaId));

        Assert.Throws<DbUpdateException>(() => db.SaveChanges());
    }

    [Fact]
    public void PersonWithoutLoans_CanBeDeleted()
    {
        using var db = CreateContext();
        db.Remove(db.Persons.Single(person => person.Id == Data.LinusId));

        db.SaveChanges();

        Assert.False(db.Persons.Any(person => person.Id == Data.LinusId));
    }

    [Fact]
    public void InventoryItemWithLoans_CannotBeDeleted()
    {
        // Even a closed loan keeps the item: the history must stay complete.
        using var db = CreateContext();
        db.Remove(db.InventoryItems.Single(item => item.Id == Data.LaptopId));

        Assert.Throws<DbUpdateException>(() => db.SaveChanges());
    }

    [Fact]
    public void InventoryItemWithoutLoans_CanBeDeleted()
    {
        using var db = CreateContext();
        db.Remove(db.InventoryItems.Single(item => item.Id == Data.BeamerId));

        db.SaveChanges();

        Assert.False(db.InventoryItems.Any(item => item.Id == Data.BeamerId));
    }
}
