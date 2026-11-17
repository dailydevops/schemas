namespace NetEvolve.Schemas.Schematron.Seams;

using System;
using System.Threading;

/// <summary>A query binding: compiles expressions and starts runs.</summary>
internal interface IQueryBinding
{
    /// <summary>Gets the canonical lower-case name.</summary>
    string Name { get; }

    /// <summary>Gets a value indicating whether XSLT instructions are allowed; <see langword="false"/> for the <c>xpath</c> family.</summary>
    bool AllowsXsltInstructions { get; }

    /// <summary>Compiles an expression. Cold path; never throws for input problems.</summary>
    bool TryCompile(
        ReadOnlySpan<char> text,
        QueryExpressionKind kind,
        QueryStaticScope scope,
        out QueryExpression expression,
        out SchematronProblem problem
    );

    /// <summary>Begins a run: one per validation call, not thread-safe.</summary>
    IQueryRun BeginRun(QueryResources resources, CancellationToken cancellationToken);
}
