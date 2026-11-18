namespace NetEvolve.Schemas.Xml.Reader.Core;

using System;
using NetEvolve.Schemas.Xml.Buffers;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Limits;
using NetEvolve.Schemas.Xml.Namespaces;

/// <summary>
/// The resumable push parser (design #108, AP3 and AP9): it drives <see cref="XmlTokenizer"/> over the pending input, owns
/// the open-element stack, the phase (prolog, content, epilog), the buffered start tag, and the limits, calls
/// <see cref="XmlNamespaceScope"/>, and pushes events into the sink.
/// </summary>
/// <remarks>
/// Input arrives through <see cref="Feed"/>; <see cref="Pump{TSink}"/> consumes it and returns
/// <see cref="XmlReadStatus.NeedMoreData"/> after keeping the unconsumed tail. A chunk is used in place until a tail has to be
/// kept. An entity reference that the entity input stack accepts is read as a frame over the outer input: content frames
/// run the tokenizer over the replacement text (balanced, no construct spans the frame end), attribute value frames are
/// walked by section 3.3.3, and frame characters never advance the outer position. Limitations: the document type
/// declaration is rejected with <c>XML2001</c> unless a doctype handler is installed (deviation from XML 1.0 section 5.1);
/// a reference to another entity is <c>XML0008</c> without an entity input stack; a diagnostic inside a frame carries the
/// outer reference position but not the frame depth (<see cref="XmlDiagnostic"/> has no such member, #646); the
/// replacement text is read without line end normalization (the entity value was normalized when it was declared, #645);
/// white space outside the root element is not reported; <c>MaxNodeCount</c> counts elements,
/// attributes, comments, processing instructions, and coalesced text runs (not namespace declarations); an unclosed
/// element at the end of the input is <c>XML0001</c> and a missing root is <c>XML0012</c>; the events before an error, the
/// code, and the position of the diagnostic do not depend on the chunking (#598), but <see cref="Position"/> after an error
/// ends at the last consumed token and follows its boundaries. An exception thrown by a sink propagates and leaves the
/// parser unusable. Not thread-safe.
/// </remarks>
internal sealed partial class XmlParser : IXmlReader, IDisposable
{
    private enum Phase : byte
    {
        Start,
        Prolog,
        Content,
        Epilog,
        Done,
    }

    private readonly XmlReaderLimits _limits;
    private readonly IXmlDoctypeHandler? _doctypeHandler;
    private readonly IXmlEntityInputStack? _entityStack;
    private readonly char[] _inline = new char[2];
    private XmlTokenizer _tokenizer;
    private PooledBuffer<char> _carry = new(256);
    private PooledBuffer<char> _spare = new(256);
    private readonly PooledBuffer<char> _scratch = new(256);
    private ReadOnlyMemory<char> _chunk;
    private bool _isFinal;
    private Phase _phase;
    private XmlDiagnostic? _diagnostic;
    private XmlNameTable? _names;
    private XmlNamespaceScope? _scope;
    private XmlBudget _documentCharacters;
    private XmlBudget _nodes;
    private XmlBudget _text;
    private bool _textOpen;
    private bool _doctypeSeen;

    /// <summary>Initializes a new instance of the <see cref="XmlParser"/> class.</summary>
    /// <param name="limits">The limits.</param>
    /// <param name="doctypeHandler">The doctype seam, or <see langword="null"/> to reject a document type declaration.</param>
    /// <param name="entityStack">The entity input stack seam, or <see langword="null"/> to reject other entity references.</param>
    internal XmlParser(
        XmlReaderLimits limits,
        IXmlDoctypeHandler? doctypeHandler = null,
        IXmlEntityInputStack? entityStack = null
    )
    {
        _limits = limits;
        _doctypeHandler = doctypeHandler;
        _entityStack = entityStack;
        _tokenizer = new XmlTokenizer(limits, doctypeHandler is not null, entityStack is not null);
        _replacementTokenizer = XmlTokenizer.ForReplacementText(limits);
        _documentCharacters = XmlBudget.DocumentCharacters(limits);
        _nodes = new XmlBudget(limits.MaxNodeCount, XmlErrorCodes.XML1108);
        _text = XmlBudget.TextLength(limits);
        InitializeTag(limits);
    }

    /// <inheritdoc />
    public XmlPosition Position => _frameCount > 0 ? _referencePosition : _tokenizer.Position;

