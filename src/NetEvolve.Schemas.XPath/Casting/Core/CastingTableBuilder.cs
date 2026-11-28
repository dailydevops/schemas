namespace NetEvolve.Schemas.XPath.Casting.Core;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>What a registration receives: it adds rows to the casting table, and nothing else.</summary>
internal sealed class CastingTableBuilder
{
    private readonly Dictionary<long, CastRow> _rows = [];
    private readonly List<XdmTypeId> _targets = [];

    internal CastingTableBuilder() { }

    /// <summary>Adds a row.</summary>
    /// <param name="row">The row.</param>
    /// <exception cref="ArgumentNullException"><paramref name="row"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">A row for the same source and target type exists.</exception>
    public void Add(CastRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        var key = CastingTable.Key(row.Source, row.Target);
        if (_rows.ContainsKey(key))
        {
            throw new InvalidOperationException(
                $"A row for the cast from type {row.Source.Value} to type {row.Target.Value} is registered twice."
            );
        }

        if (!_targets.Contains(row.Target))
        {
            _targets.Add(row.Target);
        }

        _rows.Add(key, row);
    }

    internal CastingTable Build() => new(_rows, _targets);
}
