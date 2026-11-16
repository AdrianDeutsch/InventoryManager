using Application.Services;
using Microsoft.Extensions.Options;

namespace Application.Test;

public class LoanPolicyTests
{
    private static readonly DateOnly Today = new(2026, 3, 16);

    private static LoanPolicy CreatePolicy(int maxLoanDays, DateTimeOffset? now = null, TimeZoneInfo? timeZone = null)
        => new(
            Options.Create(new LoanOptions { MaxLoanDays = maxLoanDays }),
            new StubTimeProvider(now ?? new DateTimeOffset(2026, 3, 16, 12, 0, 0, TimeSpan.Zero), timeZone ?? TimeZoneInfo.Utc));

    [Fact]
    public void DueDate_IsLoanDatePlusMaxLoanDays()
        => Assert.Equal(new DateOnly(2026, 3, 15), CreatePolicy(maxLoanDays: 14).DueDate(new DateOnly(2026, 3, 1)));

    [Theory]
    [InlineData(13, false)] // one day left
    [InlineData(14, false)] // due today: still on time
    [InlineData(15, true)]  // due yesterday
    [InlineData(60, true)]
    public void IsOverdue_StartsTheDayAfterTheDueDate(int daysOnLoan, bool expected)
        => Assert.Equal(expected, CreatePolicy(maxLoanDays: 14).IsOverdue(Today.AddDays(-daysOnLoan)));

    [Fact]
    public void IsOverdue_RespectsTheConfiguredLimit()
    {
        var loanDate = Today.AddDays(-10);

        Assert.True(CreatePolicy(maxLoanDays: 7).IsOverdue(loanDate));
        Assert.False(CreatePolicy(maxLoanDays: 30).IsOverdue(loanDate));
    }

    [Fact]
    public void Today_IsTheLocalCalendarDayNotTheUtcOne()
    {
        // 23:30 UTC on the 16th is already the 17th in Vienna (UTC+1 in March).
        var vienna = TimeZoneInfo.FindSystemTimeZoneById("Europe/Vienna");
        var policy = CreatePolicy(14, new DateTimeOffset(2026, 3, 16, 23, 30, 0, TimeSpan.Zero), vienna);

        Assert.Equal(new DateOnly(2026, 3, 17), policy.Today);
    }

    private sealed class StubTimeProvider(DateTimeOffset now, TimeZoneInfo timeZone) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
        public override TimeZoneInfo LocalTimeZone => timeZone;
    }
}
