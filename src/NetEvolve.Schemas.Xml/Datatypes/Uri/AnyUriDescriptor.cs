namespace NetEvolve.Schemas.Xml.Datatypes.Uri;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.Strings;
using NetEvolve.Schemas.Xml.Text.CharClasses;

/// <summary>
/// The built-in <c>anyURI</c> type (XSD 1.1 Part 2, section 3.3.17; XSD 1.0 Part 2, anyURI): an <see cref="XsdString"/> with
/// whitespace collapse and no <see cref="System.Uri"/>. The versions differ in <see cref="Basis"/> only.
/// </summary>
/// <remarks>
/// Limitation: the lexical check is lax in both versions: every string of XML <c>Char</c> is accepted, so a malformed reference
/// such as <c>http://[</c> validates. <see cref="AnyUriGrammar.Strict"/> is recorded but not implemented (#411, #412). The
/// descriptor has no base type, because a primitive descriptor does not take one.
/// </remarks>
public sealed class AnyUriDescriptor : IDatatypeDescriptor<XsdString>
{
    private readonly AtomicDescriptor<XsdString> _inner;

    /// <summary>Initializes a new instance of the <see cref="AnyUriDescriptor"/> class.</summary>
    /// <param name="version">The XSD version whose rules apply.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/> is not a defined version.</exception>
    public AnyUriDescriptor(XsdVersion version)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan((int)version, (int)XsdVersion.V10);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)version, (int)XsdVersion.V11);

        _inner = new("anyURI", version, new AnyUriMapping());
        Basis = version == XsdVersion.V10 ? AnyUriBasis.Rfc2396 : AnyUriBasis.Rfc3987;
        Grammar = AnyUriGrammar.Lax;
    }

    /// <summary>Gets the specification that defines the lexical space in this version.</summary>
    public AnyUriBasis Basis { get; }

    /// <summary>Gets the strictness of the lexical check; always <see cref="AnyUriGrammar.Lax"/> at present.</summary>
    public AnyUriGrammar Grammar { get; }

    /// <inheritdoc />
    public string Name => _inner.Name;

    /// <inheritdoc />
    public DatatypeVariety Variety => _inner.Variety;

    /// <inheritdoc />
    public IDatatypeDescriptor? BaseType => _inner.BaseType;

    /// <inheritdoc />
    public XsdVersion LexicalMode => _inner.LexicalMode;

    /// <inheritdoc />
    public bool IsOrdered => _inner.IsOrdered;

    /// <inheritdoc />
    public IReadOnlyList<IFacet> Facets => _inner.Facets;

    /// <inheritdoc />
    public ValidationResult Validate(ReadOnlySpan<char> lexical) => _inner.Validate(lexical);

    /// <inheritdoc />
    public ParseResult<XsdString> Parse(ReadOnlySpan<char> lexical) => _inner.Parse(lexical);

    /// <inheritdoc />
    public XsdOrder Compare(in XsdString a, in XsdString b) => _inner.Compare(in a, in b);

    /// <inheritdoc />
    public bool IsIdentical(in XsdString a, in XsdString b) => _inner.IsIdentical(in a, in b);

    private sealed class AnyUriMapping : ILexicalMapping<XsdString>
    {
        public WhitespaceMode Whitespace => WhitespaceMode.Collapse;

        public bool IsOrdered => false;

        public DatatypeError Check(ReadOnlySpan<char> normalized, XsdVersion version, out int errorOffset)
        {
            errorOffset = XmlCharClass.IndexOfNonChar(normalized);
            if (errorOffset >= 0)
            {
                return DatatypeError.InvalidCharacter;
            }

            errorOffset = 0;
            return DatatypeError.None;
        }

        public XsdString Map(ReadOnlySpan<char> normalized, WhitespaceMode mode, XsdVersion version) =>
            new(new TextRange(0, normalized.Length), mode);

        // Values are views; equality needs the spans and lives on XsdString.
        public XsdOrder Compare(in XsdString a, in XsdString b, XsdVersion version) => XsdOrder.Incomparable;

        public bool IsIdentical(in XsdString a, in XsdString b, XsdVersion version) => false;
    }
}
