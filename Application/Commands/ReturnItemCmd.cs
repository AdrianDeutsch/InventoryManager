using System;

namespace Application.Commands;

/// <summary>Takes a lent item back. A damaged item stays blocked until staff releases it.</summary>
public record ReturnItemCmd(
    int ReservationId,
    int StaffId,
    DateOnly ReturnDate,
    bool Damaged);
