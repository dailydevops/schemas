namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;

/// <summary>
/// The namespace bindings in scope of the element under construction: the bindings that its constructed ancestors declare. The
/// Xml tree builder derives the namespace nodes of an element from its parent, so the namespace fixup (XSLT 2.0 section 5.7.3)
/// keeps this record to know what an element already has. The scope is immutable; an element that declares a binding gets a new one.
/// </summary>
internal sealed class NamespaceScope
{
    private readonly NamespaceBinding[] _bindings;

    private NamespaceScope(NamespaceBinding[] bindings) => _bindings = bindings;

    /// <summary>Gets the scope of a node without a constructed parent.</summary>
    internal static NamespaceScope Empty { get; } = new([]);

    /// <summary>Gets the number of bindings.</summary>
    internal int Count => _bindings.Length;

    /// <summary>Gets a binding.</summary>
    /// <param name="index">The index.</param>
    /// <returns>The binding.</returns>
    internal NamespaceBinding this[int index] => _bindings[index];

    /// <summary>Looks a prefix up.</summary>
    /// <param name="prefix">The prefix; empty for the default namespace.</param>
    /// <returns>The namespace URI, or <see langword="null"/> when the prefix is not bound.</returns>
    internal string? Lookup(string prefix)
    {
        Argument.ThrowIfNull(prefix);

        foreach (var binding in _bindings)
        {
            if (string.Equals(binding.Prefix, prefix, StringComparison.Ordinal))
            {
                return binding.Uri;
            }
        }

        return null;
    }

    /// <summary>Creates the scope of an element that declares bindings: a declared prefix replaces the binding of the same prefix, an empty URI removes it.</summary>
    /// <param name="declared">The bindings the element declares.</param>
    /// <returns>The scope of the element, this scope when nothing changes.</returns>
    internal NamespaceScope With(List<NamespaceBinding> declared)
    {
        Argument.ThrowIfNull(declared);

        if (declared.Count == 0)
        {
            return this;
        }

        var bindings = new List<NamespaceBinding>(_bindings);
        foreach (var binding in declared)
        {
            var at = bindings.FindIndex(existing =>
                string.Equals(existing.Prefix, binding.Prefix, StringComparison.Ordinal)
            );
            if (at >= 0)
            {
                bindings.RemoveAt(at);
            }

            if (binding.Uri.Length > 0)
            {
                bindings.Add(binding);
            }
        }

        return new NamespaceScope(bindings.ToArray());
    }
}
