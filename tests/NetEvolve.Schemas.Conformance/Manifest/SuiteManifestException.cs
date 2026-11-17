namespace NetEvolve.Schemas.Conformance.Manifest;

/// <summary>The exception thrown when a suite manifest is missing, malformed, or ambiguous.</summary>
public sealed class SuiteManifestException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="SuiteManifestException"/> class.</summary>
    public SuiteManifestException() { }

    /// <summary>Initializes a new instance of the <see cref="SuiteManifestException"/> class.</summary>
    /// <param name="message">The error message.</param>
    public SuiteManifestException(string message)
        : base(message) { }

    /// <summary>Initializes a new instance of the <see cref="SuiteManifestException"/> class.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The underlying exception.</param>
    public SuiteManifestException(string message, Exception innerException)
        : base(message, innerException) { }
}
