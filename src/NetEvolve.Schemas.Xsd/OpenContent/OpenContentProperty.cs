namespace NetEvolve.Schemas.Xsd.OpenContent;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>
/// The Open Content property record of a content type (Structures 1.1, 3.4.1): a mode and the wildcard of the elements that
/// the content model accepts in addition to its own. Immutable.
/// </summary>
internal sealed class OpenContentProperty
{
    private OpenContentProperty(OpenContentMode mode, IWildcard wildcard)
    {
        Mode = mode;
        Wildcard = wildcard;
    }

    /// <summary>Gets the mode: <see cref="OpenContentMode.Interleave"/> or <see cref="OpenContentMode.Suffix"/>.</summary>
    public OpenContentMode Mode { get; }

    /// <summary>Gets the wildcard of the elements that open content accepts.</summary>
    public IWildcard Wildcard { get; }

    /// <summary>Creates the record.</summary>
    /// <param name="mode">The mode, <see cref="OpenContentMode.Interleave"/> or <see cref="OpenContentMode.Suffix"/>.</param>
    /// <param name="wildcard">The wildcard.</param>
    /// <returns>The record.</returns>
    public static OpenContentProperty Create(OpenContentMode mode, IWildcard wildcard)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan((int)mode, (int)OpenContentMode.Interleave);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)mode, (int)OpenContentMode.Suffix);
        ArgumentNullException.ThrowIfNull(wildcard);

        return new(mode, wildcard);
    }
}
