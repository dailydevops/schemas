namespace NetEvolve.Schemas.Xml.Reader.Core;

using System;
using System.Runtime.InteropServices;
using NetEvolve.Schemas.Xml.Buffers;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Reader.References;
using NetEvolve.Schemas.Xml.Text.CharClasses;

internal sealed partial class XmlParser
{
    // One open expansion. A content frame keeps the tokenizer state of the input it interrupts and the open element depth
    // it must return to; an attribute value frame only walks its text. The frame of an external entity has no text: its
    // pending characters are the unconsumed tail of the chunks read from the source, refilled when the tokenizer asks.
    [StructLayout(LayoutKind.Auto)]
    private struct EntityFrame
    {
        internal XmlTokenizer Saved;
        internal string? Text;
        internal int Offset;
        internal int ElementDepth;
        internal IXmlEntityText? Source;
        internal PooledBuffer<char>? Pending;
        internal bool SourceEnded;
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

    // The base URI of the entity whose text is being read; without an entity input stack there is none.
    internal string? EntityBaseUri => _entityStack?.BaseUri;

    private ReadOnlySpan<char> FrameSpan()
    {
        ref var frame = ref _frames[_frameCount - 1];
        return frame.Pending is { } pending ? pending.WrittenSpan.Slice(frame.Offset) : frame.Text.AsSpan(frame.Offset);
    }

    // The text of an internal frame is final by itself; the text of an external frame is final once its source has ended.
    private bool FrameIsFinal()
    {
        ref var frame = ref _frames[_frameCount - 1];
        return frame.Pending is null || frame.SourceEnded;
    }

    // A reference to an entity that is not predefined (sections 4.1 and 4.4): outside the root element it is text, so it is
    // not allowed there; inside, the stack decides and the expansion is read as a frame.
    private bool EnterEntity(ReadOnlySpan<char> name, XmlPosition position)
    {
        if (!_inTag && _phase != Phase.Content)
        {
            return Fail(XmlErrorCodes.XML0018, position);
        }

        IXmlEntityText? external = null;
        try
        {
            var error = _entityStack!.Enter(name, _inTag, _standalone, out var replacement, out external);
            if (error is not null)
            {
                return Fail(error, position);
            }

            if (replacement is null && external is null)
            {
                return true;
            }

            if (_frameCount == 0)
            {
                _referencePosition = position;
            }

            if (external is not null)
            {
                // The frame owns the text from here on.
                var owned = external;
                external = null;
                return PushExternalFrame(owned);
            }

            return _inTag ? ExpandInAttributeValue(replacement!, position) : PushContentFrame(replacement!);
        }
        finally
        {
            external?.Dispose();
        }
    }

    private bool PushContentFrame(string text)
    {
        PushFrame(text);
        return EnterContentFrame();
    }

    // The text of an external entity is read through the source as the tokenizer asks for more.
    private bool PushExternalFrame(IXmlEntityText source)
    {
        PushFrame(string.Empty);
        ref var frame = ref _frames[_frameCount - 1];
        frame.Source = source;
        frame.Pending = new PooledBuffer<char>();

        return EnterContentFrame();
    }

    private bool EnterContentFrame()
    {
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
        frame.Source = null;
        frame.Pending = null;
        frame.SourceEnded = false;
    }

    // Reads more of the text of the innermost external frame; false when the diagnostic is set. A frame whose text is
    // complete cannot be refilled: the tokenizer then stopped inside a construct, which the replacement text must not do.
    private bool RefillFrame()
    {
        ref var frame = ref _frames[_frameCount - 1];
        if (frame.Pending is not { } pending || frame.SourceEnded)
        {
            return Fail(XmlErrorCodes.XML0001, _referencePosition);
        }

        // Only the characters of the construct that is still open stay in the buffer.
        if (frame.Offset > 0)
        {
            _spare.Clear();
            _spare.Append(pending.WrittenSpan.Slice(frame.Offset));
            pending.Clear();
            pending.Append(_spare.WrittenSpan);
            _spare.Clear();
            frame.Offset = 0;
        }

        if (frame.Source!.TryRead(out var chunk))
        {
            pending.Append(chunk.Span);
            return true;
        }

        frame.SourceEnded = true;
        return frame.Source.Error is not { } error || Fail(error, _referencePosition);
    }

    // Closes the text of every open external frame; the frames themselves stay, because the position of a diagnostic needs them.
    private void CloseSources()
    {
        for (var i = 0; i < _frameCount; i++)
        {
            ref var frame = ref _frames[i];
            frame.Source?.Dispose();
            frame.Source = null;
            frame.Pending?.Dispose();
            frame.Pending = null;
        }
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
        frame.Source?.Dispose();
        frame.Source = null;
        frame.Pending?.Dispose();
        frame.Pending = null;
        _frameCount--;
        _entityStack!.Leave();

        return true;
    }

    // Section 3.3.3 for the replacement text of an entity in an attribute value, and for the literal of a default value
    // (isLiteral, XML-16): references expand recursively, a literal
    // '<' is rejected (WFC No < in Attribute Values), TAB, CR, and LF become one space each (CR LF written as
    // two character references is two spaces, #645), character references stay unnormalized, and quotes are ordinary
    // characters. The frames live on the same stack as the content frames and are all closed again
    // when the walk returns.
    private bool ExpandInAttributeValue(string replacement, XmlPosition position, bool isLiteral = false)
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

                // The literal of a default is no entity: its frame was never entered, so it is not left.
                if (!isLiteral || _frameCount > floor)
                {
                    _entityStack!.Leave();
                }

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
                // The line ends of the entity value were normalized when it was declared: every CR or LF here is a white
                // space character of its own, so CR LF written as two character references gives two spaces.
                frame.Offset++;
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
        var failure = _entityStack!.Enter(rest.Slice(1, length - 2), true, _standalone, out var inner, out _);
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
