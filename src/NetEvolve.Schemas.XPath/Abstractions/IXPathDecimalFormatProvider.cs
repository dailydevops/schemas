namespace NetEvolve.Schemas.XPath.Abstractions;

using NetEvolve.Schemas.XPath.Api;

/// <summary>Resolves a decimal format by name; the host supplies it through <see cref="XPathHostSlots.DecimalFormats"/>.</summary>
/// <remarks>
/// <c>format-number</c> asks for the unnamed decimal format with <see langword="default"/> of <see cref="XPathQName"/> (an empty namespace
/// name and an empty local name); a provider that has none answers <see langword="false"/> and the default format applies. A named format
/// is asked for by its expanded name, and an unprefixed name is in no namespace.
/// </remarks>
public interface IXPathDecimalFormatProvider
{
    /// <summary>Resolves a decimal format.</summary>
    /// <param name="name">The format name.</param>
    /// <param name="format">The format, <see langword="null"/> when the name is unknown.</param>
    /// <returns><see langword="true"/> when the format exists.</returns>
    bool TryGetDecimalFormat(XPathQName name, out XPathDecimalFormat? format);
}
