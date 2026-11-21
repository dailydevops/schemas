namespace NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

/// <summary>The deviation list is malformed.</summary>
internal sealed class DeviationFormatException : Exception
{
    public DeviationFormatException() { }

    public DeviationFormatException(string message)
        : base(message) { }

    public DeviationFormatException(string message, Exception innerException)
        : base(message, innerException) { }
}
