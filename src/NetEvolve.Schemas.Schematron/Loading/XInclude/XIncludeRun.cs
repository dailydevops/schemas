namespace NetEvolve.Schemas.Schematron.Loading.XInclude;

using System;
using System.Collections.Generic;
using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Resources.Default;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.Xml.Model.Traversal;

/// <summary>
/// The state of one resolution: a sink between the traversal of a document and a tree builder. It forwards every event, except
/// that an <c>xi:include</c> element, which it reads, is replaced by the content of its resource, or by its <c>xi:fallback</c>
/// content when the resource cannot be used (see <see cref="XIncludeResolver"/>).
/// </summary>
internal sealed class XIncludeRun : IXmlSink, IDisposable
{
    private const string XIncludeNamespace = "http://www.w3.org/2001/XInclude";
    private const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";

    // Each nested include costs a few stack frames, so the nesting is capped apart from MaxDepth, which can be raised to the
    // ceiling. The path holds the root document too, hence the cap counts the includes below it.
    // The value is the default MaxDepth: the nesting of a document within the default limits is never refused by it.
    private const int MaxIncludeNesting = 256;

    private readonly XIncludeResolver _owner;
    private readonly IProblemSink _problems;
    private readonly CancellationToken _cancellationToken;
    private readonly XmlTreeBuilder _builder;
    private readonly List<Frame> _stack = [];
    private readonly List<string> _path = [];
    private readonly XmlSymbol _xiNamespace;
    private readonly XmlSymbol _includeName;
    private readonly XmlSymbol _fallbackName;
    private readonly XmlSymbol _xmlNamespace;
    private readonly XmlSymbol _baseName;
    private readonly XmlSymbol _hrefName;
    private readonly XmlSymbol _parseName;
    private readonly XmlSymbol _xpointerName;
    private readonly XmlSymbol _encodingName;
    private readonly XmlName _xmlBase;
    private string? _base;
    private int _documentBase;
    private int _skip;
    private int _depth;
    private bool _failed;
    private bool _baseSeen;
    private bool _positionFixed;
    private XmlPosition _fixedPosition;

    internal XIncludeRun(
        XIncludeResolver owner,
        IProblemSink problems,
        string? systemId,
        Uri? documentUri,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(problems);

        _owner = owner;
        _problems = problems;
        _base = systemId;
        _cancellationToken = cancellationToken;
        _builder = new XmlTreeBuilder(null, documentUri);
        var names = _builder.Names;
        _xiNamespace = names.Intern(XIncludeNamespace.AsSpan());
        _includeName = names.Intern("include".AsSpan());
        _fallbackName = names.Intern("fallback".AsSpan());
        _xmlNamespace = names.Intern(XmlNamespace.AsSpan());
        _baseName = names.Intern("base".AsSpan());
        _hrefName = names.Intern("href".AsSpan());
        _parseName = names.Intern("parse".AsSpan());
        _xpointerName = names.Intern("xpointer".AsSpan());
        _encodingName = names.Intern("encoding".AsSpan());
        _xmlBase = names.Intern("xml".AsSpan(), "base".AsSpan(), XmlNamespace.AsSpan());
    }

    public XmlNameTable Names => _builder.Names;

    internal bool Run(XmlNode document, out XmlNode result)
    {
        if (_base is not null)
        {
            _path.Add(_base);
        }

        XmlTreePump.Pump(document, this);
        if (_failed)
        {
            result = default;
            return false;
        }

        result = _builder.Complete().Document;
        return true;
    }

    public void Dispose() => _builder.Dispose();

    public void StartDocument(in XmlDeclaration declaration) => _builder.StartDocument(in declaration);

    public void EndDocument()
    {
        if (!_failed)
        {
            _builder.EndDocument();
        }
    }

    public void Doctype(in XmlDoctypeInfo doctype)
    {
        // The document type declaration of a pumped model is never reported; included ones are dropped.
    }

