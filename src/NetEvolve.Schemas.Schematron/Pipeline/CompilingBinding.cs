namespace NetEvolve.Schemas.Schematron.Pipeline;

using System;
using System.Collections.Generic;
using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>
/// The binding of a compiled schema as the build sees it: it forwards to the binding of the registry and records what the build
/// compiles (SCH-71, #340), so a test can show that every expression is compiled once at build and never in a validation, and the
/// build can read the element names of the expressions to intern them in the name table of the validation contexts.
/// </summary>
/// <remarks>
/// Not thread-safe: it is used by one build on one thread. After the build, a run only calls <see cref="BeginRun"/>, which keeps no state
/// here, so the compiled schema stays free of shared mutable state.
/// </remarks>
internal sealed class CompilingBinding : IQueryBinding
{
    // The names of the expressions are a hint to pre-intern; the cap keeps a hostile schema from filling the table.
    private const int MaxNames = 4096;

    private readonly IQueryBinding _inner;
    private readonly List<string> _texts = [];
    private readonly HashSet<string> _names = new(StringComparer.Ordinal);
    private readonly List<string> _orderedNames = [];

    internal CompilingBinding(IQueryBinding inner)
    {
        ArgumentNullException.ThrowIfNull(inner);

        _inner = inner;
    }

    public string Name => _inner.Name;

    public bool AllowsXsltInstructions => _inner.AllowsXsltInstructions;

    /// <summary>Gets the text of every expression that was compiled, in the order of compilation.</summary>
    internal IReadOnlyList<string> Texts => _texts;

    /// <summary>Gets the names (local names and namespace URIs) that the expressions mention, in the order of first use.</summary>
    internal IReadOnlyList<string> Names => _orderedNames;

    public bool TryCompile(
        ReadOnlySpan<char> text,
        QueryExpressionKind kind,
        QueryStaticScope scope,
        out QueryExpression expression,
        out SchematronProblem problem
    )
    {
        Record(text, scope);
        return _inner.TryCompile(text, kind, scope, out expression, out problem);
    }

    /// <summary>Records an expression that is compiled by the build.</summary>
    /// <param name="text">The text of the expression.</param>
    /// <param name="scope">The scope that the expression compiles in.</param>
    internal void Record(ReadOnlySpan<char> text, QueryStaticScope scope)
    {
        _texts.Add(text.ToString());
        Collect(text, scope);
    }

    public IQueryRun BeginRun(QueryResources resources, CancellationToken cancellationToken) =>
        _inner.BeginRun(resources, cancellationToken);

    // ponytail: a lexical scan, not the parse tree of the expression; it also picks up function and axis names, which only
    // costs a few extra names in the table. Upgrade to the name tests of the compiled expression when the XPath package exposes them.
    private void Collect(ReadOnlySpan<char> text, QueryStaticScope scope)
    {
        var i = 0;
        while (i < text.Length)
        {
            if (!IsStart(text[i]))
            {
                i++;
                continue;
            }

            var start = i;
            while (i < text.Length && IsPart(text[i]))
            {
                i++;
            }

            var first = text[start..i];
            if (i + 1 < text.Length && text[i] == ':' && IsStart(text[i + 1]))
            {
                var localStart = ++i;
                while (i < text.Length && IsPart(text[i]))
                {
                    i++;
                }

                Add(text[localStart..i]);
                foreach (var declaration in scope.Namespaces)
                {
                    if (first.SequenceEqual(declaration.Prefix.AsSpan()))
                    {
                        Add(declaration.NamespaceName.AsSpan());
                    }
                }

                continue;
            }

            Add(first);
        }
    }

    private void Add(ReadOnlySpan<char> name)
    {
        if (name.IsEmpty || _orderedNames.Count >= MaxNames)
        {
            return;
        }

        var text = name.ToString();
        if (_names.Add(text))
        {
            _orderedNames.Add(text);
        }
    }

    private static bool IsStart(char c) => char.IsLetter(c) || c == '_';

    private static bool IsPart(char c) => char.IsLetterOrDigit(c) || c is '_' or '-' or '.';
}
