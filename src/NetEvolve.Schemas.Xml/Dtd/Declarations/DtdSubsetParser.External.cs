namespace NetEvolve.Schemas.Xml.Dtd.Declarations;

using System;
using System.Collections.Generic;
using System.Text;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.External.Subset;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Text.CharClasses;

// The part of the declaration parser that reads external text (XML-20, #291). It lives with the external subset code and
// extends DtdSubsetParser: the external subset and the external parameter entities, parameter-entity references inside
// declarations (section 4.4.8), parameter-entity references inside entity values (section 4.4.5), and conditional sections
// (section 3.4).
internal sealed partial class DtdSubsetParser
{
    // The frames of the parameter entities that are included inside a declaration; each one holds the text to return to and
    // the entity that was read for the included text, which closes when the frame returns.
    private readonly List<(
        string Text,
        int Position,
        bool InExternal,
        string? BaseUri,
        ExternalSubsetText? ChildOwner
    )> _frames = [];

    // The parameter entities whose replacement text is open, outermost first (WFC No Recursion).
    private readonly List<DtdEntityDeclaration> _open = [];
    private string? _baseUri;
    private int _depth;
    private int _frameBase;
    private int _sectionDepth;
    private bool _inExternal;

    // The rest of the external entity whose text is parsed as a sequence: the window `_text` holds the unit that is parsed.
    private ExternalSubsetText? _source;

    // The number of texts that are open around the current position: the sequences and the frames of this sequence.
    private int Nesting => _depth + _frames.Count - _frameBase;

    // The window is refilled from the source only while no frame is open: a frame holds a complete text of its own.
    private bool Streaming => _source is not null && _frames.Count == _frameBase;

    private (
        string Text,
        int Position,
        int Depth,
        bool InExternal,
        string? BaseUri,
        int FrameBase,
        int SectionDepth,
        ExternalSubsetText? Source
    ) SaveContext() => (_text, _pos, _depth, _inExternal, _baseUri, _frameBase, _sectionDepth, _source);

    private void RestoreContext(
        (
            string Text,
            int Position,
            int Depth,
            bool InExternal,
            string? BaseUri,
            int FrameBase,
            int SectionDepth,
            ExternalSubsetText? Source
        ) saved
    ) => (_text, _pos, _depth, _inExternal, _baseUri, _frameBase, _sectionDepth, _source) = saved;

    private bool IsOpen(DtdEntityDeclaration entity)
    {
        for (var i = 0; i < _open.Count; i++)
        {
            if (ReferenceEquals(_open[i], entity))
            {
                return true;
            }
        }

        return false;
    }

    // Counts one element, attribute, entity, or notation declaration against the limit of the document (design #122, section 9).
    private bool CountDeclaration() => ++_declarations <= _maxDeclarations || Fail(DtdLimitCodes.XML1119);

    // Charges the characters of an internal replacement text to the one budget of the document.
    private bool Charge(int count)
    {
        if (_external is not null)
        {
            return _external.TryChargeExpansion(count);
        }

        _expanded += count;
        return _expanded <= _limits.MaxEntityExpansionCharacters;
    }

    private bool TryOpen(DtdEntityDeclaration entity, out ExternalSubsetText? opened) =>
        _external!.TryOpen(
            ResourceKind.ExternalParameterEntity,
            entity.Name,
            entity.PublicId,
            entity.SystemId!,
            entity.BaseUri,
            out opened,
            out var error
        ) || Fail(error!);

    // Productions 30 and 31: the external subset follows the internal one, so its declarations bind later.
    private bool ParseExternalSubset(string name, string? publicId, string systemId, string? baseUri)
    {
        ExternalSubsetText? opened = null;
        try
        {
            if (
                !_external!.TryOpen(
                    ResourceKind.ExternalSubset,
                    name,
                    publicId,
                    systemId,
                    baseUri,
                    out opened,
                    out var error
                )
            )
            {
                return Fail(error!);
            }

            return ParseSequence(string.Empty, 0, inExternal: true, opened!.BaseUri, opened);
        }
        finally
        {
            opened?.Dispose();
        }
    }

    // Leaves the frames that an error left open; their entities close.
    private void CloseFrames()
    {
        for (var i = _frames.Count - 1; i >= _frameBase; i--)
        {
            _frames[i].ChildOwner?.Dispose();
            _open.RemoveAt(_open.Count - 1);
        }

        _frames.RemoveRange(_frameBase, _frames.Count - _frameBase);
    }

    // Returns to the text that included the current one; false when this sequence has no open frame.
    private bool PopFrame()
    {
        if (_frames.Count <= _frameBase)
        {
            return false;
        }

        var frame = _frames[_frames.Count - 1];
        _frames.RemoveAt(_frames.Count - 1);
        _open.RemoveAt(_open.Count - 1);
        (_text, _pos, _inExternal, _baseUri) = (frame.Text, frame.Position, frame.InExternal, frame.BaseUri);
        frame.ChildOwner?.Dispose();

        return true;
    }

