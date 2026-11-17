namespace NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;

using System;
using System.Collections.Generic;
using System.Threading;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

internal sealed class FakeQueryBinding : IQueryBinding
{
    internal int CompileCalls { get; private set; }

    internal FakeQueryRun? LastRun { get; private set; }

    public string Name => "fake";

    public bool AllowsXsltInstructions => false;

    public bool TryCompile(
        ReadOnlySpan<char> text,
        QueryExpressionKind kind,
        QueryStaticScope scope,
        out QueryExpression expression,
        out SchematronProblem problem
    )
    {
        CompileCalls++;
        expression = new FakeQueryExpression(text.ToString());
        problem = default;
        if (text.IsEmpty)
        {
            problem = new SchematronProblem(
                SchematronDiagnosticCodes.ExpressionInvalid,
                "empty expression",
                FakeModel.Origin,
                "XPST0003"
            );
            return false;
        }

        return true;
    }

    public IQueryRun BeginRun(QueryResources resources, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastRun = new FakeQueryRun();
        return LastRun;
    }
}
