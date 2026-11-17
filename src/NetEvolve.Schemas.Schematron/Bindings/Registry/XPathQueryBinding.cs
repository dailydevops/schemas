namespace NetEvolve.Schemas.Schematron.Bindings.Registry;

using System;
using System.Threading;
using NetEvolve.Schemas.Schematron.Diagnostics;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>
/// The binding for the XPath 2.0 contract, shared by the <c>xpath</c> and <c>xslt</c> names (design #137 AP-25).
/// </summary>
/// <remarks>
/// Limitation: compilation and evaluation are stubbed until the XPath evaluator lands (XP-19). Until SCH-16 (#298) the
/// names <c>xslt</c>, <c>xslt1</c>, and <c>xpath</c> also use the XPath 2.0 grammar; the 1.0 compatibility differences are
/// not applied.
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