    // White space inside a declaration of external text: references are included, the end of a frame returns to its parent.
    private bool SkipIncludedSpaces(bool any)
    {
        while (_error is null)
        {
            if (_pos >= _text.Length)
            {
                if (!PopFrame())
                {
                    break;
                }
            }
            else if (_text[_pos] == '%' && StartsReference())
            {
                if (!TryEnterReference())
                {
                    break;
                }
            }
            else
            {
                break;
            }

            any = true;
            _ = SkipPlainSpaces();
        }

        return any;
    }

    // A percent sign that is followed by a name start character begins a reference; any other one is for the caller.
    private bool StartsReference() => _pos + 1 < _text.Length && XmlCharClass.NameLength(_text.AsSpan(_pos + 1)) > 0;

    // Production 69 inside a declaration; the reference is at the position. The replacement text becomes the current text
    // until its end; an undeclared entity is skipped (VC Entity Declared), an external one is read through the loader.
    private bool TryEnterReference()
    {
        _pos++;
        if (!ScanName(out var name))
        {
            return false;
        }

        if (!Current(';'))
        {
            return FailHere();
        }

        _pos++;
        _builder.HasParameterEntityReferences = true;
        if (!_builder.TryGetParameterEntity(name, out var entity))
        {
            return true;
        }

        if (IsOpen(entity))
        {
            return Fail(XmlErrorCodes.XML0020);
        }

        if (Nesting + 1 > _limits.MaxEntityDepth)
        {
            return Fail(XmlErrorCodes.XML1110);
        }

        return PushFrame(entity);
    }

    // The entity closes when the frame returns to its parent; an error closes it with the sequence.
    private bool PushFrame(DtdEntityDeclaration entity)
    {
        ExternalSubsetText? owner = null;
        try
        {
            var text = entity.Value;
            var baseUri = _baseUri;
            if (text is null)
            {
                if (!TryOpen(entity, out owner))
                {
                    return false;
                }

                if (!owner!.TryReadAll(_limits.MaxDoctypeLength, out text, out var readError))
                {
                    return Fail(readError!);
                }

                baseUri = owner.BaseUri;
            }
            else if (!Charge(text.Length))
            {
                return Fail(XmlErrorCodes.XML1109);
            }

            _frames.Add((_text, _pos, _inExternal, _baseUri, owner));
            _open.Add(entity);
            owner = null;
            (_text, _pos, _baseUri) = (text!, 0, baseUri);

            return true;
        }
        finally
        {
            owner?.Dispose();
        }
    }

    // Production 69 inside an entity value of external text (section 4.4.5): the replacement text is included in the
    // literal as if it were part of it. An undeclared entity includes nothing (VC Entity Declared).
    private bool IncludeParameterEntity(StringBuilder builder, string source, int percent, int level, out int next)
    {
        next = percent + 1;
        var length = XmlCharClass.NameLength(source.AsSpan(next));
        if (length == 0 || next + length >= source.Length || source[next + length] != ';')
        {
            return Fail(XmlErrorCodes.XML0011);
        }

        var name = source.Substring(next, length);
        next += length + 1;
        _builder.HasParameterEntityReferences = true;
        if (!_builder.TryGetParameterEntity(name, out var entity))
        {
            return true;
        }

        if (IsOpen(entity))
        {
            return Fail(XmlErrorCodes.XML0020);
        }

        var inner = level + 1;
        if (Nesting + inner > _limits.MaxEntityDepth)
        {
            return Fail(XmlErrorCodes.XML1110);
        }

        _open.Add(entity);
        var ok = IncludeEntityText(builder, entity, inner);
        _open.RemoveAt(_open.Count - 1);

        return ok;
    }

    private bool IncludeEntityText(StringBuilder builder, DtdEntityDeclaration entity, int inner)
    {
        if (entity.Value is not null)
        {
            return Charge(entity.Value.Length)
                ? AppendEntityValue(builder, entity.Value, inner)
                : Fail(XmlErrorCodes.XML1109);
        }

        ExternalSubsetText? opened = null;
        try
        {
            if (!TryOpen(entity, out opened))
            {
                return false;
            }

            return opened!.TryReadAll(_limits.MaxDoctypeLength, out var text, out var readError)
                ? AppendEntityValue(builder, text!, inner)
                : Fail(readError!);
        }
        finally
        {
            opened?.Dispose();
        }
    }

    // Productions 61 to 65; `<![` is consumed. The keyword may come from a parameter entity.
    private bool ParseConditionalSection()
    {
        _ = SkipSpaces();
        var include = Match("INCLUDE");
        if (!include && !Match("IGNORE"))
        {
            return Fail(XmlErrorCodes.XML0011);
        }

        _ = SkipSpaces();
        if (!Current('['))
        {
            return FailHere();
        }

        _pos++;
        if (!include)
        {
            return SkipIgnoredContents();
        }

        if (_sectionDepth >= _limits.MaxElementDepth)
        {
            return Fail(XmlErrorCodes.XML1102);
        }

        _sectionDepth++;
        return true;
    }

