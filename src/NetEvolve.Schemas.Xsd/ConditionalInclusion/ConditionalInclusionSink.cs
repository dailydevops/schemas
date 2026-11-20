namespace NetEvolve.Schemas.Xsd.ConditionalInclusion;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.QNames;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Diagnostics;

/// <summary>
/// Removes the elements that the version control attributes exclude (Structures 1.1, 4.2.2), before the inner sink sees
/// them, so that no syntax check of the schema document looks at an excluded subtree.
/// </summary>
/// <remarks>
/// <para>
/// The start tag of an element is held until its attributes are complete, then the version control attributes decide: the
/// events of a kept element are replayed to the inner sink, the events of an excluded element and its descendants are
/// dropped. An excluded document element stays with its <c>targetNamespace</c> and version attributes and has no content.
/// An attribute with an invalid value is reported as <c>src-cip</c> and excludes nothing; it is not reported when another
/// attribute of the element excludes it, because the element is then absent from the effective document.
/// </para>
/// <para>
/// The filter is a no-op for <see cref="XsdVersion.V10"/>, which ignores the version control attributes (design #146 DP-3).
/// Limitation: an attribute is located at the start tag of its element (design #108 AP9). The sink is not thread-safe and
/// serves one document.
/// </para>
/// </remarks>
internal sealed class ConditionalInclusionSink : IXmlSink, INamespaceContext
{
    private const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";

    private readonly IXmlSink _inner;
    private readonly bool _passThrough;
    private readonly string? _baseUri;
    private readonly XsdIssueReporter _reporter;
    private readonly List<ScopedNamespace> _scope = [];
    private readonly List<int> _scopeMarks = [];
    private readonly List<ScopedNamespace> _declarations = [];
    private readonly List<HeldAttribute> _attributes = [];
    private readonly List<int> _invalid = [];
    private char[] _values = new char[256];
    private int _valuesLength;
    private XmlName _name;
    private XmlPosition _position;
    private int _depth;
    private int _skipDepth;
    private bool _keepsEmptyRoot;

    internal ConditionalInclusionSink(IXmlSink inner, XsdVersion version, string? baseUri, XsdIssueReporter reporter)
    {
        Argument.ThrowIfNull(inner);
        Argument.ThrowIfLessThan((int)version, (int)XsdVersion.V10);
        Argument.ThrowIfGreaterThan((int)version, (int)XsdVersion.V11);
        Argument.ThrowIfNull(reporter);

        _inner = inner;
        _passThrough = version == XsdVersion.V10;
        _baseUri = baseUri;
        _reporter = reporter;
    }

    /// <inheritdoc />
    public XmlNameTable Names => _inner.Names;

    /// <inheritdoc />
    public void StartDocument(in XmlDeclaration declaration) => _inner.StartDocument(in declaration);

    /// <inheritdoc />
    public void EndDocument() => _inner.EndDocument();

    /// <inheritdoc />
    public void StartElement(XmlName name, XmlPosition position)
    {
        if (_passThrough)
        {
            _inner.StartElement(name, position);
            return;
        }

        if (_skipDepth > 0)
        {
            _skipDepth++;
            return;
        }

        _name = name;
        _position = position;
        _declarations.Clear();
        _attributes.Clear();
        _valuesLength = 0;
    }

    /// <inheritdoc />
    public void NamespaceDeclaration(XmlSymbol prefix, XmlSymbol uri)
    {
        if (_passThrough)
        {
            _inner.NamespaceDeclaration(prefix, uri);
        }
        else if (_skipDepth == 0)
        {
            _declarations.Add(new ScopedNamespace(prefix, uri));
        }
    }

    /// <inheritdoc />
    public void Attribute(XmlName name, ReadOnlySpan<char> value)
    {
        if (_passThrough)
        {
            _inner.Attribute(name, value);
            return;
        }

        if (_skipDepth > 0)
        {
            return;
        }

        if (_valuesLength + value.Length > _values.Length)
        {
            Array.Resize(ref _values, Math.Max(_values.Length * 2, _valuesLength + value.Length));
        }

        value.CopyTo(_values.AsSpan(_valuesLength));
        _attributes.Add(new HeldAttribute(name, _valuesLength, value.Length));
        _valuesLength += value.Length;
    }

    /// <inheritdoc />
    public void EndAttributes()
    {
        if (_passThrough)
        {
            _inner.EndAttributes();
            return;
        }

        if (_skipDepth > 0)
        {
            return;
        }

        _scopeMarks.Add(_scope.Count);
        _scope.AddRange(_declarations);
        var exclude = Evaluate();
        var isRoot = _depth == 0;
        if (!exclude)
        {
            ReportInvalid(onlyVersions: false);
            Replay(onlyRootAttributes: false);
            _depth++;
            return;
        }

        PopScope();
        _skipDepth = 1;
        if (isRoot)
        {
            // The effective document keeps the version attributes of an excluded root, so their errors stay.
            ReportInvalid(onlyVersions: true);
            Replay(onlyRootAttributes: true);
            _keepsEmptyRoot = true;
        }
    }

    /// <inheritdoc />
    public void EndElement(XmlName name)
    {
        if (_passThrough)
        {
            _inner.EndElement(name);
            return;
        }

        if (_skipDepth > 0)
        {
            _skipDepth--;
            if (_skipDepth == 0 && _keepsEmptyRoot)
            {
                _keepsEmptyRoot = false;
                _inner.EndElement(name);
            }

            return;
        }

        _inner.EndElement(name);
        PopScope();
        _depth--;
    }

