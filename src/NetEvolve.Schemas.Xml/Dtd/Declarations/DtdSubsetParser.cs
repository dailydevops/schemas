namespace NetEvolve.Schemas.Xml.Dtd.Declarations;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.External.Subset;
using NetEvolve.Schemas.Xml.Dtd.Model;

/// <summary>
/// Parses the declarations of the internal subset (productions 28 and 29, 45 to 60, 70 to 76, 82 and 83) into a
/// <see cref="DtdDeclarations"/> model, and, when an <see cref="ExternalSubsetReader"/> is given, of the external subset and
/// the external parameter entities (productions 30 and 31, 61 to 65). The subset is one complete token, so the parser is
/// not resumable; chunking is already resolved by the tokenizer.
/// </summary>
/// <remarks>
/// Limitations: the external subset and the external parameter entities are read only when a reader is given (the options of
/// external access), otherwise they are skipped; a parameter-entity reference between declarations includes the replacement
/// text, which must hold complete declarations (WFC PE Between Declarations); a reference inside a declaration is
/// recognized in external text only (WFC PEs in Internal Subset), and in an entity value there it is included in the
/// literal; the nesting validity constraints of parameter entities and conditional sections (Proper Declaration/PE Nesting,
/// Proper Group/PE Nesting, Proper Conditional Section/PE Nesting) are not reported (XML-67, #608); a self-referencing
/// internal parameter entity ends in the depth limit <c>XML1110</c>, not <c>XML0020</c>; an IGNORE section must end in the
/// text that opened it; the external subset and an external parameter entity between declarations are read through a window
/// of one unit (a declaration, a comment, a processing instruction, a conditional section opener, a reference) that is
/// refilled chunk by chunk, so no external entity is buffered completely (design #122, D9 rule 5), and one unit is bounded
/// by <c>MaxDoctypeLength</c> (<c>XML1115</c>); an external parameter entity that is included inside a declaration or an
/// entity value is one unit with it and bounded the same way; the number of declarations is bounded by <c>MaxDeclarations</c> (<c>XML1119</c>), counted over both
/// subsets and every parsed replacement text; the first declaration of an element, attribute, entity, or notation binds, so the declarations of
/// the internal subset win over the external ones, and later ones are ignored without a report; an error is reported at the
/// document type declaration, not at the offending offset; a public or system identifier that is present but empty reads
/// as absent.
/// </remarks>
internal sealed partial class DtdSubsetParser
{
    private readonly DtdDeclarationBuilder _builder;
    private readonly XmlReaderLimits _limits;
    private readonly ExternalSubsetReader? _external;
    private string _text = string.Empty;
    private int _pos;
    private readonly int _maxDeclarations;
    private long _expanded;
    private int _declarations;
    private XmlErrorCode? _error;

    private DtdSubsetParser(DtdDeclarationBuilder builder, XmlReaderLimits limits, ExternalSubsetReader? external)
    {
        _builder = builder;
        _limits = limits;
        _external = external;
        _maxDeclarations = external?.MaxDeclarations ?? DtdValidationLimits.Default.MaxDeclarations;
    }

    /// <summary>Parses the internal subset of a document type declaration.</summary>
    /// <param name="doctype">The declaration; an absent identifier is empty.</param>
    /// <param name="limits">The limits.</param>
    /// <param name="declarations">The model, <see langword="null"/> when the subset is not well formed.</param>
    /// <returns><see langword="null"/> when the subset is well formed, otherwise the error code.</returns>
    internal static XmlErrorCode? Parse(
        in XmlDoctypeInfo doctype,
        XmlReaderLimits limits,
        out DtdDeclarations? declarations
    ) => Parse(in doctype, limits, null, null, out declarations);

    /// <summary>Parses the internal subset and, when <paramref name="external"/> is set, the external subset of a document type declaration.</summary>
    /// <param name="doctype">The declaration; an absent identifier is empty.</param>
    /// <param name="limits">The limits.</param>
    /// <param name="external">The reader of external text, <see langword="null"/> to leave the external subset and the external parameter entities unread.</param>
    /// <param name="baseUri">The absolute base URI of the document, or <see langword="null"/>.</param>
    /// <param name="declarations">The model, <see langword="null"/> when a subset is not well formed.</param>
    /// <returns><see langword="null"/> when the subsets are well formed, otherwise the error code.</returns>
    internal static XmlErrorCode? Parse(
        in XmlDoctypeInfo doctype,
        XmlReaderLimits limits,
        ExternalSubsetReader? external,
        string? baseUri,
        out DtdDeclarations? declarations
    )
    {
        Argument.ThrowIfNull(limits);

        var name = doctype.Name.ToString();
        var publicId = NullIfEmpty(doctype.PublicId);
        var systemId = NullIfEmpty(doctype.SystemId);
        var builder = new DtdDeclarationBuilder(name, publicId, systemId);
        var parser = new DtdSubsetParser(builder, limits, external);
        if (
            !parser.ParseSequence(doctype.InternalSubset.ToString(), 0, inExternal: false, baseUri, null)
            || (
                external is not null
                && systemId is not null
                && !parser.ParseExternalSubset(name, publicId, systemId, baseUri)
            )
        )
        {
            declarations = null;
            return parser._error;
        }

        declarations = builder.Build();
        return null;
    }

