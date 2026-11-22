namespace NetEvolve.Schemas.XPath.Casting.Core;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Types;

/// <summary>Generates the constructor functions from the casting table: one <c>xs:T</c> function for every target type of a row.</summary>
internal static class ConstructorFunctions
{
    /// <summary>Creates the constructor functions.</summary>
    /// <param name="table">The casting table.</param>
    /// <returns>One function for each target type of the table; a target type without a name in the type table gets none.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="table"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<ConstructorFunction> Create(CastingTable table)
    {
        ArgumentNullException.ThrowIfNull(table);

        var functions = new List<ConstructorFunction>(table.Targets.Count);
        for (var i = 0; i < table.Targets.Count; i++)
        {
            var target = table.Targets[i];
            if (XdmTypes.NameOf(target) is { } name)
            {
                functions.Add(new ConstructorFunction(new XPathQName(XdmTypes.SchemaNamespace, name), target, table));
            }
        }

        return functions;
    }
}
