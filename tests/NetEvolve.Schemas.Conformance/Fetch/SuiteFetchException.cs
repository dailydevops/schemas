namespace NetEvolve.Schemas.Conformance.Fetch;

/// <summary>The exception thrown when a suite cannot be downloaded, verified, or extracted.</summary>
public sealed class SuiteFetchException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="SuiteFetchException"/> class.</summary>
    public SuiteFetchException() { }

    /// <summary>Initializes a new instance of the <see cref="SuiteFetchException"/> class.</summary>
    /// <param name="message">The error message.</param>
    public SuiteFetchException(string message)
        : base(message) { }

    /// <summary>Initializes a new instance of the <see cref="SuiteFetchException"/> class.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The underlying exception.</param>
    public SuiteFetchException(string message, Exception innerException)
        : base(message, innerException) { }
}
