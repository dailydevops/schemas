namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Harness;

/// <summary>The exception thrown when a golden document source is not backed by a pinned suite or names no file inside it.</summary>
public sealed class GoldenSourceException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="GoldenSourceException"/> class.</summary>
    public GoldenSourceException() { }

    /// <summary>Initializes a new instance of the <see cref="GoldenSourceException"/> class.</summary>
    /// <param name="message">The error message.</param>
    public GoldenSourceException(string message)
        : base(message) { }

    /// <summary>Initializes a new instance of the <see cref="GoldenSourceException"/> class.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The underlying exception.</param>
    public GoldenSourceException(string message, Exception innerException)
        : base(message, innerException) { }
}
