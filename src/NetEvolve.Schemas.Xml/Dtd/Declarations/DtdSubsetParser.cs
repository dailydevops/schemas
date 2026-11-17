namespace NetEvolve.Schemas.Xml.Dtd.Declarations;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;

/// <summary>
/// Parses the declarations of the internal subset (productions 28 and 29, 45 to 60, 70 to 76, 82 and 83) into a
/// <see cref="DtdDeclarations"/> model. The subset is one complete token, so the parser is not resumable; chunking is
/// already resolved by the tokenizer.
/// </summary>
/// <remarks>
/// Limitations: only the internal subset is read (the external subset, parameter entities inside declarations, and
/// conditional sections are XML-20); a parameter-entity reference between declarations includes the replacement text of an
/// internal entity and skips an external one; markup declarations in a replacement text must be complete; the first
/// declaration of an element, attribute, entity, or notation binds and later ones are ignored without a report; an error is
/// reported at the document type declaration, not at the offending offset; a public or system identifier that is present but
/// empty reads as absent.
/// </remarks>
internal sealed partial class DtdSubsetParser
{
    private readonly DtdDeclarationBuilder _builder;
    private readonly XmlReaderLimits _limits;
    private string _text = string.Empty;
    private int _pos;
    private long _expanded;
    private XmlErrorCode? _error;

    private DtdSubsetParser(DtdDeclarationBuilder builder, XmlReaderLimits limits)
    {
        _builder = builder;
        _limits = limits;
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
    )
    {
        var builder = new DtdDeclarationBuilder(
            doctype.Name.ToString(),
            NullIfEmpty(doctype.PublicId),
            NullIfEmpty(doctype.SystemId)
        );
        var parser = new DtdSubsetParser(builder, limits);
        if (!parser.ParseSequence(doctype.InternalSubset.ToString(), 0))
        {
            declarations = null;
            return parser._error;
        }

        declarations = builder.Build();
        return null;
    }

    private static string? NullIfEmpty(ReadOnlySpan<char> value) => value.IsEmpty ? null : value.ToString();

    // Parses `text` as a sequence of declarations (the subset itself, or the replacement text of a parameter entity).
    private bool ParseSequence(string text, int depth)
    {
        var (savedText, savedPosition) = (_text, _pos);
        _text = text;
        _pos = 0;
        var ok = ParseDeclarations(depth);
        (_text, _pos) = (savedText, savedPosition);

        return ok;
    }

    private bool ParseDeclarations(int depth)
    {
        while (true)
        {
            _ = SkipSpaces();
            if (_pos >= _text.Length)
            {
                return true;
            }

            var ok = _text[_pos] switch
            {
                '%' => ParseReference(depth),
                _ when Match("<!--") => SkipComment(),
                _ when Match("<?") => SkipInstruction(),
                _ when Match("<!ELEMENT") => ParseElement(),
                _ when Match("<!ATTLIST") => ParseAttlist(),
                _ when Match("<!ENTITY") => ParseEntity(),
                _ when Match("<!NOTATION") => ParseNotation(),
                _ => Fail(XmlErrorCodes.XML0011),
            };
            if (!ok)
            {
                return false;
            }
        }
    }

    // Production 69 between declarations: the replacement text of an internal entity is included, an external one is skipped.
    private bool ParseReference(int depth)
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
            return _builder.HasExternalSubset || Fail(XmlErrorCodes.XML0008);
        }

        if (entity.Value is null)
        {
            return true;
        }

        if (depth + 1 > _limits.MaxEntityDepth)
        {
            return Fail(XmlErrorCodes.XML1110);
        }

        _expanded += entity.Value.Length;
        return _expanded <= _limits.MaxEntityExpansionCharacters
            ? ParseSequence(entity.Value, depth + 1)
            : Fail(XmlErrorCodes.XML1109);
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
