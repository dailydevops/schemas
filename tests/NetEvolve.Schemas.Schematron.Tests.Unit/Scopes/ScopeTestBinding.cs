namespace NetEvolve.Schemas.Schematron.Tests.Unit.Scopes;

using System;
using System.Collections.Generic;
using System.Threading;
using NetEvolve.Schemas.Schematron;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;

/// <summary>
/// A fake binding that resolves each <c>$name</c> against the visible variable names of the compile scope the way a real
/// binding does (the last entry of a name wins, an empty entry is not visible) and reports the slots an expression reads.
/// </summary>
internal sealed class ScopeTestBinding : IQueryBinding
{
    internal List<(string Text, QueryStaticScope Scope)> Compiled { get; } = [];

    internal int RunsBegun { get; private set; }

    internal ScopeTestRun? LastRun { get; private set; }

    public string Name => "scope-test";

    public bool AllowsXsltInstructions => false;

    public bool TryCompile(
        ReadOnlySpan<char> text,
        QueryExpressionKind kind,
        QueryStaticScope scope,
        out QueryExpression expression,
        out SchematronProblem problem
    )
    {
        var source = text.ToString();
        Compiled.Add((source, scope));
        expression = null!;
        problem = default;
        var slots = new List<int>();
        var at = source.IndexOf('$', StringComparison.Ordinal);
        while (at >= 0)
        {
            var end = at + 1;
            while (end < source.Length && (char.IsLetterOrDigit(source[end]) || source[end] is '_' or '-'))
            {
                end++;
            }

            var name = source.Substring(at + 1, end - at - 1);
            var slot = Resolve(scope, name);
            if (slot < 0)
            {
                problem = new SchematronProblem(
                    SchematronDiagnosticCodes.VariableUndefined,
                    $"The variable '${name}' is not in scope.",
                    FakeModel.Origin,
                    "XPST0008"
                );
                return false;
            }

            slots.Add(slot);
            at = source.IndexOf('$', end);
        }

        expression = new ScopeTestExpression(source, scope.DeclaredType, [.. slots]);
        return true;
    }

    public IQueryRun BeginRun(QueryResources resources, CancellationToken cancellationToken)
    {
        RunsBegun++;
        LastRun = new ScopeTestRun();
        return LastRun;
    }

    /// <summary>Resolves a name the way a binding does: the last non-empty entry of the name wins.</summary>
    internal static int Resolve(QueryStaticScope scope, string name)
    {
        for (var slot = scope.VariableNames.Count - 1; slot >= 0; slot--)
        {
            if (string.Equals(scope.VariableNames[slot], name, StringComparison.Ordinal))
            {
                return slot;
            }
        }

        return -1;
    }
}
