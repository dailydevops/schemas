namespace NetEvolve.Schemas.XPath.Formatting.Numbers;

using System;
using NetEvolve.Schemas.Xml.Datatypes.QNames;
using NetEvolve.Schemas.Xml.Text.CharClasses;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// Reads the <c>decimal-format-name</c> argument of <c>format-number</c>: a lexical QName in XSLT 2.0 (16.4), and in Functions and Operators
/// 3.1 (4.7.2) an EQName after the leading and trailing whitespace is removed, that is a lexical QName or a <c>Q{uri}local</c> name. An
/// unprefixed name is in no namespace; the default namespace does not apply.
/// </summary>
/// <remarks>Limitation: a call does not reach the statically known namespaces (the gap of #752), so a prefixed name is resolved by the namespace context the function was built with, and by none in the default catalog, where it is invalid.</remarks>
internal static class DecimalFormatName
{
    /// <summary>Reads a name.</summary>
    /// <param name="text">The argument as written.</param>
    /// <param name="xslt20">Whether the XSLT 2.0 rules apply: a lexical QName only, and no whitespace is removed.</param>
    /// <param name="namespaces">The namespaces that resolve a prefix.</param>
    /// <param name="name">The expanded name.</param>
    /// <returns><see langword="true"/> when the text is a valid name with a bound prefix.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> or <paramref name="namespaces"/> is <see langword="null"/>.</exception>
    public static bool TryParse(string text, bool xslt20, INamespaceContext namespaces, out XPathQName name)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(namespaces);

        name = default;
        var trimmed = xslt20 ? text : text.Trim(' ', '\t', '\r', '\n');
        if (!xslt20 && trimmed.StartsWith("Q{", StringComparison.Ordinal))
        {
            var close = trimmed.AsSpan().IndexOf('}');
            var local = close < 0 ? string.Empty : trimmed.Substring(close + 1);
            if (close < 0 || !IsNCName(local))
            {
                return false;
            }

            name = new XPathQName(trimmed.Substring(2, close - 2), local);
            return true;
        }

        var parsed = XsdQName.Parse(trimmed.AsSpan(), namespaces);
        if (!parsed.Success)
        {
            return false;
        }

        name = new XPathQName(parsed.Value.NamespaceName ?? string.Empty, parsed.Value.LocalName);
        return true;
    }

    private static bool IsNCName(string text) =>
        text.Length > 0 && text.AsSpan().IndexOf(':') < 0 && XmlCharClass.NameLength(text.AsSpan()) == text.Length;
}
