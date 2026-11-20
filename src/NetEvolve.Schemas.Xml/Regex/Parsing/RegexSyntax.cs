namespace NetEvolve.Schemas.Xml.Regex.Parsing;

using System.Runtime.InteropServices;
using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Regex.XPathDialect;
using NetEvolve.Schemas.Xml.Text.CharClasses;

/// <summary>The parse-time parameters of a pattern: the dialect, the name edition behind <c>\i \I \c \C</c>, and the flags.</summary>
/// <remarks><see langword="default"/> is the XSD dialect with the Second Edition name characters, the XSD 1.0 mode, and no flags. The XSD dialect has no flags.</remarks>
[StructLayout(LayoutKind.Auto)]
public readonly record struct RegexSyntax
{
    /// <summary>Initializes a new instance of the <see cref="RegexSyntax"/> struct without flags.</summary>
    /// <param name="dialect">The dialect.</param>
    /// <param name="names">The name edition: the Second Edition for the XSD 1.0 mode, the Fifth Edition for the XSD 1.1 mode.</param>
    public RegexSyntax(RegexDialect dialect, NameEdition names)
        : this(dialect, names, RegexOptions.None) { }

    /// <summary>Initializes a new instance of the <see cref="RegexSyntax"/> struct.</summary>
    /// <param name="dialect">The dialect.</param>
    /// <param name="names">The name edition: the Second Edition for the XSD 1.0 mode, the Fifth Edition for the XSD 1.1 mode.</param>
    /// <param name="options">The options; only the XPath dialect has flags, any flag with the XSD dialect is <see cref="RegexError.InvalidFlag"/>.</param>
    public RegexSyntax(RegexDialect dialect, NameEdition names, RegexOptions options)
    {
        Dialect = dialect;
        Names = names;
        Options = options;
    }

    /// <summary>Gets the dialect.</summary>
    public RegexDialect Dialect { get; }

    /// <summary>Gets the name edition behind <c>\i \I \c \C</c>.</summary>
    public NameEdition Names { get; }

    /// <summary>Gets the flags.</summary>
    public RegexOptions Options { get; }
}
