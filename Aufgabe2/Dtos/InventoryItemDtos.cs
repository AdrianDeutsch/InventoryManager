using System;
using System.Collections.Generic;

namespace Aufgabe2.Dtos;

/// <summary>
/// Represents an inventory item including its reservations.
/// Used as the return type of <c>GET /inventoryItems</c>.
/// </summary>
public record InventoryItemWithReservationsDto(
    int Id,
    string SerialNumber,
    string ItemName,
    List<ReservationDto> Reservations);

/// <summary>
/// Represents a single reservation of an inventory item.
/// </summary>
public record ReservationDto(
    int Id,
    DateOnly LoanDate,
    DateOnly? ReturnDate,
    string PersonFirstName,
    string PersonLastName);

/// <summary>
/// Represents a single inventory item without its reservations.
/// Used as the return type of <c>GET /inventoryItems/{id}</c>.
/// </summary>
public record InventoryItemDto(int Id, string SerialNumber, string ItemName);
