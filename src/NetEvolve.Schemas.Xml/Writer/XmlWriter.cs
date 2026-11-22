namespace NetEvolve.Schemas.Xml.Writer;

using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Buffers;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Text.CharClasses;

/// <summary>
/// Writes the events of the sink contract as well-formed XML 1.0: to an <see cref="IBufferWriter{T}"/> of characters or
/// bytes on the span path, or to a <see cref="Stream"/> through <see cref="FlushAsync"/>.
/// </summary>
/// <remarks>
/// <para>
/// A start tag is written when its <see cref="EndAttributes"/> arrives, after namespace declaration fixup: a missing
/// declaration is added, a prefix bound to another URI in scope is replaced by an existing prefix of the URI or by a
/// fresh <c>ns1</c>, <c>ns2</c>, ... prefix issued into <see cref="Names"/>, and a declaration that repeats an in-scope
/// binding is left out. An invalid event throws <see cref="XmlWriteException"/> before anything is written for it; after
/// an exception from <see cref="EndAttributes"/> the writer must not be used further.
/// </para>
/// <para>
/// Limitations: XML 1.0 only; output encodings are UTF-8 and UTF-16 little endian (with a byte order mark written by
/// <see cref="StartDocument"/>). The stream path buffers in memory and writes only in <see cref="FlushAsync"/>; the
/// stream itself is neither flushed nor closed. The writer does not check that a document type declaration names the
/// root element, and it does not check namespace-reserved URIs other than the <c>xml</c> and <c>xmlns</c> prefixes.
/// </para>
/// <para>
/// Canonical output (<see cref="XmlWriterOptions.Canonical"/>) is a documented subset of the canonical form of the W3C XML
/// Conformance Test Suite, because the normative text of that form is not available: UTF-8 output; XML declaration and
/// document type declaration removed; attributes, including <c>xmlns</c> declarations, sorted by code point of their
/// qualified name; CDATA sections and references written as their characters; empty elements as start and end tag; double
/// quotes as delimiters; <c>&amp;</c>, <c>&lt;</c>, <c>&gt;</c> and the quote escaped in attribute values together with
/// <c>&amp;#9;</c>, <c>&amp;#10;</c>, <c>&amp;#13;</c>; <c>&amp;#13;</c> in text; comments and processing instructions
/// kept. Output of the real suite can differ in details this subset does not cover (W3C Canonical XML is DEF-65).
/// </para>
/// </remarks>
public sealed partial class XmlWriter : IXmlSink, IDisposable
{
    private readonly IBufferWriter<char> _output;
    private readonly XmlNameTable _names;
    private readonly bool _canonical;
    private readonly bool _omitDeclaration;
    private readonly CharToByteWriter? _adapter;
    private readonly bool _utf16;
    private readonly StreamBuffer? _stream;
    private readonly PooledBuffer<char> _chars = new();

    private Entry[] _entries = new Entry[8];
    private int _entryCount;
    private XmlName[] _open = new XmlName[16];
    private int[] _openMark = new int[16];
    private int _depth;
    private int[] _bindPrefix = new int[16];
    private int[] _bindUri = new int[16];
    private int _bindCount;
    private XmlName _pending;
    private bool _inTag;
    private bool _tagOpen;
    private bool _rootSeen;

    /// <summary>Initializes a new instance of the <see cref="XmlWriter"/> class that writes characters.</summary>
    /// <param name="output">The destination.</param>
    /// <param name="names">The table that resolves the symbols of the events; the writer issues generated prefixes into it.</param>
    /// <param name="options">The options, <see langword="null"/> for <see cref="XmlWriterOptions.Default"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="output"/> or <paramref name="names"/> is <see langword="null"/>.</exception>
    public XmlWriter(IBufferWriter<char> output, XmlNameTable names, XmlWriterOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(names);
        _output = output;
        _names = names;
        var effective = options ?? XmlWriterOptions.Default;
        _canonical = effective.Canonical;
        _omitDeclaration = effective.Canonical || effective.OmitXmlDeclaration;
    }

