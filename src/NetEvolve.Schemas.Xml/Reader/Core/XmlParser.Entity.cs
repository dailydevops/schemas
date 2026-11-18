namespace NetEvolve.Schemas.Xml.Reader.Core;

using System;
using System.Runtime.InteropServices;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Reader.References;
using NetEvolve.Schemas.Xml.Text.CharClasses;

internal sealed partial class XmlParser
{
    // One open expansion. A content frame keeps the tokenizer state of the input it interrupts and the open element depth
    // it must return to; an attribute value frame only walks its text.
    [StructLayout(LayoutKind.Auto)]
    private struct EntityFrame
    {
        internal XmlTokenizer Saved;
        internal string? Text;
        internal int Offset;
        internal int ElementDepth;
    }

    private static readonly char[] AttributeStops = ['&', '<', '\t', '\r', '\n'];

    // Built once: the guard of the factory allocates on netstandard2.0, so it must not run for every frame.
    private readonly XmlTokenizer _replacementTokenizer;
    private EntityFrame[] _frames = [];
    private int _frameCount;
    private XmlPosition _referencePosition;
    private bool _standalone;

    // The open element depth the innermost content frame started at; zero outside of a frame.
    private int FrameElementDepth => _frameCount > 0 ? _frames[_frameCount - 1].ElementDepth : 0;

    private ReadOnlySpan<char> FrameSpan() => _frames[_frameCount - 1].Text.AsSpan(_frames[_frameCount - 1].Offset);

    // A reference to an entity that is not predefined (sections 4.1 and 4.4): outside the root element it is text, so it is
    // not allowed there; inside, the stack decides and the expansion is read as a frame.
    private bool EnterEntity(ReadOnlySpan<char> name, XmlPosition position)
    {
        if (!_inTag && _phase != Phase.Content)
        {
            return Fail(XmlErrorCodes.XML0018, position);
        }

        var error = _entityStack!.Enter(name, _inTag, _standalone, out var replacement);
        if (error is not null)
        {
            return Fail(error, position);
        }

        if (replacement is null)
        {
            return true;
        }

        if (_frameCount == 0)
        {
            _referencePosition = position;
        }

        return _inTag ? ExpandInAttributeValue(replacement, position) : PushContentFrame(replacement);
    }

    private bool PushContentFrame(string text)
    {
        PushFrame(text);
        _frames[_frameCount - 1].Saved = _tokenizer;
        _frames[_frameCount - 1].ElementDepth = _depth;
        _tokenizer = _replacementTokenizer;

        return true;
    }

    private void PushFrame(string text)
    {
        if (_frameCount == _frames.Length)
        {
            Array.Resize(ref _frames, Math.Max(4, _frameCount * 2));
        }

        ref var frame = ref _frames[_frameCount++];
        frame.Text = text;
        frame.Offset = 0;
    }

    // The replacement text must have closed every element it opened (section 4.3.2: it matches the production content).
    private bool PopFrame()
    {
        ref var frame = ref _frames[_frameCount - 1];
        if (_depth != frame.ElementDepth)
        {
            return Fail(XmlErrorCodes.XML0001, _referencePosition);
        }

        _tokenizer = frame.Saved;
        frame.Text = null;
        _frameCount--;
        _entityStack!.Leave();

        return true;
    }

    // Section 3.3.3 for the replacement text of an entity in an attribute value: references expand recursively, a literal
    // '<' is rejected (WFC No < in Attribute Values), TAB, CR, LF, and CRLF become one space each (limitation: CR LF
    // written as two character references should be two spaces, #645), character references stay unnormalized, and
    // quotes are ordinary characters. The frames live on the same stack as the content frames and are all closed again
    // when the walk returns.
    private bool ExpandInAttributeValue(string replacement, XmlPosition position)
    {
        var floor = _frameCount;
        PushFrame(replacement);
        while (_frameCount > floor)
        {
            ref var frame = ref _frames[_frameCount - 1];
            var text = frame.Text.AsSpan();
            if (frame.Offset >= text.Length)
            {
                frame.Text = null;
                _frameCount--;
                _entityStack!.Leave();
                continue;
            }

            var rest = text.Slice(frame.Offset);
            var run = rest.IndexOfAny(AttributeStops);
            if (run != 0)
            {
                var length = run < 0 ? rest.Length : run;
                frame.Offset += length;
                if (XmlCharClass.IndexOfNonChar(rest.Slice(0, length)) >= 0)
                {
                    return Fail(XmlErrorCodes.XML0002, position);
                }

                if (!AppendValue(rest.Slice(0, length), position))
                {
                    return false;
                }

                continue;
            }

            if (!ExpandAttributeMarkup(ref frame, rest, position))
            {
                return false;
            }
        }

        return true;
    }

    // Handles the character at the front of `rest`, one of '&', '<', TAB, CR, or LF.
    private bool ExpandAttributeMarkup(ref EntityFrame frame, ReadOnlySpan<char> rest, XmlPosition position)
    {
        switch (rest[0])
        {
            case '<':
                return Fail(XmlErrorCodes.XML0013, position);
            case '&':
                break;
            default:
                // A CRLF pair is one white space character.
                frame.Offset += rest[0] == '\r' && rest.Length > 1 && rest[1] == '\n' ? 2 : 1;
                var space = XmlToken.Inline(XmlTokenKind.Reference, ' ');
                return AppendValue(InlineSpan(in space), position);
        }

        var error = XmlReferenceResolver.TryParseReference(rest, out var length, out var codePoint);
        if (error is null)
        {
            frame.Offset += length;
            var character = XmlToken.InlineCodePoint(XmlTokenKind.Reference, codePoint);
            return AppendValue(InlineSpan(in character), position);
        }

        if (!ReferenceEquals(error, XmlErrorCodes.XML0008))
        {
            return Fail(error, position);
        }

        // The frame reference is not used after the push, which may move the array.
        frame.Offset += length;
        var failure = _entityStack!.Enter(rest.Slice(1, length - 2), true, _standalone, out var inner);
        if (failure is not null)
        {
            return Fail(failure, position);
        }

        if (inner is not null)
        {
            PushFrame(inner);
        }

        return true;
    }
}
