namespace BaseModel.Model;

/// <summary>
/// Lifecycle of a loan. The states drive business rules (who may return what, when an item
/// becomes available again), so they live in code instead of a lookup table.
/// </summary>
public enum ReservationStatus
{
    /// <summary>The item was handed out and has not come back yet.</summary>
    Issued,

    /// <summary>The item came back in working condition.</summary>
    Returned,

    /// <summary>The item came back damaged and stays blocked until staff releases it again.</summary>
    DamagedReturn,
}