    /// <summary>Initializes a new instance of the <see cref="XmlWriter"/> class that writes UTF-8 or UTF-16 bytes.</summary>
    /// <param name="output">The destination.</param>
    /// <param name="names">The table that resolves the symbols of the events; the writer issues generated prefixes into it.</param>
    /// <param name="options">The options, <see langword="null"/> for <see cref="XmlWriterOptions.Default"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="output"/> or <paramref name="names"/> is <see langword="null"/>.</exception>
    public XmlWriter(IBufferWriter<byte> output, XmlNameTable names, XmlWriterOptions? options = null)
        : this(new CharToByteWriter(output, IsUtf16(options)), names, options) { }

    /// <summary>Initializes a new instance of the <see cref="XmlWriter"/> class that writes bytes to a stream with <see cref="FlushAsync"/>.</summary>
    /// <param name="stream">The destination; it is only written asynchronously.</param>
    /// <param name="names">The table that resolves the symbols of the events; the writer issues generated prefixes into it.</param>
    /// <param name="options">The options, <see langword="null"/> for <see cref="XmlWriterOptions.Default"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> or <paramref name="names"/> is <see langword="null"/>.</exception>
    public XmlWriter(Stream stream, XmlNameTable names, XmlWriterOptions? options = null)
        : this(new StreamBuffer(stream), names, options) { }

    private XmlWriter(StreamBuffer buffer, XmlNameTable names, XmlWriterOptions? options)
        : this(new CharToByteWriter(buffer, IsUtf16(options)), names, options) => _stream = buffer;

    private XmlWriter(CharToByteWriter adapter, XmlNameTable names, XmlWriterOptions? options)
        : this((IBufferWriter<char>)adapter, names, options)
    {
        _adapter = adapter;
        _utf16 = IsUtf16(options);
    }

    /// <inheritdoc />
    public XmlNameTable Names => _names;

    /// <summary>Writes the bytes buffered since the last call to the stream with one asynchronous write; it does nothing for buffer writer output.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the stream has accepted the bytes.</returns>
    public Task FlushAsync(CancellationToken cancellationToken = default) =>
        _stream is null ? Task.CompletedTask : _stream.FlushAsync(cancellationToken);

    /// <inheritdoc />
    public void StartDocument(in XmlDeclaration declaration)
    {
        var hasDeclaration = !declaration.Version.IsEmpty;
        if (hasDeclaration && !declaration.Version.SequenceEqual("1.0".AsSpan()))
        {
            throw Error(XmlErrorCodes.XML0010);
        }

        if (_utf16)
        {
            _adapter!.WriteBom();
        }

        if (_omitDeclaration || !hasDeclaration)
        {
            return;
        }

        Put("<?xml version=\"1.0\"");
        if (_adapter is not null)
        {
            Put(_utf16 ? " encoding=\"UTF-16\"" : " encoding=\"UTF-8\"");
        }

        if (declaration.Standalone is { } standalone)
        {
            Put(standalone ? " standalone=\"yes\"" : " standalone=\"no\"");
        }

        Put("?>");
    }

    /// <inheritdoc />
    public void EndDocument()
    {
        if (_inTag || _depth > 0)
        {
            throw Error(XmlErrorCodes.XML0001);
        }

        if (!_rootSeen)
        {
            throw Error(XmlErrorCodes.XML0012);
        }
    }

    /// <inheritdoc />
    public void StartElement(XmlName name, XmlPosition position)
    {
        NotInTag();
        if (_depth == 0 && _rootSeen)
        {
            throw Error(XmlErrorCodes.XML0012);
        }

        ValidateName(name);
        CloseTag();
        _rootSeen = true;
        _pending = name;
        _inTag = true;
        _entryCount = 0;
        _chars.Clear();
    }

