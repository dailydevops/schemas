namespace NetEvolve.Schemas.Schematron.Bindings.Registry;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>
/// Finds the variable slots an expression reads. The XPath package does not report the variables of a compiled expression, so the
/// syntax tree is walked: a reference is a slot when its name is not bound by an enclosing <see langword="for"/>, <c>some</c>, or
/// <c>every</c>, and is visible in the scope, where the last entry of a name wins (an inner declaration shadows an outer one).
/// </summary>
internal sealed class SlotReferences
{
    private readonly QueryStaticScope _scope;
    private readonly List<string> _ranges = [];
    private readonly List<int> _slots = [];

    private SlotReferences(QueryStaticScope scope) => _scope = scope;

    /// <summary>Collects the slots the expression reads, in order of first use.</summary>
    /// <param name="text">The expression, which has already compiled.</param>
    /// <param name="scope">The scope it compiled in.</param>
    /// <param name="slots">The slots.</param>
    /// <param name="names">The name of each slot, parallel to <paramref name="slots"/>.</param>
    internal static void Collect(
        ReadOnlySpan<char> text,
        QueryStaticScope scope,
        out int[] slots,
        out XPathQName[] names
    )
    {
        ArgumentNullException.ThrowIfNull(scope);

        slots = [];
        names = [];
        if (!AnyVisible(scope))
        {
            return;
        }

        // The expression has compiled, so it parses.
        var parsed = XPathParser.TryParse(text, out var tree, out _);
        Debug.Assert(parsed, "The expression compiled, so it parses.");

        var run = new SlotReferences(scope);
        run.Visit(tree!);
        slots = [.. run._slots];
        names = new XPathQName[slots.Length];
        for (var i = 0; i < slots.Length; i++)
        {
            names[i] = VariableKey.ToQName(scope.VariableNames[slots[i]]);
        }
    }

    private static bool AnyVisible(QueryStaticScope scope)
    {
        for (var i = 0; i < scope.VariableNames.Count; i++)
        {
            if (scope.VariableNames[i].Length > 0)
            {
                return true;
            }
        }

        return false;
    }

    private void Visit(SyntaxNode node)
    {
        switch (node)
        {
            case VariableReference reference:
                Reference(reference.Name);
                break;
            case ForExpression loop:
                Bind(loop.Bindings, loop.Return);
                break;
            case QuantifiedExpression quantified:
                Bind(quantified.Bindings, quantified.Satisfies);
                break;
            default:
                foreach (var child in node.Children)
                {
                    Visit(child);
                }

                break;
        }
    }

    // The source of a binding sees the variables bound before it; the body sees all of them.
    private void Bind(IReadOnlyList<VariableBinding> bindings, Expression body)
    {
        var mark = _ranges.Count;
        foreach (var binding in bindings)
        {
            Visit(binding.Source);
            _ranges.Add(binding.Name.LocalName);
        }

        Visit(body);
        _ranges.RemoveRange(mark, _ranges.Count - mark);
    }

    private void Reference(QualifiedName name)
    {
        // A prefixed name is a slot when the prefix resolves to the namespace of a prefixed local variable; the ranges of for,
        // some, and every are unprefixed.
        if (name.Prefix is null && _ranges.Contains(name.LocalName))
        {
            return;
        }

        var key = name.Prefix is null ? name.LocalName : VariableKey.Of(NamespaceOf(name.Prefix), name.LocalName);

        var names = _scope.VariableNames;
        for (var slot = names.Count - 1; slot >= 0; slot--)
        {
            if (string.Equals(names[slot], key, StringComparison.Ordinal))
            {
                if (!_slots.Contains(slot))
                {
                    _slots.Add(slot);
                }

                return;
            }
        }
    }

    // The last declaration of a prefix wins, as in the static context of the expression; an undeclared prefix gives a key no slot has.
    private string NamespaceOf(string prefix)
    {
        var namespaces = _scope.Namespaces;
        for (var i = namespaces.Count - 1; i >= 0; i--)
        {
            if (string.Equals(namespaces[i].Prefix, prefix, StringComparison.Ordinal))
            {
                return namespaces[i].NamespaceName;
            }
        }

        return prefix + ":";
    }
}
