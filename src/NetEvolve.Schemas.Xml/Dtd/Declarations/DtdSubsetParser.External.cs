namespace NetEvolve.Schemas.Xml.Dtd.Declarations;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.External.Subset;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Text.CharClasses;

// The part of the declaration parser that reads external text (XML-20, #291). It lives with the external subset code and
// extends DtdSubsetParser: the external subset and the external parameter entities, parameter-entity references inside
// declarations (section 4.4.8), parameter-entity references inside entity values (section 4.4.5), and conditional sections
// (section 3.4).
internal sealed partial class DtdSubsetParser
{
    // The frames of the parameter entities that are included inside a declaration; each one holds the text to return to.
    private readonly List<TextFrame> _frames = [];
    private string? _baseUri;
    private int _depth;
    private int _frameBase;
    private int _sectionDepth;
    private bool _inExternal;

    [StructLayout(LayoutKind.Auto)]
    private readonly record struct TextFrame(
        string Text,
        int Position,
        bool InExternal,
        string? BaseUri,
        ExternalSubsetText? ChildOwner
    );

    [StructLayout(LayoutKind.Auto)]
    private readonly record struct Context(
        string Text,
        int Position,
        int Depth,
        bool InExternal,
        string? BaseUri,
        int FrameBase,
        int SectionDepth
    );

    // The number of texts that are open around the current position: the sequences and the frames of this sequence.
    private int Nesting => _depth + _frames.Count - _frameBase;

    private Context SaveContext() => new(_text, _pos, _depth, _inExternal, _baseUri, _frameBase, _sectionDepth);

    private void RestoreContext(in Context saved) =>
        (_text, _pos, _depth, _inExternal, _baseUri, _frameBase, _sectionDepth) = (
            saved.Text,
            saved.Position,
            saved.Depth,
            saved.InExternal,
            saved.BaseUri,
            saved.FrameBase,
            saved.SectionDepth
        );

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

            return ParseSequence(opened!.Text, 0, inExternal: true, opened.BaseUri);
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

        if (Nesting + 1 > _limits.MaxEntityDepth)
        {
            return Fail(XmlErrorCodes.XML1110);
        }

        // The entity closes when the frame returns to its parent; an error closes it with the sequence.
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

                text = owner!.Text;
                baseUri = owner.BaseUri;
            }
            else if (!Charge(text.Length))
            {
                return Fail(XmlErrorCodes.XML1109);
            }

            _frames.Add(new TextFrame(_text, _pos, _inExternal, _baseUri, owner));
            owner = null;
            (_text, _pos, _baseUri) = (text, 0, baseUri);

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

        var inner = level + 1;
        if (Nesting + inner > _limits.MaxEntityDepth)
        {
            return Fail(XmlErrorCodes.XML1110);
        }

        if (entity.Value is not null)
        {
            return Charge(entity.Value.Length)
                ? AppendEntityValue(builder, entity.Value, inner)
                : Fail(XmlErrorCodes.XML1109);
        }

        ExternalSubsetText? opened = null;
        try
        {
            return TryOpen(entity, out opened) && AppendEntityValue(builder, opened!.Text, inner);
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

    // Productions 64 and 65: the contents of an IGNORE section are not parsed; nested `<![` and `]]>` pairs balance.
    private bool SkipIgnoredContents()
    {
        var level = 1;
        var open = _text.IndexOf("<![", _pos, StringComparison.Ordinal);
        while (level > 0)
        {
            var close = _text.IndexOf("]]>", _pos, StringComparison.Ordinal);
            if (close < 0)
            {
                return Fail(XmlErrorCodes.XML0011);
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
}
