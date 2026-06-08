using System;
using System.ComponentModel.DataAnnotations;
namespace Application.Commands;

// TODO: Add validation
public record AddInventoryItemCmd(
    string SerialNumber,
    string ItemName,
    int CategoryId,
    bool IsAvailable
    );
