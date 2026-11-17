namespace NetEvolve.Schemas.Xml.Datatypes.QNames;

using System;
using System.Runtime.InteropServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Text.CharClasses;

/// <summary>
/// The value of <c>QName</c> and <c>NOTATION</c> (XSD 1.1 Part 2, sections 3.3.18 and 3.3.19): a namespace name and a local name.
/// The prefix is not part of the value, so two prefixes bound to one namespace give equal values.
/// </summary>
/// <remarks>
/// Limitation: <c>NOTATION</c> shares this value type, and a NOTATION value is not checked against declared notations (XSD-14).
/// The names are checked as XML 1.0 Fifth Edition NCNames.
/// </remarks>
[StructLayout(LayoutKind.Auto)]
public readonly struct XsdQName : IEquatable<XsdQName>
{
    internal XsdQName(string? namespaceName, string localName)
    {
        NamespaceName = namespaceName;
        LocalName = localName;
    }

    /// <summary>Gets the namespace name, <see langword="null"/> when the name is in no namespace.</summary>
    public string? NamespaceName { get; }

    /// <summary>Gets the local name.</summary>
    public string LocalName { get; }

    /// <summary>Parses a QName or NOTATION without whitespace handling and resolves its prefix.</summary>
    /// <param name="lexical">The lexical form, <c>prefix:local</c> or <c>local</c>.</param>
    /// <param name="context">The namespace declarations in scope.</param>
    /// <returns>The value, or the error with its offset; an unbound prefix is <see cref="DatatypeError.UnboundPrefix"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    public static ParseResult<XsdQName> Parse(ReadOnlySpan<char> lexical, INamespaceContext context)
    {
        Argument.ThrowIfNull(context);
        var error = Scan(lexical, out var colon, out var offset);
        if (error != DatatypeError.None)
        {
            return new ParseResult<XsdQName>(error, offset);
        }

        var prefix = colon < 0 ? default : lexical.Slice(0, colon);
        var found = context.TryResolvePrefix(prefix, out var namespaceName);
        if (colon >= 0 && (!found || string.IsNullOrEmpty(namespaceName)))
        {
            return new ParseResult<XsdQName>(DatatypeError.UnboundPrefix, 0);
        }

        var local = lexical.Slice(colon + 1).ToString();
        return new ParseResult<XsdQName>(
            new XsdQName(found && !string.IsNullOrEmpty(namespaceName) ? namespaceName : null, local)
        );
    }

    /// <summary>Checks the shape of a lexical form: one or two NCNames separated by a single colon.</summary>
    /// <param name="lexical">The lexical form.</param>
    /// <param name="colon">The offset of the colon, -1 when there is none.</param>
    /// <param name="offset">The offset of the error.</param>
    /// <returns><see cref="DatatypeError.None"/> when the form is well formed.</returns>
    internal static DatatypeError Scan(ReadOnlySpan<char> lexical, out int colon, out int offset)
    {
        colon = lexical.IndexOf(':');
        offset = 0;
        if (colon >= 0)
        {
            var error = ScanNCName(lexical.Slice(0, colon), 0, out offset);
            if (error != DatatypeError.None)
            {
                return error;
            }
        }

        return ScanNCName(lexical.Slice(colon + 1), colon + 1, out offset);
    }

    private static DatatypeError ScanNCName(ReadOnlySpan<char> name, int start, out int offset)
    {
        offset = start;
        if (name.IsEmpty)
        {
            return DatatypeError.InvalidLexicalForm;
        }

        // A Name admits colons, an NCName does not: a second colon ends the name.
        var colon = name.IndexOf(':');
        var length = XmlCharClass.NameLength(colon < 0 ? name : name.Slice(0, colon));
        offset = start + length;
        return length < name.Length ? DatatypeError.InvalidCharacter : DatatypeError.None;
    }

    /// <summary>Gets the expanded name: <c>{namespace}local</c>, or the local name alone.</summary>
    /// <returns>The expanded name.</returns>
    public override string ToString() => NamespaceName is null ? LocalName : "{" + NamespaceName + "}" + LocalName;

    /// <inheritdoc />
    public bool Equals(XsdQName other) =>
        string.Equals(NamespaceName, other.NamespaceName, StringComparison.Ordinal)
        && string.Equals(LocalName, other.LocalName, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is XsdQName other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        unchecked(
            (StringComparer.Ordinal.GetHashCode(NamespaceName ?? string.Empty) * 397)
            ^ StringComparer.Ordinal.GetHashCode(LocalName ?? string.Empty)
        );

    /// <summary>Determines whether two values are equal.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> when equal.</returns>
    public static bool operator ==(XsdQName left, XsdQName right) => left.Equals(right);

    /// <summary>Determines whether two values differ.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> when not equal.</returns>
    public static bool operator !=(XsdQName left, XsdQName right) => !left.Equals(right);
}