    /// <inheritdoc />
    public XmlDiagnostic? Diagnostic => _diagnostic;

    /// <summary>Supplies the next chunk of decoded characters; call <see cref="Pump{TSink}"/> afterwards.</summary>
    /// <param name="chunk">The characters; used in place until the next <see cref="Pump{TSink}"/> returns.</param>
    /// <param name="isFinal">Whether no more data follows.</param>
    internal void Feed(ReadOnlyMemory<char> chunk, bool isFinal)
    {
        if (_carry.Length > 0)
        {
            _carry.Append(chunk.Span);
            _chunk = default;
        }
        else
        {
            _chunk = chunk;
        }

        _isFinal = isFinal;
    }

    /// <inheritdoc />
    public XmlReadStatus Pump<TSink>(ref TSink sink)
        where TSink : IXmlSink
    {
        if (!TryStart(sink.Names, out var early))
        {
            return early;
        }

        var fromCarry = _carry.Length > 0;
        var pos = 0;
        while (true)
        {
            // While a frame is open the tokenizer reads its replacement text, which is final by itself.
            var inFrame = _frameCount > 0;
            var span = inFrame ? FrameSpan() : (fromCarry ? _carry.WrittenSpan : _chunk.Span).Slice(pos);
            if (!Tokenize(span, inFrame, out var status, out var token, out var consumed))
            {
                return XmlReadStatus.Error;
            }

            if (inFrame)
            {
                _frames[_frameCount - 1].Offset += consumed;
            }
            else
            {
                pos += consumed;
            }

            if (status == XmlTokenStatus.NeedMoreData)
            {
                if (inFrame)
                {
                    _ = Fail(XmlErrorCodes.XML0001, _referencePosition);
                    return XmlReadStatus.Error;
                }

                KeepTail(fromCarry, pos);
                return XmlReadStatus.NeedMoreData;
            }

            if (status == XmlTokenStatus.End)
            {
                if (!inFrame)
                {
                    return Finish(ref sink) ? XmlReadStatus.Completed : XmlReadStatus.Error;
                }

                if (!PopFrame())
                {
                    return XmlReadStatus.Error;
                }

                continue;
            }

            if (!Handle(ref sink, in token, span))
            {
                return XmlReadStatus.Error;
            }
        }
    }

    // False with the status to return when the parser has already failed or finished, or cannot take the name table.
    private bool TryStart(XmlNameTable names, out XmlReadStatus status)
    {
        status = XmlReadStatus.Error;
        if (_diagnostic is not null)
        {
            return false;
        }

        if (_phase == Phase.Done)
        {
            status = XmlReadStatus.Completed;
            return false;
        }

        return _scope is not null || Initialize(names);
    }

    // One tokenizer step with the budgets charged for it; false when a diagnostic was set.
    private bool Tokenize(
        ReadOnlySpan<char> span,
        bool inFrame,
        out XmlTokenStatus status,
        out XmlToken token,
        out int consumed
    )
    {
        var before = _tokenizer.Position;
        status = _tokenizer.Next(span, inFrame || _isFinal, out token, out consumed);
        if (status == XmlTokenStatus.Error)
        {
            var failure = _tokenizer.Diagnostic!;
            return Fail(failure.Code, failure.Position);
        }

        var after = _tokenizer.Position;

        // Frame characters are charged to the expansion budget when the frame opens, not to the document.
        if (!inFrame && !_documentCharacters.TryCharge(after.CharOffset - before.CharOffset))
        {
            return Fail(XmlErrorCodes.XML1101, after);
        }

        var startsTag = status == XmlTokenStatus.Token && token.Kind == XmlTokenKind.StartTagName;
        return !(_inTag || startsTag) || ChargeStartTag(startsTag, consumed, after);
    }

    /// <summary>Returns the buffers.</summary>
    public void Dispose()
    {
        _carry.Dispose();
        _spare.Dispose();
        _scratch.Dispose();
        DisposeTag();
    }

    // Keeps the unconsumed tail for the next chunk; a buffer that was fully consumed from the front is compacted only when something was consumed.
    private void KeepTail(bool fromCarry, int consumed)
    {
        if (fromCarry)
        {
            if (consumed == 0)
            {
                return;
            }

            _spare.Clear();
            _spare.Append(_carry.WrittenSpan.Slice(consumed));
            (_carry, _spare) = (_spare, _carry);
            _spare.Clear();
            return;
        }

        _carry.Clear();
        _carry.Append(_chunk.Span.Slice(consumed));
        _chunk = default;
    }

