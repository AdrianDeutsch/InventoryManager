using Application.Commands;
using Application.Services;
using BaseModel.Model;
using Microsoft.EntityFrameworkCore;

namespace Application.Test;

public class ReservationServiceTests : ServiceTestBase
{
    // ---- Lending --------------------------------------------------------------------------

    [Fact]
    public async Task Lend_AvailableItem_CreatesOpenLoanAndBlocksTheItem()
    {
        var loanId = await ReservationService.LendAsync(new LendItemCmd(Data.LaptopId, Data.LinusId, Today));

        using var db = CreateContext();
        var loan = await db.Reservations
            .Include(r => r.Person).Include(r => r.InventoryItem)
            .SingleAsync(r => r.Id == loanId);
        Assert.True(loan.IsOpen);
        Assert.Equal(ReservationStatus.Issued, loan.Status);
        Assert.Equal(Today, loan.LoanDate);
        Assert.Equal(Data.LinusId, loan.Person.Id);
        Assert.False(loan.InventoryItem.IsAvailable);
    }

    [Fact]
    public async Task Lend_ItemAlreadyOnLoan_IsRejected()
    {
        var exception = await Assert.ThrowsAsync<ReservationException>(() =>
            ReservationService.LendAsync(new LendItemCmd(Data.CableId, Data.LinusId, Today)));

        Assert.Contains("already on loan", exception.Message);
    }

    [Fact]
    public async Task Lend_BlockedItem_IsRejected()
    {
        var exception = await Assert.ThrowsAsync<ReservationException>(() =>
            ReservationService.LendAsync(new LendItemCmd(Data.BeamerId, Data.LinusId, Today)));

        Assert.Contains("blocked", exception.Message);
    }

    [Fact]
    public async Task Lend_LoanDateInTheFuture_IsRejected()
    {
        var exception = await Assert.ThrowsAsync<ReservationException>(() =>
            ReservationService.LendAsync(new LendItemCmd(Data.LaptopId, Data.LinusId, Today.AddDays(1))));

        Assert.Contains("future", exception.Message);
    }

    [Fact]
    public async Task Lend_LoanDateBeforeThePreviousReturn_IsRejected()
    {
        var exception = await Assert.ThrowsAsync<ReservationException>(() =>
            ReservationService.LendAsync(
                new LendItemCmd(Data.LaptopId, Data.LinusId, Fixture.LaptopReturnDate.AddDays(-1))));

        Assert.Contains("2026-01-09", exception.Message);
    }

    [Fact]
    public async Task Lend_OnTheDayOfThePreviousReturn_IsAllowed()
    {
        var loanId = await ReservationService.LendAsync(
            new LendItemCmd(Data.LaptopId, Data.LinusId, Fixture.LaptopReturnDate));

        Assert.NotEqual(0, loanId);
    }

    [Fact]
    public async Task Lend_UnknownItem_IsRejected()
    {
        var exception = await Assert.ThrowsAsync<ReservationException>(() =>
            ReservationService.LendAsync(new LendItemCmd(9999, Data.LinusId, Today)));

        Assert.Contains("item not found", exception.Message);
    }

    [Fact]
    public async Task Lend_UnknownPerson_IsRejected()
    {
        var exception = await Assert.ThrowsAsync<ReservationException>(() =>
            ReservationService.LendAsync(new LendItemCmd(Data.LaptopId, 9999, Today)));

        Assert.Contains("Person not found", exception.Message);
    }

    [Fact]
    public async Task Lend_AnotherUserLendsTheSameItemFirst_OnlyOneLoanExists()
    {
        // Both users see the laptop as available. The other one commits first.
        BeforeNextSave(db =>
        {
            var laptop = db.InventoryItems.Single(item => item.Id == Data.LaptopId);
            var alan = db.Persons.Single(person => person.Id == Data.AlanId);
            db.Add(new Reservation(alan, laptop, Today));
        });

        var exception = await Assert.ThrowsAsync<ReservationException>(() =>
            ReservationService.LendAsync(new LendItemCmd(Data.LaptopId, Data.LinusId, Today)));

        Assert.Contains("in the meantime", exception.Message);
        using var verify = CreateContext();
        var openLoan = await verify.Reservations
            .Include(r => r.Person)
            .SingleAsync(r => r.InventoryItem.Id == Data.LaptopId && r.ReturnDate == null);
        Assert.Equal(Data.AlanId, openLoan.Person.Id);
    }

    // ---- Returning ------------------------------------------------------------------------

    [Fact]
    public async Task Return_InWorkingCondition_ClosesTheLoanAndReleasesTheItem()
    {
        await ReservationService.ReturnAsync(
            new ReturnItemCmd(Data.CableOpenLoanId, Data.GraceId, Today, Damaged: false));

        using var db = CreateContext();
        var loan = await db.Reservations
            .Include(r => r.AcceptedBy).Include(r => r.InventoryItem)
            .SingleAsync(r => r.Id == Data.CableOpenLoanId);
        Assert.False(loan.IsOpen);
        Assert.Equal(ReservationStatus.Returned, loan.Status);
        Assert.Equal(Today, loan.ReturnDate);
        Assert.Equal(Data.GraceId, loan.AcceptedBy!.Id);
        Assert.True(loan.InventoryItem.IsAvailable);
    }

