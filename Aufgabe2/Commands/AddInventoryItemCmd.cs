using System;
using System.ComponentModel.DataAnnotations;
namespace Aufgabe2.Commands;

// TODO: Add validation
public record AddInventoryItemCmd(
    string SerialNumber,
    string ItemName,
    int CategoryId,
    bool IsAvailable
    );
