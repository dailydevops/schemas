namespace NetEvolve.Schemas.Schematron.Bindings.Xslt1;

using System;
using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Bindings.Registry;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Context;

/// <summary>Skeleton of the binding of the default names; the compatibility mode is wired in the next commit.</summary>
internal sealed class XPath10QueryBinding : IQueryBinding
{
    private readonly XPathQueryBinding _inner;

    internal XPath10QueryBinding(string name, bool allowsXsltInstructions)
    {
        Argument.ThrowIfNull(name);

        _inner = new XPathQueryBinding(name, allowsXsltInstructions);
    }

    public string Name => _inner.Name;

    public bool AllowsXsltInstructions => _inner.AllowsXsltInstructions;

    internal static XPathStaticContext CreateStaticContext(QueryStaticScope scope)
    {
        Argument.ThrowIfNull(scope);

        return XPathStaticContext.Default;
    }

    public bool TryCompile(
        ReadOnlySpan<char> text,
        QueryExpressionKind kind,
        QueryStaticScope scope,
        out QueryExpression expression,
        out SchematronProblem problem
    ) => _inner.TryCompile(text, kind, scope, out expression, out problem);

    public IQueryRun BeginRun(QueryResources resources, CancellationToken cancellationToken) =>
        _inner.BeginRun(resources, cancellationToken);
}
