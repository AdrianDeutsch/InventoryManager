using System;

namespace Application.Services;

[Serializable]
public class InventoryNotFoundException : InventoryServiceException
{
    public InventoryNotFoundException()
    {
    }

    public InventoryNotFoundException(string? message) : base(message)
    {
    }

    public InventoryNotFoundException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}