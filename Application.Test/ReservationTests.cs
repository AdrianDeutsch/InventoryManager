using BaseModel.Model;

namespace Application.Test;

/// <summary>Rules the <see cref="Reservation"/> entity enforces on its own, without a database.</summary>
public class ReservationTests
{
    private static readonly DateOnly LoanDate = new(2026, 3, 10);

    private static Reservation NewLoan()
    {
        var item = new InventoryItem("SN-1", "Laptop", new Category("Notebooks"), isAvailable: true);
        var person = new Person("Ada", "Lovelace", "5AAIF", "ada@example.com", "+43 1 234567");
        return new Reservation(person, item, LoanDate);
    }

    [Fact]
    public void NewReservation_IsAnOpenLoan()
    {
        var loan = NewLoan();

        Assert.True(loan.IsOpen);
        Assert.Equal(ReservationStatus.Issued, loan.Status);
        Assert.Null(loan.ReturnDate);
        Assert.Null(loan.AcceptedBy);
    }

    [Theory]
    [InlineData(false, ReservationStatus.Returned)]
    [InlineData(true, ReservationStatus.DamagedReturn)]
    public void Return_SetsStatusDateAndStaffTogether(bool damaged, ReservationStatus expectedStatus)
    {
        var loan = NewLoan();
        var staff = new Staff("Grace", "Hopper");

        loan.Return(LoanDate.AddDays(2), staff, damaged);

        Assert.False(loan.IsOpen);
        Assert.Equal(expectedStatus, loan.Status);
        Assert.Equal(LoanDate.AddDays(2), loan.ReturnDate);
        Assert.Same(staff, loan.AcceptedBy);
    }

    [Fact]
    public void Return_ASecondTime_IsRejectedAndKeepsTheFirstReturn()
    {
        var loan = NewLoan();
        loan.Return(LoanDate.AddDays(2), new Staff("Grace", "Hopper"), damaged: false);

        Assert.Throws<InvalidOperationException>(
            () => loan.Return(LoanDate.AddDays(5), new Staff("Alan", "Kay"), damaged: true));

        Assert.Equal(ReservationStatus.Returned, loan.Status);
        Assert.Equal(LoanDate.AddDays(2), loan.ReturnDate);
    }
}
