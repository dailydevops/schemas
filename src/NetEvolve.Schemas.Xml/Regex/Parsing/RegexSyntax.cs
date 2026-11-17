namespace NetEvolve.Schemas.Xml.Regex.Parsing;

using System.Runtime.InteropServices;
using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Text.CharClasses;

/// <summary>The parse-time parameters of a pattern: the dialect and the name edition behind <c>\i \I \c \C</c>.</summary>
/// <remarks><see langword="default"/> is the XSD dialect with the Second Edition name characters, the XSD 1.0 mode.</remarks>
[StructLayout(LayoutKind.Auto)]
public readonly record struct RegexSyntax
{
    /// <summary>Initializes a new instance of the <see cref="RegexSyntax"/> struct.</summary>
    /// <param name="dialect">The dialect; only <see cref="RegexDialect.Xsd"/> is parsed until DT-14 adds the XPath additions.</param>
    /// <param name="names">The name edition: the Second Edition for the XSD 1.0 mode, the Fifth Edition for the XSD 1.1 mode.</param>
    public RegexSyntax(RegexDialect dialect, NameEdition names)
    {
        Dialect = dialect;
        Names = names;
    }

    /// <summary>Gets the dialect.</summary>
    public RegexDialect Dialect { get; }

    /// <summary>Gets the name edition behind <c>\i \I \c \C</c>.</summary>
    public NameEdition Names { get; }
}
