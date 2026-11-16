using Radzen;

namespace WebApp.Components.Shared;

/// <summary>
/// Reports a failure that is not a business rule (database unreachable, a bug). The details
/// go to the log, where someone can act on them. The user gets a neutral message: an exception
/// text would not help them and may reveal internals.
/// </summary>
public class TechnicalErrorNotifier(NotificationService notifications, ILogger<TechnicalErrorNotifier> logger)
{
    public const string Advice = "Please try again. If it keeps happening, the server log has the details.";

    public void Report(Exception exception, string whatFailed)
    {
        logger.LogError(exception, "{WhatFailed}", whatFailed);
        notifications.Notify(NotificationSeverity.Error, whatFailed, Advice);
    }
}
