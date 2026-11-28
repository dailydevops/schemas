namespace NetEvolve.Schemas.XPath.Api;

using System;
using System.Collections.Generic;

/// <summary>A named group of host functions. A static context stacks libraries in layers; the later layer wins.</summary>
public sealed class XPathFunctionLibrary
{
    /// <summary>Initializes a new instance of the <see cref="XPathFunctionLibrary"/> class.</summary>
    /// <param name="name">The library name.</param>
    /// <param name="functions">The functions; inside a library the later entry wins, and the list is copied.</param>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    public XPathFunctionLibrary(string name, IReadOnlyList<XPathHostFunction> functions)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(functions);

        var copy = new XPathHostFunction[functions.Count];
        for (var i = 0; i < copy.Length; i++)
        {
            copy[i] = functions[i];
        }

        Name = name;
        Functions = Array.AsReadOnly(copy);
    }

    /// <summary>Gets the library name.</summary>
    public string Name { get; }

    /// <summary>Gets the functions in registration order.</summary>
    public IReadOnlyList<XPathHostFunction> Functions { get; }
}
