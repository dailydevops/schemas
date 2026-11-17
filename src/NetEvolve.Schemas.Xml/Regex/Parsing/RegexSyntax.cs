namespace NetEvolve.Schemas.Xml.Regex.Parsing;

using System.Runtime.InteropServices;
using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Regex.XPathDialect;
using NetEvolve.Schemas.Xml.Text.CharClasses;

/// <summary>The parse-time parameters of a pattern: the dialect, the name edition behind <c>\i \I \c \C</c>, and the XPath flags.</summary>
/// <remarks><see langword="default"/> is the XSD dialect with the Second Edition name characters and no flags, the XSD 1.0 mode.</remarks>
[StructLayout(LayoutKind.Auto)]
public readonly record struct RegexSyntax
{
    /// <summary>Initializes a new instance of the <see cref="RegexSyntax"/> struct without flags.</summary>
    /// <param name="dialect">The dialect.</param>
    /// <param name="names">The name edition: the Second Edition for the XSD 1.0 mode, the Fifth Edition for the XSD 1.1 mode.</param>
    public RegexSyntax(RegexDialect dialect, NameEdition names)
        : this(dialect, names, RegexFlagSet.None) { }

    /// <summary>Initializes a new instance of the <see cref="RegexSyntax"/> struct.</summary>
    /// <param name="dialect">The dialect.</param>
    /// <param name="names">The name edition: the Second Edition for the XSD 1.0 mode, the Fifth Edition for the XSD 1.1 mode.</param>
    /// <param name="flags">The flags; the XSD dialect has none, so any flag there is <see cref="RegexError.ConstructNotAllowed"/>.</param>
    public RegexSyntax(RegexDialect dialect, NameEdition names, RegexFlagSet flags)
    {
        Dialect = dialect;
        Names = names;
        Flags = flags;
    }

    /// <summary>Gets the dialect.</summary>
    public RegexDialect Dialect { get; }

    /// <summary>Gets the name edition behind <c>\i \I \c \C</c>.</summary>
    public NameEdition Names { get; }

    /// <summary>Gets the flags of the XPath dialect.</summary>
    public RegexFlagSet Flags { get; }
}
