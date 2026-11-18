namespace NetEvolve.Schemas.Xml.Reader.EntityInput;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Declarations;
using NetEvolve.Schemas.Xml.Dtd.Entities;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Limits;
using NetEvolve.Schemas.Xml.Reader.Core;

/// <summary>
/// The entity input stack (design #108, AP20): it resolves a reference against the declarations of the document type
/// declaration, rejects recursion (section 4.1, WFC No Recursion), and charges the expansion budgets before the parser
/// reads the replacement text as a frame.
/// </summary>
/// <remarks>
/// The depth budget counts the open frames (<c>MaxEntityDepth</c>, <c>XML1110</c>); the expansion budget counts the
/// characters of the replacement text of every accepted reference, nested ones included (<c>MaxEntityExpansionCharacters</c>,
/// <c>XML1109</c>). Recursion is checked before depth so a cycle reports <c>XML0020</c> and not the depth limit. The
/// declarations are read from the doctype handler when the first reference arrives, because they exist only after the
/// document type declaration. Limitation: external entities are not expanded (XML-61, XML-62), so no external frame is
/// charged. Not thread-safe.
/// </remarks>
internal sealed class XmlEntityInputStack : IXmlEntityInputStack
{
    private readonly DtdDoctypeHandler _doctype;
    private XmlBudget _depth;
    private XmlBudget _expansion;
    private DtdEntityDeclaration[] _active = new DtdEntityDeclaration[4];
    private int _count;
    private DtdDeclarations? _source;
    private GeneralEntityResolver? _resolver;

    /// <summary>Initializes a new instance of the <see cref="XmlEntityInputStack"/> class.</summary>
    /// <param name="limits">The limits.</param>
    /// <param name="doctype">The doctype handler whose declarations the references are resolved against.</param>
    internal XmlEntityInputStack(XmlReaderLimits limits, DtdDoctypeHandler doctype)
    {
        Argument.ThrowIfNull(limits);
        Argument.ThrowIfNull(doctype);

        _doctype = doctype;
        _depth = XmlBudget.EntityDepth(limits);
        _expansion = XmlBudget.ExpansionCharacters(limits);
    }

    /// <inheritdoc />
    public XmlErrorCode? Enter(ReadOnlySpan<char> name, bool inAttributeValue, bool standalone, out string? replacement)
    {
        replacement = null;
        var declarations = _doctype.Declarations;
        if (_resolver is null || !ReferenceEquals(declarations, _source))
        {
            _source = declarations;
            _resolver = new GeneralEntityResolver(declarations);
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

        // The value of an internal entity is never null; the resolver returns no other kind.
        var text = entity.Value!;
        if (!_expansion.TryCharge(text.Length))
        {
            return _expansion.Code;
        }

        if (_count == _active.Length)
        {
            Array.Resize(ref _active, _count * 2);
        }

        _active[_count++] = entity;
        replacement = text;
        return null;
    }

    /// <inheritdoc />
    public void Leave()
    {
        _count--;
        _depth.Release(1);
    }
}