    [Fact]
    public async Task Return_Damaged_ClosesTheLoanButKeepsTheItemBlocked()
    {
        await ReservationService.ReturnAsync(
            new ReturnItemCmd(Data.CableOpenLoanId, Data.GraceId, Today, Damaged: true));

        using var db = CreateContext();
        var loan = await db.Reservations.Include(r => r.InventoryItem).SingleAsync(r => r.Id == Data.CableOpenLoanId);
        Assert.Equal(ReservationStatus.DamagedReturn, loan.Status);
        Assert.False(loan.InventoryItem.IsAvailable);
    }

    [Fact]
    public async Task Return_OnTheLoanDate_IsAllowed()
    {
        await ReservationService.ReturnAsync(
            new ReturnItemCmd(Data.CableOpenLoanId, Data.GraceId, Fixture.CableLoanDate, Damaged: false));

        using var db = CreateContext();
        Assert.Equal(Fixture.CableLoanDate, (await db.Reservations.SingleAsync(r => r.Id == Data.CableOpenLoanId)).ReturnDate);
    }

    [Fact]
    public async Task Return_BeforeTheLoanDate_IsRejected()
    {
        var exception = await Assert.ThrowsAsync<ReservationException>(() =>
            ReservationService.ReturnAsync(
                new ReturnItemCmd(Data.CableOpenLoanId, Data.GraceId, Fixture.CableLoanDate.AddDays(-1), Damaged: false)));

        Assert.Contains("before the loan date", exception.Message);
    }

    [Fact]
    public async Task Return_InTheFuture_IsRejected()
    {
        var exception = await Assert.ThrowsAsync<ReservationException>(() =>
            ReservationService.ReturnAsync(
                new ReturnItemCmd(Data.CableOpenLoanId, Data.GraceId, Today.AddDays(1), Damaged: false)));

        Assert.Contains("future", exception.Message);
    }

    [Fact]
    public async Task Return_LoanThatIsAlreadyClosed_IsRejected()
    {
        var exception = await Assert.ThrowsAsync<ReservationException>(() =>
            ReservationService.ReturnAsync(
                new ReturnItemCmd(Data.LaptopClosedLoanId, Data.GraceId, Today, Damaged: false)));

        Assert.Contains("already been returned", exception.Message);
    }

    [Fact]
    public async Task Return_UnknownLoan_IsRejected()
    {
        var exception = await Assert.ThrowsAsync<ReservationException>(() =>
            ReservationService.ReturnAsync(new ReturnItemCmd(9999, Data.GraceId, Today, Damaged: false)));

        Assert.Contains("Loan not found", exception.Message);
    }

    [Fact]
    public async Task Return_UnknownStaff_IsRejectedAndTheLoanStaysOpen()
    {
        var exception = await Assert.ThrowsAsync<ReservationException>(() =>
            ReservationService.ReturnAsync(new ReturnItemCmd(Data.CableOpenLoanId, 9999, Today, Damaged: false)));

        Assert.Contains("Staff member not found", exception.Message);
        using var db = CreateContext();
        Assert.True((await db.Reservations.SingleAsync(r => r.Id == Data.CableOpenLoanId)).IsOpen);
    }

    [Fact]
    public async Task Return_AnotherUserReturnsTheSameLoanFirst_TheFirstReturnWins()
    {
        // The other user reports the cable as damaged a moment before us.
        BeforeNextSave(db =>
        {
            var loan = db.Reservations.Include(r => r.InventoryItem).Single(r => r.Id == Data.CableOpenLoanId);
            loan.Return(Today.AddDays(-1), db.Staffs.Single(), damaged: true);
            loan.InventoryItem.IsAvailable = false;
        });

        var exception = await Assert.ThrowsAsync<ReservationException>(() =>
            ReservationService.ReturnAsync(
                new ReturnItemCmd(Data.CableOpenLoanId, Data.GraceId, Today, Damaged: false)));

        Assert.Contains("already been returned", exception.Message);
        using var verify = CreateContext();
        var persisted = await verify.Reservations.Include(r => r.InventoryItem).SingleAsync(r => r.Id == Data.CableOpenLoanId);
        Assert.Equal(ReservationStatus.DamagedReturn, persisted.Status);
        Assert.Equal(Today.AddDays(-1), persisted.ReturnDate);
        Assert.False(persisted.InventoryItem.IsAvailable);
    }

    [Fact]
    public async Task LendReturnLend_FullCycle_KeepsTheHistory()
    {
        var firstLoan = await ReservationService.LendAsync(new LendItemCmd(Data.LaptopId, Data.LinusId, Today.AddDays(-3)));
        await ReservationService.ReturnAsync(new ReturnItemCmd(firstLoan, Data.GraceId, Today.AddDays(-1), Damaged: false));
        await ReservationService.LendAsync(new LendItemCmd(Data.LaptopId, Data.AlanId, Today));

        var history = await ReservationService.GetHistoryAsync(Data.LaptopId);

        // Newest first: Alan (open), Linus (returned), Ada (returned in January).
        Assert.Equal(["Alan Turing", "Linus Torvalds", "Ada Lovelace"], history.Select(entry => entry.PersonName));
        Assert.Equal(
            [ReservationStatus.Issued, ReservationStatus.Returned, ReservationStatus.Returned],
            history.Select(entry => entry.Status));
        Assert.Null(history[0].ReturnDate);
        Assert.Null(history[0].AcceptedBy);
    }