    public void StartElement(XmlName name, XmlPosition position)
    {
        if (_failed)
        {
            return;
        }

        if (_skip > 0)
        {
            _skip++;
            return;
        }

        if (name.NamespaceUri == _xiNamespace)
        {
            StartXInclude(name.Local, position);
        }
        else if (IsSuppressed())
        {
            _skip = 1;
        }
        else
        {
            Forward(name, position);
        }
    }

    public void NamespaceDeclaration(XmlSymbol prefix, XmlSymbol uri)
    {
        if (Forwarding())
        {
            _builder.NamespaceDeclaration(prefix, uri);
        }
    }

    public void Attribute(XmlName name, ReadOnlySpan<char> value)
    {
        if (_failed || _skip > 0)
        {
            return;
        }

        var top = Top;
        if (top.Kind == FrameKind.Include)
        {
            Collect(top.Include!, name, value);
        }
        else if (top.Kind == FrameKind.Plain)
        {
            if (top.FixBase && name.Local == _baseName && name.NamespaceUri == _xmlNamespace)
            {
                // The xml:base of an included top-level element is relative to the included document.
                _baseSeen = true;
                var resolved = UriReference.Resolve(value.ToString(), _base).Uri ?? value.ToString();
                _builder.Attribute(name, resolved.AsSpan());
            }
            else
            {
                _builder.Attribute(name, value);
            }
        }
    }

    public void EndAttributes()
    {
        if (_failed || _skip > 0)
        {
            return;
        }

        var top = Top;
        if (top.Kind == FrameKind.Plain)
        {
            if (top.FixBase && !_baseSeen)
            {
                _builder.Attribute(_xmlBase, _base.AsSpan());
            }

            _builder.EndAttributes();
        }
        else if (top.Kind == FrameKind.Include)
        {
            Include(top.Include!);
        }
    }

    public void EndElement(XmlName name)
    {
        if (_failed)
        {
            return;
        }

        if (_skip > 0)
        {
            _skip--;
            return;
        }

        var frame = Top;
        _stack.RemoveAt(_stack.Count - 1);
        if (frame.Kind == FrameKind.Plain)
        {
            _depth--;
            _builder.EndElement(name);
        }
        else if (frame.Kind == FrameKind.Include && frame.Include!.Failed && !frame.Include.HasFallback)
        {
            Fail(frame.Include.Problem);
        }
    }

    public void Text(ReadOnlySpan<char> text, XmlTextKind kind)
    {
        if (Forwarding())
        {
            _builder.Text(text, kind);
        }
    }

    public void Comment(ReadOnlySpan<char> text)
    {
        if (Forwarding())
        {
            _builder.Comment(text);
        }
    }

    public void ProcessingInstruction(XmlSymbol target, ReadOnlySpan<char> data)
    {
        if (Forwarding())
        {
            _builder.ProcessingInstruction(target, data);
        }
    }

    private Frame Top => _stack[_stack.Count - 1];

    /// <summary>Gets a value indicating whether the parent drops its content: an <c>xi:include</c>, or a <c>xi:fallback</c> not in use.</summary>
    private bool IsSuppressed() =>
        _stack.Count > 0 && (Top.Kind == FrameKind.Include || (Top.Kind == FrameKind.Fallback && !Top.Active));

    private bool Forwarding() => !_failed && _skip == 0 && !IsSuppressed();

    private DocumentLocation At(XmlPosition position) => new(_base, position.Line, position.Column);

    private void Fail(DiagnosticCode code, string message, XmlPosition position) =>
        Fail(new SchematronProblem(code, message, At(position)));

    private void Fail(in SchematronProblem problem)
    {
        _failed = true;
        _problems.Add(in problem);
    }

