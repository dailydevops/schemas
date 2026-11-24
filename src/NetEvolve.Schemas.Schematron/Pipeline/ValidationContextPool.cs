namespace NetEvolve.Schemas.Schematron.Pipeline;

using System.Collections.Concurrent;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Text.NameTable;

/// <summary>
/// The pooled per-validation state of a compiled schema (SCH-71, #340): a name table that already holds the names of the schema, so
/// reading an instance interns only the names the schema does not mention. A validation rents a context and returns it; the compiled
/// schema itself is never written.
/// </summary>
internal sealed class ValidationContextPool
{
    /// <summary>The number of names a table may gain from instances before its context is dropped instead of pooled.</summary>
    internal const int MaxGrowth = 4096;

    private const int MaxPooled = 64;

    private readonly IReadOnlyList<string> _names;
    private readonly ConcurrentQueue<ValidationContext> _free = new();

    internal ValidationContextPool(IReadOnlyList<string> names)
    {
        ArgumentNullException.ThrowIfNull(names);

        _names = names;
    }

    /// <summary>Takes a free context, or creates one.</summary>
    /// <returns>A context that no other validation uses.</returns>
    internal ValidationContext Rent()
    {
        if (_free.TryDequeue(out var context))
        {
            return context;
        }

        var table = new SpanInterningNameTable(XmlReaderLimits.Default.MaxDistinctNames);
        foreach (var name in _names)
        {
            _ = table.Intern(name.AsSpan());
        }

        return new ValidationContext(table, table.Count + MaxGrowth);
    }

    /// <summary>Gives a context back; one whose table grew past the bound, or one that finds the pool full, is dropped.</summary>
    /// <param name="context">The context.</param>
    internal void Return(ValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Names.Count <= context.Limit && _free.Count < MaxPooled)
        {
            _free.Enqueue(context);
        }
    }
}
