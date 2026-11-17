namespace NetEvolve.Schemas.Conformance.Baseline;

/// <summary>The exception thrown when a baseline file has a blank, unsorted, or duplicate case id.</summary>
public sealed class BaselineFormatException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="BaselineFormatException"/> class.</summary>
    public BaselineFormatException() { }

    /// <summary>Initializes a new instance of the <see cref="BaselineFormatException"/> class.</summary>
    /// <param name="message">The error message.</param>
    public BaselineFormatException(string message)
        : base(message) { }

    /// <summary>Initializes a new instance of the <see cref="BaselineFormatException"/> class.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The underlying exception.</param>
    public BaselineFormatException(string message, Exception innerException)
        : base(message, innerException) { }
}