    private void Forward(XmlName name, XmlPosition position)
    {
        if (++_depth > _owner.Limits.MaxDepth)
        {
            Fail(
                CoreDiagnosticCodes.DepthLimitExceeded,
                "The element nesting after inclusion exceeds " + _owner.Limits.MaxDepth + ".",
                position
            );
            return;
        }

        _stack.Add(new Frame(FrameKind.Plain, false, null, _stack.Count > 0 && Top.Kind == FrameKind.Content));
        _baseSeen = false;
        _builder.StartElement(name, _positionFixed ? _fixedPosition : position);
    }

    private void StartXInclude(XmlSymbol local, XmlPosition position)
    {
        if (local == _includeName)
        {
            if (_stack.Count == _documentBase)
            {
                Fail(
                    SchematronDiagnosticCodes.ElementMisplaced,
                    "The xi:include element must not be the document element.",
                    position
                );
            }
            else if (Top.Kind == FrameKind.Include)
            {
                Fail(
                    SchematronDiagnosticCodes.ElementMisplaced,
                    "An xi:include element is only allowed in the xi:fallback child of an xi:include element.",
                    position
                );
            }
            else if (IsSuppressed())
            {
                _skip = 1;
            }
            else
            {
                _stack.Add(new Frame(FrameKind.Include, false, new IncludeState(position), false));
            }
        }
        else if (local == _fallbackName)
        {
            var include = _stack.Count == _documentBase || Top.Kind != FrameKind.Include ? null : Top.Include;
            if (include is null || include.HasFallback)
            {
                Fail(
                    SchematronDiagnosticCodes.ElementMisplaced,
                    "An xi:fallback element must be the only xi:fallback child of an xi:include element.",
                    position
                );
            }
            else
            {
                include.HasFallback = true;
                _stack.Add(new Frame(FrameKind.Fallback, include.Failed, null, false));
            }
        }
        else
        {
            Fail(
                SchematronDiagnosticCodes.ElementUnknown,
                "The element is in the XInclude namespace, which has only xi:include and xi:fallback.",
                position
            );
        }
    }

    private void Collect(IncludeState include, XmlName name, ReadOnlySpan<char> value)
    {
        if (!name.NamespaceUri.IsEmpty)
        {
            return;
        }

        if (name.Local == _hrefName)
        {
            include.Href = value.ToString();
        }
        else if (name.Local == _parseName)
        {
            include.Parse = value.ToString();
        }
        else if (name.Local == _xpointerName)
        {
            include.XPointer = value.ToString();
        }
        else if (name.Local == _encodingName)
        {
            include.Encoding = value.ToString();
        }
    }

    /// <summary>Reads the attributes of an <c>xi:include</c> element, loads its resource, and merges it.</summary>
    private void Include(IncludeState include)
    {
        var at = At(include.Position);
        var parse = include.Parse ?? "xml";
        var text = string.Equals(parse, "text", StringComparison.Ordinal);
        if (include.XPointer is not null)
        {
            Fail(
                SchematronDiagnosticCodes.Unsupported,
                "The xpointer attribute of xi:include is not supported yet.",
                include.Position
            );
        }
        else if (!text && !string.Equals(parse, "xml", StringComparison.Ordinal))
        {
            Fail(
                SchematronDiagnosticCodes.AttributeInvalid,
                "The parse attribute of xi:include must be 'xml' or 'text', not '" + parse + "'.",
                include.Position
            );
        }
        else if (include.Href is null)
        {
            Fail(
                SchematronDiagnosticCodes.AttributeMissing,
                "The xi:include element has no href attribute.",
                include.Position
            );
        }
        else if (include.Href.AsSpan().IndexOf('#') >= 0)
        {
            Fail(
                SchematronDiagnosticCodes.Unsupported,
                "The reference '" + include.Href + "' has a fragment identifier, which is not supported yet.",
                include.Position
            );
        }
        else
        {
            Load(include, text, at);
        }
    }

