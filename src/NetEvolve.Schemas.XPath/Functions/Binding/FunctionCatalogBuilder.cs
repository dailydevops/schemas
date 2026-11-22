namespace NetEvolve.Schemas.XPath.Functions.Binding;

using System.Collections.Generic;
using NetEvolve.Arguments;

/// <summary>What a registration file receives: it adds the functions of its group, and nothing else.</summary>
internal sealed class FunctionCatalogBuilder
{
    private readonly List<XPathFunction> _functions = [];

    internal FunctionCatalogBuilder() { }

    /// <summary>Adds a function of the group.</summary>
    /// <param name="function">The function.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="function"/> is <see langword="null"/>.</exception>
    public void Add(XPathFunction function)
    {
        ArgumentNullException.ThrowIfNull(function);

        _functions.Add(function);
    }

    internal FunctionRegistry Build() => FunctionRegistry.Create(_functions);
}
