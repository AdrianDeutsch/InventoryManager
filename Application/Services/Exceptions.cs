using System;

namespace Application.Services;

/// <summary>
/// A request was understood but violates a business rule. The message is written for the
/// person using the app, so the UI can show it as it is. Technical failures never use this type.
/// </summary>
public abstract class BusinessRuleException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public class InventoryServiceException(string message, Exception? innerException = null)
    : BusinessRuleException(message, innerException);

public class InventoryNotFoundException(string message)
    : InventoryServiceException(message);

public class ReservationException(string message, Exception? innerException = null)
    : BusinessRuleException(message, innerException);
