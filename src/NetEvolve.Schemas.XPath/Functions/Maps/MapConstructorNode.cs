namespace NetEvolve.Schemas.XPath.Functions.Maps;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Atomization;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Maps;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The executable form of a map constructor (XPath 3.1, 3.11.1.1): the key of each entry is atomized and must be one atomic value, the
/// value is any sequence, and two entries with the same key (<see cref="MapKeys"/>) are XQDY0137.
/// </summary>
internal sealed class MapConstructorNode : ExecutableNode
{
    private readonly ExecutableNode[] _keys;
    private readonly ExecutableNode[] _values;
    private readonly SourceSpan[] _locations;

    private MapConstructorNode(ExecutableNode[] keys, ExecutableNode[] values, SourceSpan[] locations)
    {
        _keys = keys;
        _values = values;
        _locations = locations;
    }

    /// <summary>Lowers a map constructor.</summary>
    /// <param name="node">The constructor.</param>
    /// <param name="context">The lowering context.</param>
    /// <returns>The executable node.</returns>
    /// <exception cref="System.ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public static ExecutableNode Lower(MapConstructor node, LoweringContext context)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(context);

        var count = node.Entries.Count;
        var keys = new ExecutableNode[count];
        var values = new ExecutableNode[count];
        var locations = new SourceSpan[count];
        for (var i = 0; i < count; i++)
        {
            keys[i] = context.Lower(node.Entries[i].Key);
            values[i] = context.Lower(node.Entries[i].Value);
            locations[i] = node.Entries[i].Location;
        }

        return new MapConstructorNode(keys, values, locations);
    }

    /// <inheritdoc />
    public override XdmSequence Evaluate(XPathEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var map = XdmMap.Empty;
        for (var i = 0; i < _keys.Length; i++)
        {
            var key = XPathAtomization.Atomize(_keys[i].Evaluate(context));
            var location = _locations[i];
            if (key.Count != 1)
            {
                throw XPathErrors.Exception(XPathErrorCodes.XPTY0004, location.Offset, location.Length);
            }

            if (map.TryGetEntry(key[0], out _))
            {
                throw XPathErrors.Exception(XPathErrorCodes.XQDY0137, location.Offset, location.Length);
            }

            map = map.Put(key[0], _values[i].Evaluate(context));
        }

        return map.ToSequence();
    }
}
