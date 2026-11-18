namespace NetEvolve.Schemas.XPath.Casting.Core;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.QNames;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The casting table as data (Functions and Operators, 17.1): one row per source and target type. <c>cast as</c>, <c>castable as</c>,
/// and the constructor functions read it; a later package adds rows through <see cref="CastingCatalog.Stage"/> and edits no code here.
/// Immutable and thread-safe.
/// </summary>
/// <remarks>
/// Limitation: a pair without a row is not allowed (XPTY0004), so a source type whose rows a later package has yet to add (the types derived
/// from the primitive types, XP-23) cannot be cast to any type, not even to <c>xs:string</c>, which the specification allows.
/// </remarks>
internal sealed class CastingTable
{
    private readonly Dictionary<long, CastRow> _rows;
    private readonly List<XdmTypeId> _targets;
    private readonly List<XdmTypeId> _namespaceTargets = [];

    internal CastingTable(Dictionary<long, CastRow> rows, List<XdmTypeId> targets)
    {
        Argument.ThrowIfNull(rows);
        Argument.ThrowIfNull(targets);

        _rows = new Dictionary<long, CastRow>(rows);
        _targets = new List<XdmTypeId>(targets);
        foreach (var row in _rows.Values)
        {
            if (row.NamespaceConversion is not null && !_namespaceTargets.Contains(row.Target))
            {
                _namespaceTargets.Add(row.Target);
            }
        }
    }

    /// <summary>Gets the number of rows.</summary>
    public int RowCount => _rows.Count;

    /// <summary>Gets the target types that have at least one row; a constructor function exists for each.</summary>
    public IReadOnlyList<XdmTypeId> Targets => _targets;

    /// <summary>Gets the row of a pair of types.</summary>
    /// <param name="source">The source type.</param>
    /// <param name="target">The target type.</param>
    /// <param name="row">The row, <see langword="null"/> when the cast is not allowed.</param>
    /// <returns><see langword="true"/> when the table has a row for the pair.</returns>
    public bool TryGetRow(XdmTypeId source, XdmTypeId target, out CastRow? row) =>
        _rows.TryGetValue(Key(source, target), out row);

    /// <summary>Gets the cell of a pair of types.</summary>
    /// <param name="source">The source type.</param>
    /// <param name="target">The target type.</param>
    /// <returns>The cell; <see cref="CastCell.NotAllowed"/> when the table has no row for the pair.</returns>
    public CastCell GetCell(XdmTypeId source, XdmTypeId target) =>
        _rows.TryGetValue(Key(source, target), out var row) ? row.Cell : CastCell.NotAllowed;

    /// <summary>Determines whether a type is the target of at least one row.</summary>
    /// <param name="target">The type.</param>
    /// <returns><see langword="true"/> when a row has the type as its target.</returns>
    public bool IsTarget(XdmTypeId target) => _targets.Contains(target);

    /// <summary>Determines whether a cast to a type depends on the namespaces in scope: some row with the type as its target resolves the prefix of a lexical form.</summary>
    /// <param name="target">The type.</param>
    /// <returns><see langword="true"/> when a row with the type as its target takes the namespaces of the cast.</returns>
    public bool IsNamespaceSensitive(XdmTypeId target) => _namespaceTargets.Contains(target);

    /// <summary>Casts an atomic value without a static context: a row that depends on the namespaces resolves against none.</summary>
    /// <param name="source">The value; a node is not atomic and is not allowed.</param>
    /// <param name="target">The target type.</param>
    /// <param name="result">The converted value, <see langword="default"/> unless the status is <see cref="CastStatus.Ok"/>.</param>
    /// <returns>The status.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="target"/> is <see langword="default"/>.</exception>
    public CastStatus TryCast(in XdmItem source, XdmTypeId target, out XdmItem result)
    {
        Argument.ThrowIfLessThan(target.Value, 1);

        result = default;
        return !source.IsNode && _rows.TryGetValue(Key(source.TypeId, target), out var row)
            ? row.Conversion(in source, out result)
            : CastStatus.NotAllowed;
    }

    /// <summary>Casts an atomic value with the namespaces of the cast.</summary>
    /// <param name="source">The value; a node is not atomic and is not allowed.</param>
    /// <param name="target">The target type.</param>
    /// <param name="namespaces">The namespaces that resolve the prefix of a lexical form, <see langword="null"/> when the operand may not be cast from its lexical form; a row that does not depend on them ignores them.</param>
    /// <param name="result">The converted value, <see langword="default"/> unless the status is <see cref="CastStatus.Ok"/>.</param>
    /// <returns>The status.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="target"/> is <see langword="default"/>.</exception>
    public CastStatus TryCast(in XdmItem source, XdmTypeId target, INamespaceContext? namespaces, out XdmItem result)
    {
        Argument.ThrowIfLessThan(target.Value, 1);

        result = default;
        if (source.IsNode || !_rows.TryGetValue(Key(source.TypeId, target), out var row))
        {
            return CastStatus.NotAllowed;
        }

        return row.NamespaceConversion is { } conversion
            ? conversion(in source, namespaces, out result)
            : row.Conversion(in source, out result);
    }

    internal static long Key(XdmTypeId source, XdmTypeId target) => ((long)source.Value << 32) | (uint)target.Value;
}
