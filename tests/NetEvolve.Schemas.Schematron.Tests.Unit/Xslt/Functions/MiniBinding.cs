namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions;

using System;
using System.Collections.Generic;
using System.Threading;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>
/// A query binding over <see cref="MiniQuery"/>: it resolves each <c>$name</c> against the visible names of the compile scope the
/// way a real binding does (the last non-empty entry of a name wins), records the compile scope of every expression, and reports the
/// slots an expression reads.
/// </summary>
internal sealed class MiniBinding : IQueryBinding
{
    internal List<(string Text, QueryStaticScope Scope)> Compiled { get; } = [];

    internal MiniRun? LastRun { get; private set; }

    public string Name => "mini";

    public bool AllowsXsltInstructions => true;

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
        if (
            MiniQuery.TryParse(
                source,
                name => Resolve(scope, name),
                prefix => Prefix(scope, prefix),
                out var node,
                out var error
            )
        )
        {
            var slots = new List<int>();
            node.Collect(slots);
            expression = new MiniExpression(source, node, scope.DeclaredType, [.. slots]);
            problem = default;
            return true;
        }

        problem = error.StartsWith("VAR:", StringComparison.Ordinal)
            ? new SchematronProblem(
                SchematronDiagnosticCodes.VariableUndefined,
                $"The variable ${error.Substring(4)} is not in scope.",
                default,
                "XPST0008"
            )
            : new SchematronProblem(
                SchematronDiagnosticCodes.ExpressionInvalid,
                error,
                default,
                error.StartsWith("PREFIX:", StringComparison.Ordinal) ? "XPST0081" : "XPST0003"
            );
        return false;
    }

    public IQueryRun BeginRun(QueryResources resources, CancellationToken cancellationToken)
    {
        LastRun = new MiniRun();
        return LastRun;
    }

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

    private static string? Prefix(QueryStaticScope scope, string prefix)
    {
        for (var i = scope.Namespaces.Count - 1; i >= 0; i--)
        {
            if (string.Equals(scope.Namespaces[i].Prefix, prefix, StringComparison.Ordinal))
            {
                return scope.Namespaces[i].NamespaceName;
            }
        }

        return prefix switch
        {
            "f" => FunctionTree.Functions,
            "xs" => FunctionTree.XmlSchema,
            _ => null,
        };
    }
}