    private bool Initialize(XmlNameTable names)
    {
        try
        {
            _scope = new XmlNamespaceScope(names);
        }
        catch (InvalidOperationException)
        {
            return Fail(names.IsFrozen ? XmlErrorCodes.XML1116 : XmlErrorCodes.XML1111, Position);
        }

        _names = names;
        return true;
    }

    private bool Finish<TSink>(ref TSink sink)
        where TSink : IXmlSink
    {
        if (_phase == Phase.Epilog)
        {
            EndText();
            sink.EndDocument();
            _phase = Phase.Done;
            return true;
        }

        return Fail(_phase == Phase.Content ? XmlErrorCodes.XML0001 : XmlErrorCodes.XML0012, Position);
    }

    private bool Handle<TSink>(ref TSink sink, in XmlToken token, ReadOnlySpan<char> span)
        where TSink : IXmlSink
    {
        var position = _frameCount > 0 ? _referencePosition : _tokenizer.TokenPosition;
        if (_phase == Phase.Start && StartDocument(ref sink, in token, span))
        {
            return true;
        }

        switch (token.Kind)
        {
            case XmlTokenKind.StartTagName:
                return StartTag(token.First(span), position);
            case XmlTokenKind.AttributeName:
                return AttributeName(token.First(span), position);
            case XmlTokenKind.AttributeValueEnd:
                return true;
            case XmlTokenKind.StartTagEnd:
                return FinishStartTag(ref sink, false);
            case XmlTokenKind.EmptyTagEnd:
                return FinishStartTag(ref sink, true);
            case XmlTokenKind.EndTagName:
                return EndTagName(token.First(span), position);
            case XmlTokenKind.EndTagEnd:
                return EndTagEnd(ref sink);
            case XmlTokenKind.Text:
                return _inTag
                    ? AppendValue(token.First(span), position)
                    : EmitText(ref sink, token.First(span), XmlTextKind.Text, true, position);
            case XmlTokenKind.LineBreak:
                return EmitText(ref sink, InlineSpan(in token), XmlTextKind.Reference, true, position);
            case XmlTokenKind.Reference:
                return _inTag
                    ? AppendValue(InlineSpan(in token), position)
                    : EmitText(ref sink, InlineSpan(in token), XmlTextKind.Reference, false, position);
            case XmlTokenKind.EntityReference:
                return EnterEntity(token.First(span), position);
            case XmlTokenKind.CDataStart:
                return _phase == Phase.Content || Fail(XmlErrorCodes.XML0018, position);
            case XmlTokenKind.CData:
                return EmitText(
                    ref sink,
                    token.InlineCount > 0 ? InlineSpan(in token) : token.First(span),
                    XmlTextKind.CData,
                    false,
                    position
                );
            case XmlTokenKind.Comment:
                return EmitComment(ref sink, token.First(span), position);
            case XmlTokenKind.ProcessingInstruction:
                return EmitProcessingInstruction(ref sink, token.First(span), token.Second(span), position);
            default:
                return EmitDoctype(ref sink, span, position);
        }
    }

    // Starts the document with the first token; returns true when the token was the XML declaration and is fully handled.
    private bool StartDocument<TSink>(ref TSink sink, in XmlToken token, ReadOnlySpan<char> span)
        where TSink : IXmlSink
    {
        _phase = Phase.Prolog;
        if (token.Kind != XmlTokenKind.Declaration)
        {
            sink.StartDocument(default);
            return false;
        }

        _standalone = token.Char0 == 'y';
        var declaration = new XmlDeclaration(
            token.First(span),
            token.Second(span),
            token.Char0 == '\0' ? null : token.Char0 == 'y'
        );
        sink.StartDocument(in declaration);
        return true;
    }

    private ReadOnlySpan<char> InlineSpan(in XmlToken token)
    {
        _inline[0] = token.Char0;
        _inline[1] = token.Char1;
        return new ReadOnlySpan<char>(_inline, 0, token.InlineCount);
    }

