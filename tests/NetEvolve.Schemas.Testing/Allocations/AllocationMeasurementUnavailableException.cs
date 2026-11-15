namespace NetEvolve.Schemas.Testing.Allocations;

/// <summary>
/// Thrown when per-thread allocation measurement is unavailable, so the assertion fails explicitly
/// instead of passing silently.
/// </summary>
public sealed class AllocationMeasurementUnavailableException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="AllocationMeasurementUnavailableException"/> class.</summary>
    public AllocationMeasurementUnavailableException() { }

    /// <summary>Initializes a new instance of the <see cref="AllocationMeasurementUnavailableException"/> class.</summary>
    /// <param name="message">The failure message.</param>
    public AllocationMeasurementUnavailableException(string message)
        : base(message) { }

    /// <summary>Initializes a new instance of the <see cref="AllocationMeasurementUnavailableException"/> class.</summary>
    /// <param name="message">The failure message.</param>
    /// <param name="innerException">The exception raised by the meter.</param>
    public AllocationMeasurementUnavailableException(string message, Exception innerException)
        : base(message, innerException) { }
}