    // ---- Queries --------------------------------------------------------------------------

    [Fact]
    public async Task GetOpenLoans_ReturnsOnlyOpenLoansOldestFirst()
    {
        var loans = await ReservationService.GetOpenLoansAsync();

        Assert.Equal([Data.DockOpenLoanId, Data.CableOpenLoanId], loans.Select(loan => loan.ReservationId));
        var dockLoan = loans[0];
        Assert.Equal("Docking Station", dockLoan.ItemName);
        Assert.Equal("SN-DOCK", dockLoan.SerialNumber);
        Assert.Equal("Ada Lovelace", dockLoan.PersonName);
        Assert.Equal(Fixture.DockLoanDate.AddDays(MaxLoanDays), dockLoan.DueDate);
        Assert.True(dockLoan.IsOverdue);
        Assert.False(loans[1].IsOverdue);
    }

    [Fact]
    public async Task GetCategoriesWithCount_CountsItemsAndIncludesEmptyCategories()
    {
        var categories = await ReservationService.GetCategoriesWithCountAsync();

        Assert.Equal(
            [("Cables", 2), ("Notebooks", 2), ("Projectors", 0)],
            categories.Select(category => (category.Text, category.Count)));
    }

    [Fact]
    public async Task GetPersonsWithOpenReservations_ListsOnlyPeopleWhoHoldAnItem()
    {
        var borrowers = await ReservationService.GetPersonsWithOpenReservationsAsync();

        // Sorted by last name; Linus holds nothing.
        Assert.Equal(["Ada Lovelace", "Alan Turing"], borrowers.Select(borrower => borrower.Name));
        Assert.All(borrowers, borrower => Assert.Equal(1, borrower.OpenLoans));
    }

    [Fact]
    public async Task GetPersonsWithOpenReservations_CountsEveryOpenLoanOfAPerson()
    {
        await ReservationService.LendAsync(new LendItemCmd(Data.LaptopId, Data.AdaId, Today));

        var borrowers = await ReservationService.GetPersonsWithOpenReservationsAsync();

        Assert.Equal(2, Assert.Single(borrowers, borrower => borrower.PersonId == Data.AdaId).OpenLoans);
    }

    [Fact]
    public async Task GetInventoryItemsWithoutReservations_ReturnsItemsNotLentInThatYear()
    {
        var notLentIn2026 = await ReservationService.GetInventoryItemsWithoutReservationsAsync(2026);
        var notLentIn2025 = await ReservationService.GetInventoryItemsWithoutReservationsAsync(2025);

        Assert.Equal([Data.BeamerId], notLentIn2026.Select(item => item.Id));
        Assert.Equal(4, notLentIn2025.Count);
    }

    [Fact]
    public async Task GetInventoryItemsWithoutReservations_LoanCountsForTheYearItStartedIn()
    {
        // Lent on the last day of 2025: the beamer was used in 2025, whatever happened afterwards.
        using (var db = CreateContext())
        {
            var beamer = db.InventoryItems.Single(item => item.Id == Data.BeamerId);
            var linus = db.Persons.Single(person => person.Id == Data.LinusId);
            db.Add(new Reservation(linus, beamer, new DateOnly(2025, 12, 31)));
            db.SaveChanges();
        }

        var notLentIn2025 = await ReservationService.GetInventoryItemsWithoutReservationsAsync(2025);
        var notLentIn2026 = await ReservationService.GetInventoryItemsWithoutReservationsAsync(2026);

        Assert.DoesNotContain(notLentIn2025, item => item.Id == Data.BeamerId);
        Assert.Contains(notLentIn2026, item => item.Id == Data.BeamerId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(9999)]
    public async Task GetInventoryItemsWithoutReservations_InvalidYear_IsRejected(int year)
        => await Assert.ThrowsAsync<ReservationException>(() =>
            ReservationService.GetInventoryItemsWithoutReservationsAsync(year));

    [Fact]
    public async Task GetPersons_ReturnsEveryoneSortedByLastName()
    {
        var persons = await ReservationService.GetPersonsAsync();

        Assert.Equal(
            ["Lovelace Ada (5AAIF)", "Torvalds Linus (5CAIF)", "Turing Alan (5BAIF)"],
            persons.Select(person => person.Text));
    }

    [Fact]
    public async Task GetStaff_ReturnsStaffForTheReturnDialog()
    {
        var staff = await ReservationService.GetStaffAsync();

        var grace = Assert.Single(staff);
        Assert.Equal(Data.GraceId, grace.Id);
        Assert.Equal("Hopper Grace", grace.Text);
    }
}
