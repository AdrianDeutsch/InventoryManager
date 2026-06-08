namespace Application.Commands;

public record UpdateAvailabilityCmd(
    int Id,
    bool IsAvailable);