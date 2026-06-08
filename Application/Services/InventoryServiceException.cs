using System;

namespace Application.Services;

[Serializable]
public class InventoryServiceException : Exception
{
    public InventoryServiceException()
    {
    }

    public InventoryServiceException(string? message) : base(message)
    {
    }

    public InventoryServiceException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}