    /// <inheritdoc />
    public void Text(ReadOnlySpan<char> text, XmlTextKind kind)
    {
        if (_skipDepth == 0)
        {
            _inner.Text(text, kind);
        }
    }

    /// <inheritdoc />
    public void Comment(ReadOnlySpan<char> text)
    {
        if (_skipDepth == 0)
        {
            _inner.Comment(text);
        }
    }

    /// <inheritdoc />
    public void ProcessingInstruction(XmlSymbol target, ReadOnlySpan<char> data)
    {
        if (_skipDepth == 0)
        {
            _inner.ProcessingInstruction(target, data);
        }
    }

    /// <inheritdoc />
    public void Doctype(in XmlDoctypeInfo doctype) => _inner.Doctype(in doctype);

    /// <inheritdoc />
    bool INamespaceContext.TryResolvePrefix(ReadOnlySpan<char> prefix, out string? namespaceName)
    {
        for (var i = _scope.Count - 1; i >= 0; i--)
        {
            if (Names.GetString(_scope[i].Prefix).AsSpan().SequenceEqual(prefix))
            {
                namespaceName = Names.GetString(_scope[i].Uri);
                return true;
            }
        }

        var isXml = prefix.SequenceEqual("xml".AsSpan());
        namespaceName = isXml ? XmlNamespace : null;
        return isXml;
    }

    private void PopScope()
    {
        var mark = _scopeMarks[_scopeMarks.Count - 1];
        _scope.RemoveRange(mark, _scope.Count - mark);
        _scopeMarks.RemoveAt(_scopeMarks.Count - 1);
    }

    // Returns true when an attribute excludes the element; the attributes with an invalid value are left in _invalid.
    private bool Evaluate()
    {
        var exclude = false;
        _invalid.Clear();
        for (var i = 0; i < _attributes.Count; i++)
        {
            var attribute = _attributes[i];
            if (
                !string.Equals(
                    Names.GetString(attribute.Name.NamespaceUri),
                    VersionControl.Namespace,
                    StringComparison.Ordinal
                )
            )
            {
                continue;
            }

            var outcome = VersionControl.Evaluate(
                Names.GetString(attribute.Name.Local),
                new ReadOnlySpan<char>(_values, attribute.Start, attribute.Length),
                this
            );
            if (outcome == VersionControlOutcome.Exclude)
            {
                exclude = true;
            }
            else if (outcome == VersionControlOutcome.Invalid)
            {
                _invalid.Add(i);
            }
        }

        return exclude;
    }

    private void ReportInvalid(bool onlyVersions)
    {
        for (var i = 0; i < _invalid.Count; i++)
        {
            var attribute = _attributes[_invalid[i]];
            var local = Names.GetString(attribute.Name.Local);
            if (onlyVersions && local is not (VersionControl.MinVersion or VersionControl.MaxVersion))
            {
                continue;
            }

            _reporter.Report(
                XsdRuleCodes.SrcCip,
                new DocumentLocation(_baseUri, _position.Line, _position.Column),
                "The value '"
                    + new string(_values, attribute.Start, attribute.Length)
                    + "' of the attribute 'vc:"
                    + local
                    + "' is not valid."
            );
        }
    }

    // An excluded document element keeps only targetNamespace, vc:minVersion, and vc:maxVersion (and its namespace declarations).
    private void Replay(bool onlyRootAttributes)
    {
        _inner.StartElement(_name, _position);
        for (var i = 0; i < _declarations.Count; i++)
        {
            _inner.NamespaceDeclaration(_declarations[i].Prefix, _declarations[i].Uri);
        }

        for (var i = 0; i < _attributes.Count; i++)
        {
            var attribute = _attributes[i];
            if (onlyRootAttributes && !IsRootAttribute(attribute.Name))
            {
                continue;
            }

            _inner.Attribute(attribute.Name, new ReadOnlySpan<char>(_values, attribute.Start, attribute.Length));
        }

        _inner.EndAttributes();
    }

    private bool IsRootAttribute(XmlName name)
    {
        var namespaceName = Names.GetString(name.NamespaceUri);
        var local = Names.GetString(name.Local);
        return namespaceName.Length == 0
            ? string.Equals(local, "targetNamespace", StringComparison.Ordinal)
            : string.Equals(namespaceName, VersionControl.Namespace, StringComparison.Ordinal)
                && local is VersionControl.MinVersion or VersionControl.MaxVersion;
    }

    [StructLayout(LayoutKind.Auto)]
    private readonly struct ScopedNamespace
    {
        internal ScopedNamespace(XmlSymbol prefix, XmlSymbol uri)
        {
            Prefix = prefix;
            Uri = uri;
        }

        internal XmlSymbol Prefix { get; }

        internal XmlSymbol Uri { get; }
    }

    [StructLayout(LayoutKind.Auto)]
    private readonly struct HeldAttribute
    {
        internal HeldAttribute(XmlName name, int start, int length)
        {
            Name = name;
            Start = start;
            Length = length;
        }

        internal XmlName Name { get; }

        internal int Start { get; }

        internal int Length { get; }
    }
}