    private void Load(IncludeState include, bool text, DocumentLocation at)
    {
        var status = _owner.TryFetch(
            include.Href!,
            _base,
            at,
            _cancellationToken,
            out var content,
            out var systemId,
            out var problem
        );
        if (status == FetchStatus.Fatal)
        {
            Fail(in problem);
        }
        else if (status == FetchStatus.Recoverable)
        {
            include.Fail(in problem);
        }
        else if (text)
        {
            IncludeText(include, content, at);
        }
        else
        {
            IncludeXml(include, content, systemId, at);
        }
    }

    private void IncludeText(IncludeState include, ReadOnlyMemory<byte> content, DocumentLocation at)
    {
        if (XIncludeText.TryDecode(content, include.Encoding, out var text, out var code))
        {
            _builder.Text(text.AsSpan(), XmlTextKind.Text);
        }
        else
        {
            include.Fail(
                new SchematronProblem(
                    code,
                    code == SchematronDiagnosticCodes.Unsupported
                        ? "The encoding '" + include.Encoding + "' is not supported."
                        : "The text resource of xi:include cannot be decoded or holds a character XML does not allow.",
                    at
                )
            );
        }
    }

    private void IncludeXml(IncludeState include, ReadOnlyMemory<byte> content, string systemId, DocumentLocation at)
    {
        if (IsOnPath(systemId))
        {
            Fail(
                new SchematronProblem(
                    SchematronDiagnosticCodes.IncludeCycle,
                    "The reference closes a cycle through the document '" + systemId + "'.",
                    at
                )
            );
        }
        else if (_path.Count > MaxIncludeNesting)
        {
            Fail(
                CoreDiagnosticCodes.DepthLimitExceeded,
                "The nesting of xi:include elements exceeds " + MaxIncludeNesting + ".",
                include.Position
            );
        }
        else if (IncludeFetcher.TryParse(content.Span, "document", systemId, at, out var document, out var problem))
        {
            Splice(document, systemId, include.Position);
        }
        else
        {
            include.Fail(in problem);
        }
    }

    private void Splice(XmlNode document, string systemId, XmlPosition site)
    {
        var outerBase = _base;
        var outerDocumentBase = _documentBase;
        var outerFixed = _positionFixed;
        var outerPosition = _fixedPosition;

        _path.Add(systemId);
        _stack.Add(new Frame(FrameKind.Content, false, null, false));
        _base = systemId;
        _documentBase = _stack.Count;
        if (!_positionFixed)
        {
            _positionFixed = true;
            _fixedPosition = site;
        }

        for (var child = document.FirstChild; !child.IsEmpty && !_failed; child = child.NextSibling)
        {
            XmlTreePump.Pump(child, this);
        }

        if (_failed)
        {
            return;
        }

        _stack.RemoveAt(_stack.Count - 1);
        _path.RemoveAt(_path.Count - 1);
        _base = outerBase;
        _documentBase = outerDocumentBase;
        _positionFixed = outerFixed;
        _fixedPosition = outerPosition;
    }

    private bool IsOnPath(string systemId)
    {
        foreach (var entry in _path)
        {
            if (string.Equals(entry, systemId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private enum FrameKind : byte
    {
        /// <summary>An element that was forwarded to the builder.</summary>
        Plain = 0,

        /// <summary>The content of an included document: its top-level elements get the base fixup.</summary>
        Content = 1,

        /// <summary>An open <c>xi:include</c> element.</summary>
        Include = 2,

        /// <summary>An open <c>xi:fallback</c> element.</summary>
        Fallback = 3,
    }

    private readonly struct Frame
    {
        internal Frame(FrameKind kind, bool active, IncludeState? include, bool fixBase)
        {
            Kind = kind;
            Active = active;
            Include = include;
            FixBase = fixBase;
        }

        internal FrameKind Kind { get; }

        /// <summary>Gets a value indicating whether a fallback is in use, because its include failed.</summary>
        internal bool Active { get; }

        internal IncludeState? Include { get; }

        /// <summary>Gets a value indicating whether this is a top-level element of included content.</summary>
        internal bool FixBase { get; }
    }
}
