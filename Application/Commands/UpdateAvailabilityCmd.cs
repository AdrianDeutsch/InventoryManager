namespace Application.Commands;

/// <summary>Blocks an item (e.g. for repair) or releases it for lending again.</summary>
public record UpdateAvailabilityCmd(
    int Id,
    bool IsAvailable);
