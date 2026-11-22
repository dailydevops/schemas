namespace NetEvolve.Schemas.Xml.Dtd.Attributes;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Dtd.Declarations;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Text.NameTable;

/// <summary>
/// The attribute definitions of a document by element type name as written: the lookup allocates nothing, and the
/// definitions of an element type are compiled on its first use. It reads the declarations of the doctype handler when the
/// first element arrives, because they exist only after the document type declaration, and again when they were replaced.
/// </summary>
/// <remarks>Not thread-safe; one table serves one reader.</remarks>
internal sealed class DtdAttributeTable
{
    private readonly DtdDoctypeHandler _doctype;
    private DtdDeclarations? _source;
    private SpanInterningNameTable? _elementNames;
    private DtdElementAttributes?[] _compiled = [];

    /// <summary>Initializes a new instance of the <see cref="DtdAttributeTable"/> class.</summary>
    /// <param name="doctype">The doctype handler whose declarations the table reads.</param>
    internal DtdAttributeTable(DtdDoctypeHandler doctype)
    {
        ArgumentNullException.ThrowIfNull(doctype);

        _doctype = doctype;
    }

    /// <summary>Finds the compiled attribute definitions of an element type.</summary>
    /// <param name="elementName">The element name as written, with its prefix.</param>
    /// <returns>The definitions, <see langword="null"/> when no attribute list declares the element type.</returns>
    internal DtdElementAttributes? Find(ReadOnlySpan<char> elementName)
    {
        var declarations = _doctype.Declarations;
        if (!ReferenceEquals(declarations, _source))
        {
            Rebuild(declarations);
        }

        if (_elementNames is null || !_elementNames.TryGet(elementName, out var symbol))
        {
            return null;
        }

        return _compiled[symbol.Value] ??= new DtdElementAttributes(
            _source!.AttributeLists[_elementNames.GetString(symbol)]
        );
    }

    private void Rebuild(DtdDeclarations? declarations)
    {
        _source = declarations;
        _elementNames = null;
        _compiled = [];
        if (declarations is null || declarations.AttributeLists.Count == 0)
        {
            return;
        }

        var count = declarations.AttributeLists.Count + 1;
        _elementNames = new SpanInterningNameTable(count, count);
        _compiled = new DtdElementAttributes?[count];
        foreach (var element in declarations.AttributeLists.Keys)
        {
            _ = _elementNames.TryIntern(element.AsSpan(), out _);
        }
    }
}
