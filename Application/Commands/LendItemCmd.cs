using System;

namespace Application.Commands;

/// <summary>Hands an available item out to a person.</summary>
public record LendItemCmd(
    int InventoryItemId,
    int PersonId,
    DateOnly LoanDate);
