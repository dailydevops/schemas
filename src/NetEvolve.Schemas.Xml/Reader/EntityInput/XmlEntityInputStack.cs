namespace NetEvolve.Schemas.Xml.Reader.EntityInput;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Declarations;
using NetEvolve.Schemas.Xml.Dtd.Entities;
using NetEvolve.Schemas.Xml.Dtd.External.General;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Limits;
using NetEvolve.Schemas.Xml.Reader.Core;

/// <summary>
/// The entity input stack (design #108, AP20): it resolves a reference against the declarations of the document type
/// declaration, rejects recursion (section 4.1, WFC No Recursion), and charges the expansion budgets before the parser
/// reads the replacement text as a frame.
/// </summary>
/// <remarks>
/// The depth budget counts the open frames, internal and external (<c>MaxEntityDepth</c>, <c>XML1110</c>); the expansion
/// budget counts the characters of the replacement text of every accepted reference, nested ones included
/// (<c>MaxEntityExpansionCharacters</c>, <c>XML1109</c>). Recursion is checked before depth so a cycle reports
/// <c>XML0020</c> and not the depth limit. The declarations are read from the doctype handler when the first reference
/// arrives, because they exist only after the document type declaration. With options of external access (the doctype
/// handler has an external reader) an external parsed entity in content is requested through the loader of the document
/// (XML-62): the characters of an external entity are charged to the expansion budget of the loader as the parser reads
/// them, and the characters of the internal entities are charged there too, so the internal and the external entities
/// share one expansion budget with the external subset, and one depth budget through this stack. Not thread-safe.
/// </remarks>
internal sealed class XmlEntityInputStack : IXmlEntityInputStack
{
    private readonly DtdDoctypeHandler _doctype;
    private readonly ExternalGeneralEntities? _external;
    private XmlBudget _depth;
    private XmlBudget _expansion;
    private DtdEntityDeclaration[] _active = new DtdEntityDeclaration[4];
    private string?[] _bases = new string?[4];
    private int _count;
    private DtdDeclarations? _source;
    private GeneralEntityResolver? _resolver;

    /// <summary>Initializes a new instance of the <see cref="XmlEntityInputStack"/> class.</summary>
    /// <param name="limits">The limits.</param>
    /// <param name="doctype">The doctype handler whose declarations the references are resolved against.</param>
    internal XmlEntityInputStack(XmlReaderLimits limits, DtdDoctypeHandler doctype)
    {
        ArgumentNullException.ThrowIfNull(limits);
        ArgumentNullException.ThrowIfNull(doctype);

        _doctype = doctype;
        _depth = XmlBudget.EntityDepth(limits);
        _expansion = XmlBudget.ExpansionCharacters(limits);
        _external = doctype.External is { } reader ? new ExternalGeneralEntities(reader.Loader) : null;
    }

    /// <inheritdoc />
    public string? BaseUri => _count == 0 ? _doctype.BaseUri : _bases[_count - 1];

    /// <inheritdoc />
    public XmlErrorCode? Enter(
        ReadOnlySpan<char> name,
        bool inAttributeValue,
        bool standalone,
        out string? replacement,
        out IXmlEntityText? external
    )
    {
        replacement = null;
        external = null;
        var declarations = _doctype.Declarations;
        if (_resolver is null || !ReferenceEquals(declarations, _source))
        {
            _source = declarations;
            _resolver = new GeneralEntityResolver(declarations, _external is not null);
        }

        var error = _resolver.Resolve(name, inAttributeValue, standalone, out var entity);
        if (error is not null || entity is null)
        {
            return error;
        }

        for (var i = 0; i < _count; i++)
        {
            if (ReferenceEquals(_active[i], entity))
            {
                return XmlErrorCodes.XML0020;
            }
        }

        if (!_depth.TryCharge(1))
        {
            return _depth.Code;
        }

        // The resolver hands out an external parsed entity only when the loader is there to open it.
        if (entity.Value is null)
        {
            return EnterExternal(entity, out external);
        }

        var text = entity.Value;
        if (!_expansion.TryCharge(text.Length) || (_external is not null && !_external.TryChargeExpansion(text.Length)))
        {
            return _expansion.Code;
        }

        Push(entity, BaseUri);
        replacement = text;
        return null;
    }

    /// <inheritdoc />
    public void Leave()
    {
        _count--;
        _depth.Release(1);
    }

    private XmlErrorCode? EnterExternal(DtdEntityDeclaration entity, out IXmlEntityText? text)
    {
        if (!_external!.TryOpen(entity, out text, out var error))
        {
            _depth.Release(1);
            return error;
        }

        Push(entity, text!.BaseUri);
        return null;
    }

    private void Push(DtdEntityDeclaration entity, string? baseUri)
    {
        if (_count == _active.Length)
        {
            Array.Resize(ref _active, _count * 2);
            Array.Resize(ref _bases, _count * 2);
        }

        _active[_count] = entity;
        _bases[_count] = baseUri;
        _count++;
    }
}
