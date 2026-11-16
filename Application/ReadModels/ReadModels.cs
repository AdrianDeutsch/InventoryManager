using BaseModel.Model;
using System;
using System.Collections.Generic;

namespace Application.ReadModels;

// Read side of the application: flat, immutable records shaped for the screens that show them.
// The UI never sees EF entities, so it cannot trigger lazy queries or change tracked state.

/// <summary>The open loan of an item, as far as a list needs it.</summary>
public record CurrentLoan(
    int ReservationId,
    string PersonName,
    string SchoolClass,
    DateOnly LoanDate,
    DateOnly DueDate,
    bool IsOverdue);

public record InventoryItemSummary(
    int Id,
    string SerialNumber,
    string ItemName,
    string Category,
    bool IsAvailable,
    CurrentLoan? CurrentLoan)
{
    public bool IsOnLoan => CurrentLoan is not null;
}

/// <summary>One line of an item's loan history.</summary>
public record LoanHistoryEntry(
    int ReservationId,
    string PersonName,
    string SchoolClass,
    DateOnly LoanDate,
    DateOnly? ReturnDate,
    ReservationStatus Status,
    string? AcceptedBy);

public record InventoryItemDetails(
    InventoryItemSummary Item,
    IReadOnlyList<LoanHistoryEntry> History);

public record OpenLoan(
    int ReservationId,
    int InventoryItemId,
    string ItemName,
    string SerialNumber,
    string PersonName,
    string SchoolClass,
    DateOnly LoanDate,
    DateOnly DueDate,
    bool IsOverdue);

public record CategoryWithCount(string Text, int Count);

public record Borrower(
    int PersonId,
    string Name,
    string SchoolClass,
    string Email,
    int OpenLoans);

/// <summary>Id/text pair for dropdowns.</summary>
public record LookupEntry(int Id, string Text);
