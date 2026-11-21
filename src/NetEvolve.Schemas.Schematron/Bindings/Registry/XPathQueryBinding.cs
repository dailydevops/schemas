namespace NetEvolve.Schemas.Schematron.Bindings.Registry;

using System;
using System.Threading;
using NetEvolve.Schemas.Schematron.Diagnostics;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>
/// The binding for the XPath 2.0 contract, shared by the names <c>xpath2</c>, <c>xslt2</c>, and <c>xslt3</c> (design #137 AP-25).
/// </summary>
/// <remarks>
/// Limitation: compilation and evaluation are stubbed until the XPath evaluator lands (XP-19). The names <c>xslt</c>,
/// <c>xslt1</c>, and <c>xpath</c> use the binding of <c>Bindings/Xslt1</c> (SCH-16), which turns XPath 1.0 compatibility mode on.
/// </remarks>
internal sealed class XPathQueryBinding : IQueryBinding
{
    internal XPathQueryBinding(string name, bool allowsXsltInstructions)
    {
        Name = name;
        AllowsXsltInstructions = allowsXsltInstructions;
    }

    public string Name { get; }

    public bool AllowsXsltInstructions { get; }

    public bool TryCompile(
        ReadOnlySpan<char> text,
        QueryExpressionKind kind,
        QueryStaticScope scope,
        out QueryExpression expression,
        out SchematronProblem problem
    )
    {
        expression = null!;
        problem = new SchematronProblem(
            SchematronDiagnosticCodes.NotImplemented,
            $"The Schematron stage 'query evaluation' of the '{Name}' binding is not implemented yet.",
            new DocumentLocation(null, 0, 0)
        );
        return false;
    }

    public IQueryRun BeginRun(QueryResources resources, CancellationToken cancellationToken) =>
        throw NotImplementedFailure.For("query evaluation");
}
