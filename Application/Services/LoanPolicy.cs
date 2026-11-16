using Microsoft.Extensions.Options;
using System;

namespace Application.Services;

public class LoanOptions
{
    public const string SectionName = "Loans";

    /// <summary>Number of days an item may stay with a person before the loan counts as overdue.</summary>
    public int MaxLoanDays { get; set; } = 14;
}

/// <summary>
/// The lending rules that depend on the calendar. The clock is injected, so "today" and
/// "overdue" can be tested without waiting for real time to pass.
/// </summary>
public class LoanPolicy(IOptions<LoanOptions> options, TimeProvider clock)
{
    public DateOnly Today => DateOnly.FromDateTime(clock.GetLocalNow().DateTime);

    public DateOnly DueDate(DateOnly loanDate) => loanDate.AddDays(options.Value.MaxLoanDays);

    /// <summary>A loan is overdue from the day after its due date.</summary>
    public bool IsOverdue(DateOnly loanDate) => Today > DueDate(loanDate);
}
