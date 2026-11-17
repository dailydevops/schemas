namespace NetEvolve.Schemas.XPath.Abstractions;

using NetEvolve.Schemas.XPath.Api;

/// <summary>Resolves a decimal format by name; the host supplies it through <see cref="XPathHostSlots.DecimalFormats"/>.</summary>
public interface IXPathDecimalFormatProvider
{
    /// <summary>Resolves a decimal format.</summary>
    /// <param name="name">The format name.</param>
    /// <param name="format">The format, <see langword="null"/> when the name is unknown.</param>
    /// <returns><see langword="true"/> when the format exists.</returns>
    bool TryGetDecimalFormat(XPathQName name, out XPathDecimalFormat? format);
}