    private static string? NullIfEmpty(ReadOnlySpan<char> value) => value.IsEmpty ? null : value.ToString();

    // Parses `text` as a sequence of declarations (the subset itself, or the replacement text of a parameter entity) that
    // continues the context of the caller.
    private bool ParseSequence(string text, int depth) => ParseSequence(text, depth, _inExternal, _baseUri, null);

    // Parses `text` as a sequence of complete declarations; `inExternal` tells whether it is external text. The text of
    // `source` follows `text` in chunks: the window holds the unit that is parsed and is refilled from the source.
    private bool ParseSequence(string text, int depth, bool inExternal, string? baseUri, ExternalSubsetText? source)
    {
        var saved = SaveContext();
        _text = text;
        _pos = 0;
        _depth = depth;
        _inExternal = inExternal;
        _baseUri = baseUri;
        _frameBase = _frames.Count;
        _sectionDepth = 0;
        _source = source;
        var ok = ParseDeclarations() && _error is null && (_sectionDepth == 0 || Fail(XmlErrorCodes.XML0011));
        CloseFrames();
        RestoreContext(saved);

        return ok;
    }

    private bool ParseDeclarations()
    {
        while (true)
        {
            _ = SkipPlainSpaces();
            if ((Streaming && !EnsureUnit()) || _error is not null)
            {
                return false;
            }

            if (_pos >= _text.Length)
            {
                if (PopFrame())
                {
                    continue;
                }

                return true;
            }

            var ok = _text[_pos] switch
            {
                '%' => ParseReference(),
                _ when Match("<!--") => SkipComment(),
                _ when Match("<?") => SkipInstruction(),
                _ when Match("<!ELEMENT") => ParseElement(),
                _ when Match("<!ATTLIST") => ParseAttlist(),
                _ when Match("<!ENTITY") => ParseEntity(),
                _ when Match("<!NOTATION") => ParseNotation(),
                _ when _inExternal && Match("<![") => ParseConditionalSection(),
                _ when _inExternal && _sectionDepth > 0 && Match("]]>") => CloseSection(),
                _ => Fail(XmlErrorCodes.XML0011),
            };
            if (!ok)
            {
                return false;
            }
        }
    }

    // Production 69 between declarations: the replacement text is included as a sequence of complete declarations.
    private bool ParseReference()
    {
        _pos++;
        if (!ScanName(out var name))
        {
            return false;
        }

        if (_pos >= _text.Length || _text[_pos] != ';')
        {
            return FailHere();
        }

        _pos++;
        _builder.HasParameterEntityReferences = true;
        if (!_builder.TryGetParameterEntity(name, out var entity))
        {
            // Without an external subset the entity cannot be declared elsewhere (WFC Entity Declared).
            return _inExternal || _builder.HasExternalSubset || Fail(XmlErrorCodes.XML0008);
        }

        // An external entity is skipped when no reader is given: the external text is not read.
        if (entity.Value is null && _external is null)
        {
            return true;
        }

        if (Nesting + 1 > _limits.MaxEntityDepth)
        {
            return Fail(XmlErrorCodes.XML1110);
        }

        if (entity.Value is not null)
        {
            return Charge(entity.Value.Length) ? ParseSequence(entity.Value, Nesting + 1) : Fail(XmlErrorCodes.XML1109);
        }

        ExternalSubsetText? opened = null;
        try
        {
            return TryOpen(entity, out opened)
                && ParseSequence(string.Empty, Nesting + 1, inExternal: true, opened!.BaseUri, opened);
        }
        finally
        {
            opened?.Dispose();
        }
    }

    // Production 15; `<!--` is consumed.
    private bool SkipComment()
    {
        var end = _text.IndexOf("--", _pos, StringComparison.Ordinal);
        if (end < 0 || end + 2 >= _text.Length)
        {
            return Fail(XmlErrorCodes.XML0011);
        }

        if (_text[end + 2] != '>')
        {
            return Fail(XmlErrorCodes.XML0014);
        }

        _pos = end + 3;
        return true;
    }

    // Production 16; `<?` is consumed.
    private bool SkipInstruction()
    {
        if (!ScanName(out var target))
        {
            return false;
        }

        if (string.Equals(target, "xml", StringComparison.OrdinalIgnoreCase))
        {
            return Fail(XmlErrorCodes.XML0015);
        }

        var end = _text.IndexOf("?>", _pos, StringComparison.Ordinal);
        if (end < 0 || (end > _pos && !IsSpace(_text[_pos])))
        {
            return Fail(XmlErrorCodes.XML0011);
        }

        _pos = end + 2;
        return true;
    }

    // The end of every declaration: optional white space and `>`.
    private bool ExpectClose()
    {
        _ = SkipSpaces();
        if (_pos < _text.Length && _text[_pos] == '>')
        {
            _pos++;
            return true;
        }

        return FailHere();
    }
}
