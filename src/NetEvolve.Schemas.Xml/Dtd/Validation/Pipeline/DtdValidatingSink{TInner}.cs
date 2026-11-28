namespace NetEvolve.Schemas.Xml.Dtd.Validation.Pipeline;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NetEvolve.Schemas;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Reader.Pipeline;

/// <summary>
/// The decorating sink of the validating reader mode (design #122, D1 and D2): it forwards every event unchanged to the
/// inner sink and then fans a copy out to the ordered, independent validation steps.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IDtdValidationStep.BeginDocument"/> fires once: at the document type declaration, which the reader reports only
/// after the internal subset and, when external access is enabled, the external subset and the external parameter entities
/// are read, or, for a document without one, at the first start tag with an empty model. A start tag reaches the steps when it
/// is complete (at <see cref="EndAttributes"/>), with the attributes the reader reported after it applied the defaults.
/// Comments and processing instructions reach the steps only inside the root element. Every event reaches the steps in
/// their registration order. <see cref="EndDocument"/> reaches them only when the reader ends the document, so a fatal status
/// of the reader skips it. Once a step stops the run (<see cref="DtdValidationContext.Stop"/>) no further event reaches a
/// step, while the inner sink still receives the rest of the document.
/// </para>
/// <para>
/// The sink is a class because the reader offers <see cref="IXmlAttributeInfoSink.AttributeInfo"/> only to a sink of a
/// reference type; the inner sink may be a struct, which this sink keeps in a field and hands back through <see cref="Inner"/>.
/// The attribute facts are copied, so the memory the steps see stays valid until the call that delivers it returns.
/// </para>
/// <para>
/// Limitations: the reader reports no event for an entity reference (#647), so <see cref="IDtdValidationStep.EntityReference"/>
/// is never called and the constraints that depend on it (Entity Declared in its validity form, content in an EMPTY element
/// that comes from a reference, the standalone clause for entities) are not reported. The sink events carry no position for
/// text, comments, processing instructions, and end tags; the steps receive the position of the reader after the token,
/// not the start of it. The location of the document type declaration is the position after it; a document without one
/// is located at its start. The reader is a reader over complete in-memory input.
/// </para>
/// </remarks>
/// <typeparam name="TInner">The type of the inner sink.</typeparam>
internal sealed class DtdValidatingSink<TInner> : IXmlAttributeInfoSink
    where TInner : IXmlSink
{
    private readonly XmlPipelineReader _reader;
    private readonly IReadOnlyList<IDtdValidationStep> _steps;
    private readonly DtdValidationOptions _options;
    private readonly ValidationIssueCollector _issues;
    private readonly string? _documentUri;
    private readonly IXmlAttributeInfoSink? _innerInfo;
    private readonly Dictionary<XmlName, string> _names = [];
    private readonly Box _box;
    private DtdValidationContext? _context;
    private bool _isStandalone;
    private int _depth;
    private string _elementName = string.Empty;
    private XmlPosition _elementPosition;
    private char[] _arena = new char[256];
    private int _arenaLength;
    private Fact[] _facts = new Fact[8];
    private int _factCount;
    private ValidationAttributeInfo[] _infos = new ValidationAttributeInfo[8];

    /// <summary>Initializes a new instance of the <see cref="DtdValidatingSink{TInner}"/> class.</summary>
    /// <param name="reader">The reader that pumps into this sink; it supplies the declaration model and the position.</param>
    /// <param name="inner">The inner sink that receives every event unchanged.</param>
    /// <param name="steps">The steps in registration order.</param>
    /// <param name="options">The options.</param>
    /// <param name="issues">The collector that receives the issues of the steps.</param>
    /// <param name="documentUri">The URI of the document, <see langword="null"/> when unknown.</param>
    internal DtdValidatingSink(
        XmlPipelineReader reader,
        TInner inner,
        IReadOnlyList<IDtdValidationStep> steps,
        DtdValidationOptions options,
        ValidationIssueCollector issues,
        string? documentUri
    )
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(steps);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(issues);

        _reader = reader;
        _box = new Box(inner);
        _steps = steps;
        _options = options;
        _issues = issues;
        _documentUri = documentUri;
        _innerInfo = typeof(TInner).IsValueType ? null : inner as IXmlAttributeInfoSink;
    }

    /// <summary>Gets the inner sink; for a struct it holds the state that the events changed.</summary>
    internal TInner Inner => _box.Value;

    /// <inheritdoc />
    public XmlNameTable Names => _box.Value.Names;

    /// <inheritdoc />
    public void StartDocument(in XmlDeclaration declaration)
    {
        _isStandalone = declaration.Standalone == true;
        _box.Value.StartDocument(in declaration);
    }

    /// <inheritdoc />
    public void Doctype(in XmlDoctypeInfo doctype)
    {
        _box.Value.Doctype(in doctype);
        Begin(_reader.Declarations!, Here());
    }

    /// <inheritdoc />
    public void StartElement(XmlName name, XmlPosition position)
    {
        _box.Value.StartElement(name, position);
        if (_context is null)
        {
            Begin(
                new DtdDeclarationBuilder(string.Empty, null, null).Build(),
                new DocumentLocation(_documentUri, 1, 1)
            );
        }

        _depth++;
        _elementName = NameOf(name);
        _elementPosition = position;
        _arenaLength = 0;
        _factCount = 0;
    }

    /// <inheritdoc />
    public void NamespaceDeclaration(XmlSymbol prefix, XmlSymbol uri) => _box.Value.NamespaceDeclaration(prefix, uri);

    /// <inheritdoc />
    public void Attribute(XmlName name, ReadOnlySpan<char> value) => _box.Value.Attribute(name, value);

    /// <inheritdoc />
    public void AttributeInfo(in ValidationAttributeInfo attribute)
    {
        _innerInfo?.AttributeInfo(in attribute);

        if (_factCount == _facts.Length)
        {
            Array.Resize(ref _facts, _factCount * 2);
        }

        var name = attribute.Name.Span;
        var value = attribute.Value.Span;
        Reserve(name.Length + value.Length);
        var nameStart = _arenaLength;
        name.CopyTo(_arena.AsSpan(nameStart));
        var valueStart = nameStart + name.Length;
        value.CopyTo(_arena.AsSpan(valueStart));
        _arenaLength = valueStart + value.Length;
        _facts[_factCount++] = new Fact(nameStart, name.Length, valueStart, value.Length, in attribute);
    }

    /// <inheritdoc />
    public void EndAttributes()
    {
        _box.Value.EndAttributes();
        if (_issues.IsStopped)
        {
            return;
        }

        if (_infos.Length < _factCount)
        {
            _infos = new ValidationAttributeInfo[Math.Max(_infos.Length * 2, _factCount)];
        }

        // The arena is final for this tag, so its slices stay valid while the steps run.
        for (var i = 0; i < _factCount; i++)
        {
            _infos[i] = _facts[i].ToInfo(_arena);
        }

        var element = new ValidationElement(_elementName.AsMemory(), _elementPosition, _infos.AsMemory(0, _factCount));
        for (var i = 0; i < _steps.Count; i++)
        {
            _steps[i].StartElement(in element);
        }
    }

    /// <inheritdoc />
    public void EndElement(XmlName name)
    {
        _box.Value.EndElement(name);
        _depth--;
        if (_issues.IsStopped)
        {
            return;
        }

        var element = new ValidationElement(NameOf(name).AsMemory(), _reader.Position);
        for (var i = 0; i < _steps.Count; i++)
        {
            _steps[i].EndElement(in element);
        }
    }

    /// <inheritdoc />
    public void Text(ReadOnlySpan<char> text, XmlTextKind kind)
    {
        _box.Value.Text(text, kind);
        if (_issues.IsStopped)
        {
            return;
        }

        var position = _reader.Position;
        for (var i = 0; i < _steps.Count; i++)
        {
            _steps[i].Text(text, kind, in position);
        }
    }

    /// <inheritdoc />
    public void Comment(ReadOnlySpan<char> text)
    {
        _box.Value.Comment(text);
        if (_depth == 0 || _issues.IsStopped)
        {
            return;
        }

        var position = _reader.Position;
        for (var i = 0; i < _steps.Count; i++)
        {
            _steps[i].Comment(in position);
        }
    }

    /// <inheritdoc />
    public void ProcessingInstruction(XmlSymbol target, ReadOnlySpan<char> data)
    {
        _box.Value.ProcessingInstruction(target, data);
        if (_depth == 0 || _issues.IsStopped)
        {
            return;
        }

        var position = _reader.Position;
        for (var i = 0; i < _steps.Count; i++)
        {
            _steps[i].ProcessingInstruction(in position);
        }
    }

    /// <inheritdoc />
    public void EndDocument()
    {
        _box.Value.EndDocument();
        if (_issues.IsStopped)
        {
            return;
        }

        for (var i = 0; i < _steps.Count; i++)
        {
            _steps[i].EndDocument();
        }
    }

    private DocumentLocation Here()
    {
        var position = _reader.Position;

        return new DocumentLocation(_documentUri, position.Line, position.Column);
    }

    private void Begin(IDtdDeclarations declarations, DocumentLocation location)
    {
        _context = new DtdValidationContext(declarations, _options, _issues, _isStandalone, _documentUri, location);
        for (var i = 0; i < _steps.Count; i++)
        {
            _steps[i].BeginDocument(_context);
        }
    }

    // The name as written, with the prefix; the steps compare it with the names of the declarations.
    private string NameOf(XmlName name)
    {
        if (_names.TryGetValue(name, out var cached))
        {
            return cached;
        }

        var names = _box.Value.Names;
        var local = names.GetString(name.Local);
        var written = name.Prefix.IsEmpty ? local : names.GetString(name.Prefix) + ":" + local;
        _names.Add(name, written);

        return written;
    }

    private void Reserve(int length)
    {
        if (_arena.Length - _arenaLength < length)
        {
            Array.Resize(ref _arena, Math.Max(_arena.Length * 2, _arenaLength + length));
        }
    }

    // Holds the inner sink so that a struct is changed in place; a readonly field would hand out copies.
    private sealed class Box(TInner value)
    {
        internal TInner Value = value;
    }

    // The offsets of one attribute in the arena and its facts, until the tag is complete and the slices can be cut.
    [StructLayout(LayoutKind.Auto)]
    private readonly struct Fact
    {
        private readonly int _nameStart;
        private readonly int _nameLength;
        private readonly int _valueStart;
        private readonly int _valueLength;
        private readonly DtdAttributeType? _declaredType;
        private readonly bool _isSpecified;
        private readonly XmlPosition _position;
        private readonly bool _isChanged;

        internal Fact(int nameStart, int nameLength, int valueStart, int valueLength, in ValidationAttributeInfo source)
        {
            _nameStart = nameStart;
            _nameLength = nameLength;
            _valueStart = valueStart;
            _valueLength = valueLength;
            _declaredType = source.DeclaredType;
            _isSpecified = source.IsSpecified;
            _position = source.Position;
            _isChanged = source.IsChangedByNormalization;
        }

        internal ValidationAttributeInfo ToInfo(char[] arena) =>
            new(
                arena.AsMemory(_nameStart, _nameLength),
                arena.AsMemory(_valueStart, _valueLength),
                _declaredType,
                _isSpecified,
                _position,
                _isChanged
            );
    }
}