    private bool EmitText<TSink>(
        ref TSink sink,
        ReadOnlySpan<char> text,
        XmlTextKind kind,
        bool blankAllowedOutside,
        XmlPosition position
    )
        where TSink : IXmlSink
    {
        if (_phase != Phase.Content)
        {
            // White space outside the root element is valid but not part of the document. The error is at the first character
            // that is not white space (a text run holds no line break), so it does not depend on where a chunk edge splits the run.
            var at = blankAllowedOutside ? FirstNonBlank(text) : 0;
            return at < 0
                || Fail(
                    XmlErrorCodes.XML0018,
                    new XmlPosition(position.Line, position.Column + at, position.CharOffset + at)
                );
        }

        if (!_textOpen)
        {
            if (!_nodes.TryCharge(1))
            {
                return Fail(XmlErrorCodes.XML1108, position);
            }

            _textOpen = true;
        }

        if (!_text.TryCharge(text.Length))
        {
            return Fail(XmlErrorCodes.XML1107, position);
        }

        sink.Text(text, kind);
        return true;
    }

    private bool EmitComment<TSink>(ref TSink sink, ReadOnlySpan<char> text, XmlPosition position)
        where TSink : IXmlSink
    {
        EndText();
        if (!_nodes.TryCharge(1))
        {
            return Fail(XmlErrorCodes.XML1108, position);
        }

        sink.Comment(NormalizeLineEnds(text));
        return true;
    }

    private bool EmitProcessingInstruction<TSink>(
        ref TSink sink,
        ReadOnlySpan<char> target,
        ReadOnlySpan<char> data,
        XmlPosition position
    )
        where TSink : IXmlSink
    {
        EndText();
        if (!_nodes.TryCharge(1))
        {
            return Fail(XmlErrorCodes.XML1108, position);
        }

        // Namespaces in XML 1.0, production [17]: a processing instruction target is an NCName.
        if (target.IndexOf(':') >= 0)
        {
            return Fail(XmlErrorCodes.XML3004, position);
        }

        var status = _names!.TryIntern(target, out var symbol);
        if (status != XmlNameTableStatus.Ok)
        {
            return Fail(NameTableError(status), position);
        }

        sink.ProcessingInstruction(symbol, NormalizeLineEnds(data));
        return true;
    }

    private bool EmitDoctype<TSink>(ref TSink sink, ReadOnlySpan<char> span, XmlPosition position)
        where TSink : IXmlSink
    {
        if (_phase != Phase.Prolog || _doctypeSeen)
        {
            return Fail(XmlErrorCodes.XML0011, position);
        }

        var parts = _tokenizer.LastDoctype;
        var info = new XmlDoctypeInfo(
            XmlDoctypeParts.Slice(span, parts.Name),
            XmlDoctypeParts.Slice(span, parts.PublicId),
            XmlDoctypeParts.Slice(span, parts.SystemId),
            XmlDoctypeParts.Slice(span, parts.Subset)
        );
        var error = _doctypeHandler!.Handle(in info);
        if (error is not null)
        {
            return Fail(error, position);
        }

        _doctypeSeen = true;
        sink.Doctype(in info);
        return true;
    }

    // Any event other than text ends the current text node and its length budget.
    private void EndText()
    {
        _text.Reset();
        _textOpen = false;
    }

    // Line ends are normalized to LF (section 2.11) in atomic tokens; text and attribute values arrive normalized.
    private ReadOnlySpan<char> NormalizeLineEnds(ReadOnlySpan<char> text)
    {
        if (text.IndexOf('\r') < 0)
        {
            return text;
        }

        _scratch.Clear();
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\r')
            {
                _scratch.Append('\n');
                if (i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }
            }
            else
            {
                _scratch.Append(text[i]);
            }
        }

        return _scratch.WrittenSpan;
    }

    // The index of the first character that is not white space, -1 when the text is blank.
    private static int FirstNonBlank(ReadOnlySpan<char> text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] is not (' ' or '\t' or '\r' or '\n'))
            {
                return i;
            }
        }

        return -1;
    }

    private static XmlErrorCode NameTableError(XmlNameTableStatus status) =>
        status == XmlNameTableStatus.Frozen ? XmlErrorCodes.XML1116 : XmlErrorCodes.XML1111;

    // An error inside a frame carries the position of the outer reference, never a position inside the replacement text.
    private bool Fail(XmlErrorCode code, XmlPosition position)
    {
        _diagnostic = new XmlDiagnostic(code, _frameCount > 0 ? _referencePosition : position);
        return false;
    }
}