    // `]]>` of an INCLUDE section is consumed.
    private bool CloseSection()
    {
        _sectionDepth--;

        return true;
    }

    // Productions 64 and 65: the contents of an IGNORE section are not parsed; nested `<![` and `]]>` pairs balance. In a
    // streamed entity the section is skipped window by window: only the nesting level and the last two characters, which may
    // begin a marker, are carried across a refill.
    private bool SkipIgnoredContents()
    {
        var level = 1;
        var open = _text.IndexOf("<![", _pos, StringComparison.Ordinal);
        while (level > 0)
        {
            var close = _text.IndexOf("]]>", _pos, StringComparison.Ordinal);
            if (close < 0)
            {
                // The openers that are left in the window nest deeper; the closers are still to come.
                while (open >= 0)
                {
                    level++;
                    _pos = open + 3;
                    open = _text.IndexOf("<![", _pos, StringComparison.Ordinal);
                }

                if (!Streaming || _source!.IsEnd)
                {
                    return Fail(XmlErrorCodes.XML0011);
                }

                _pos = Math.Max(_pos, _text.Length - 2);
                if (!Refill())
                {
                    return false;
                }

                open = _text.IndexOf("<![", _pos, StringComparison.Ordinal);
                continue;
            }

            if (open >= 0 && open < close)
            {
                level++;
                _pos = open + 3;
            }
            else
            {
                level--;
                _pos = close + 3;
            }

            if (open >= 0 && open < _pos)
            {
                open = _text.IndexOf("<![", _pos, StringComparison.Ordinal);
            }
        }

        return true;
    }

    // Makes the window hold the whole unit that starts at the position (a declaration, a comment, a processing instruction,
    // the opener of a conditional section, a reference), or the end of the entity. The unit is bounded by MaxDoctypeLength
    // (XML1115, design #108 AP12): a window that holds more than that without a complete unit is an error.
    private bool EnsureUnit()
    {
        while (true)
        {
            _ = SkipPlainSpaces();
            if (_source!.IsEnd || IsUnitComplete(_text.AsSpan(_pos)))
            {
                return _error is null;
            }

            if (_text.Length - _pos >= _limits.MaxDoctypeLength)
            {
                return Fail(XmlErrorCodes.XML1115);
            }

            if (!Refill())
            {
                return false;
            }
        }
    }

    // Replaces the window with its unread rest and the next chunks of the source, at least doubling the rest so that the
    // work stays linear in the length of the unit.
    private bool Refill()
    {
        var rest = _text.Length - _pos;
        var builder = new StringBuilder(_text, _pos, rest, rest + RefillCapacity);
        do
        {
            if (!_source!.TryAppend(builder))
            {
                break;
            }
        } while (builder.Length < 2 * rest);

        _text = builder.ToString();
        _pos = 0;

        return _source.Error is null || Fail(_source.Error);
    }

    // The shortest text that tells the kinds of unit apart is `<!NOTATION`.
    private const int UnitHead = 10;
    private const int RefillCapacity = 4096;

    // Tells whether `rest`, which starts with a unit, holds the unit completely: a comment up to its `--`, a processing
    // instruction up to `?>`, a conditional section opener up to its second `[`, a declaration up to its `>` outside the
    // literals, a reference up to its `;`. Anything else is complete: the parser reports it.
    private static bool IsUnitComplete(ReadOnlySpan<char> rest)
    {
        if (rest.Length < UnitHead)
        {
            return false;
        }

        if (rest[0] == '%')
        {
            var length = XmlCharClass.NameLength(rest.Slice(1));

            return length == 0 || 1 + length < rest.Length;
        }

        if (rest.StartsWith("<!--".AsSpan()))
        {
            var end = rest.Slice(4).IndexOf("--".AsSpan());

            return end >= 0 && 4 + end + 2 < rest.Length;
        }

        if (rest.StartsWith("<?".AsSpan()))
        {
            return rest.Slice(2).IndexOf("?>".AsSpan()) >= 0;
        }

        if (rest.StartsWith("<![".AsSpan()))
        {
            return rest.Slice(3).IndexOf('[') >= 0;
        }

        if (!rest.StartsWith("<!".AsSpan()))
        {
            return true;
        }

        var quote = '\0';
        for (var i = 2; i < rest.Length; i++)
        {
            var c = rest[i];
            if (quote != '\0')
            {
                quote = c == quote ? '\0' : quote;
            }
            else if (c is '"' or '\'')
            {
                quote = c;
            }
            else if (c == '>')
            {
                return true;
            }
        }

        return false;
    }
}