    /// <inheritdoc />
    public void NamespaceDeclaration(XmlSymbol prefix, XmlSymbol uri)
    {
        InTag();
        var p = Str(prefix);
        var u = Str(uri);
        if (p.Length > 0 && !IsNCName(p))
        {
            throw Error(XmlErrorCodes.XML3004);
        }

        if (p is "xml" or "xmlns")
        {
            throw Error(XmlErrorCodes.XML3002);
        }

        if (p.Length > 0 && u.Length == 0)
        {
            throw Error(XmlErrorCodes.XML3005);
        }

        CheckChars(u);
        for (var i = 0; i < _entryCount; i++)
        {
            if (_entries[i].IsDecl && _entries[i].Name.Prefix == prefix)
            {
                throw Error(XmlErrorCodes.XML0006);
            }
        }

        AddEntry(new Entry { IsDecl = true, Name = new XmlName(prefix, default, uri) });
    }

    /// <inheritdoc />
    public void Attribute(XmlName name, ReadOnlySpan<char> value)
    {
        InTag();
        ValidateName(name);
        CheckChars(value);
        for (var i = 0; i < _entryCount; i++)
        {
            if (
                !_entries[i].IsDecl
                && _entries[i].Name.Local == name.Local
                && _entries[i].Name.NamespaceUri == name.NamespaceUri
            )
            {
                throw Error(name.NamespaceUri.IsEmpty ? XmlErrorCodes.XML0006 : XmlErrorCodes.XML3003);
            }
        }

        var start = _chars.Length;
        _chars.Append(value);
        AddEntry(
            new Entry
            {
                Name = name,
                ValStart = start,
                ValLen = value.Length,
            }
        );
    }

    /// <inheritdoc />
    public void EndAttributes()
    {
        InTag();
        var mark = _bindCount;
        var count = _entryCount;
        FixDeclarations(count);
        _pending = FixName(_pending, attribute: false, mark);
        for (var i = 0; i < count; i++)
        {
            if (!_entries[i].IsDecl)
            {
                _entries[i].Name = FixName(_entries[i].Name, attribute: true, mark);
            }
        }

        BuildQualifiedNames();
        if (_canonical)
        {
            SortEntries();
        }

        WriteStartTag();
        Push(_pending, mark);
        _inTag = false;
        _tagOpen = true;
    }

    /// <inheritdoc />
    public void EndElement(XmlName name)
    {
        NotInTag();
        if (_depth == 0)
        {
            throw Error(XmlErrorCodes.XML0005);
        }

        var top = _open[_depth - 1];
        if (top.Local != name.Local || top.NamespaceUri != name.NamespaceUri)
        {
            throw Error(XmlErrorCodes.XML0005);
        }

        _depth--;
        _bindCount = _openMark[_depth];
        if (_tagOpen && !_canonical)
        {
            _tagOpen = false;
            Put("/>");
            return;
        }

        CloseTag();
        Put("</");
        PutName(top);
        Put('>');
    }

    /// <inheritdoc />
    public void Text(ReadOnlySpan<char> text, XmlTextKind kind)
    {
        NotInTag();
        if (text.IsEmpty)
        {
            return;
        }

        CheckChars(text);
        if (_depth == 0)
        {
            throw Error(XmlErrorCodes.XML0018);
        }

        CloseTag();
        if (kind == XmlTextKind.CData && !_canonical && text.IndexOf('\r') < 0)
        {
            PutCData(text);
        }
        else
        {
            PutEscaped(text, attribute: false);
        }
    }

    /// <inheritdoc />
    public void Comment(ReadOnlySpan<char> text)
    {
        NotInTag();
        CheckChars(text);
        if (text.IndexOf("--".AsSpan()) >= 0 || (!text.IsEmpty && text[text.Length - 1] == '-'))
        {
            throw Error(XmlErrorCodes.XML0014);
        }

        CloseTag();
        Put("<!--");
        Put(text);
        Put("-->");
    }

