namespace Aufgabe2.Commands;

public record UpdateAvailabilityCmd(
    int Id,
    bool IsAvailable);