namespace NetEvolve.Schemas.Resources.Default;

/// <summary>The outcome of <see cref="UriReference.Resolve"/>: an absolute ASCII URI, or a diagnostic code.</summary>
internal readonly struct UriResolution
{
    internal UriResolution(string? uri, DiagnosticCode code)
    {
        Uri = uri;
        Code = code;
    }

    /// <summary>Gets the absolute URI, <see langword="null"/> on failure.</summary>
    internal string? Uri { get; }

    /// <summary>Gets the failure code, <see langword="default"/> on success.</summary>
    internal DiagnosticCode Code { get; }
}