    /// <inheritdoc />
    public void ProcessingInstruction(XmlSymbol target, ReadOnlySpan<char> data)
    {
        NotInTag();
        var t = Str(target);
        if (!IsNCName(t))
        {
            throw Error(XmlErrorCodes.XML3004);
        }

        if (string.Equals(t, "xml", StringComparison.OrdinalIgnoreCase))
        {
            throw Error(XmlErrorCodes.XML0015);
        }

        CheckChars(data);
        if (data.IndexOf("?>".AsSpan()) >= 0)
        {
            throw Error(XmlErrorCodes.XML0011);
        }

        CloseTag();
        Put("<?");
        Put(t);
        if (!data.IsEmpty)
        {
            Put(' ');
            Put(data);
        }

        Put("?>");
    }

    /// <inheritdoc />
    public void Doctype(in XmlDoctypeInfo doctype)
    {
        NotInTag();
        if (_canonical)
        {
            return;
        }

        if (_depth > 0 || _rootSeen)
        {
            throw Error(XmlErrorCodes.XML0011);
        }

        if (doctype.Name.IsEmpty || XmlCharClass.NameLength(doctype.Name) != doctype.Name.Length)
        {
            throw Error(XmlErrorCodes.XML3004);
        }

        CheckChars(doctype.PublicId);
        CheckChars(doctype.SystemId);
        CheckChars(doctype.InternalSubset);
        if (HasBothQuotes(doctype.PublicId) || HasBothQuotes(doctype.SystemId))
        {
            throw Error(XmlErrorCodes.XML0011);
        }

        WriteDoctype(doctype);
    }

    /// <summary>Releases the buffers of the writer.</summary>
    public void Dispose() => _chars.Dispose();

    private static bool HasBothQuotes(ReadOnlySpan<char> text) => text.IndexOf('"') >= 0 && text.IndexOf('\'') >= 0;

    private void WriteDoctype(in XmlDoctypeInfo doctype)
    {
        Put("<!DOCTYPE ");
        Put(doctype.Name);
        if (!doctype.PublicId.IsEmpty)
        {
            Put(" PUBLIC ");
            PutQuoted(doctype.PublicId);
            Put(' ');
            PutQuoted(doctype.SystemId);
        }
        else if (!doctype.SystemId.IsEmpty)
        {
            Put(" SYSTEM ");
            PutQuoted(doctype.SystemId);
        }

        if (!doctype.InternalSubset.IsEmpty)
        {
            Put(" [");
            Put(doctype.InternalSubset);
            Put(']');
        }

        Put('>');
    }

    private void WriteStartTag()
    {
        Put('<');
        PutName(_pending);
        var span = _chars.WrittenSpan;
        for (var pass = 0; pass < (_canonical ? 1 : 2); pass++)
        {
            for (var i = 0; i < _entryCount; i++)
            {
                var e = _entries[i];
                if (e.Drop || (!_canonical && e.IsDecl != (pass == 0)))
                {
                    continue;
                }

                Put(' ');
                Put(span.Slice(e.NameStart, e.NameLen));
                Put("=\"");
                PutEscaped(span.Slice(e.ValStart, e.ValLen), attribute: true);
                Put('"');
            }
        }
    }

    private void Push(XmlName name, int mark)
    {
        if (_depth == _open.Length)
        {
            Array.Resize(ref _open, _depth * 2);
            Array.Resize(ref _openMark, _depth * 2);
        }

        _open[_depth] = name;
        _openMark[_depth] = mark;
        _depth++;
    }

    private void AddEntry(Entry entry)
    {
        if (_entryCount == _entries.Length)
        {
            Array.Resize(ref _entries, _entryCount * 2);
        }

        _entries[_entryCount++] = entry;
    }

    private void CloseTag()
    {
        if (_tagOpen)
        {
            _tagOpen = false;
            Put('>');
        }
    }

    private void InTag()
    {
        if (!_inTag)
        {
            throw new InvalidOperationException("The event is only valid between StartElement and EndAttributes.");
        }
    }

    private void NotInTag()
    {
        if (_inTag)
        {
            throw new InvalidOperationException("EndAttributes must end the start tag before this event.");
        }
    }

    private static XmlWriteException Error(XmlErrorCode code) => new(code);

    private static bool IsUtf16(XmlWriterOptions? options) =>
        options is { Canonical: false, Encoding: XmlWriterEncoding.Utf16 };
}
