namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Delta3;

using System;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:environment-variable</c> and <c>fn:available-environment-variables</c> (Functions and Operators 3.1, 16.4 and 16.5): the
/// environment of the process is never exposed, which the specification allows, so the first gives the empty sequence for every name and
/// the second gives the empty sequence.
/// </summary>
public class EnvironmentFunctionTests
{
    [Test]
    [Arguments("environment-variable", 1)]
    [Arguments("available-environment-variables", 0)]
    public async Task Registry_ResolvesInXPath30AndLater_ButNotInXPath20(
        string local,
        int arity,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Delta3Harness.TryResolve(local, arity, out _, XPathVersion.V20)).IsFalse();
        await Assert.That(Delta3Harness.TryResolve(local, arity, out _, XPathVersion.V30)).IsTrue();
        await Assert.That(Delta3Harness.TryResolve(local, arity, out _, XPathVersion.V31)).IsTrue();
    }

    [Test]
    [Arguments("fn:environment-variable('PATH')")]
    [Arguments("fn:available-environment-variables()")]
    public async Task TryCompile_InXPath20_ReturnsXpst0017(string query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var statics = XPathStaticContext
            .Default.ToBuilder()
            .WithVersion(XPathVersion.V20)
            .WithNamespace("fn", "http://www.w3.org/2005/xpath-functions")
            .Build();

        await Assert.That(XPathExpression.TryCompile(query, statics).Diagnostic.Code).IsEqualTo("XPST0017");
    }

    [Test]
    public async Task EnvironmentVariable_ForAVariableThatIsSetInTheProcess_ReturnsTheEmptySequence(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        const string Name = "NETEVOLVE_SCHEMAS_XPATH_TEST_VARIABLE";
        var previous = Environment.GetEnvironmentVariable(Name);
        Environment.SetEnvironmentVariable(Name, "value");
        try
        {
            await Assert.That(Delta3Harness.Call("environment-variable", Delta3Harness.Str(Name)).Count).IsEqualTo(0);
            await Assert.That(Delta3Harness.Call("environment-variable", Delta3Harness.Str("PATH")).Count).IsEqualTo(0);
        }
        finally
        {
            Environment.SetEnvironmentVariable(Name, previous);
        }
    }

    [Test]
    public async Task EnvironmentVariable_ForAnEmptyArgument_RaisesXPTY0004(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Delta3Harness.ErrorOf("environment-variable", XdmSequence.Empty)).IsEqualTo("XPTY0004");
    }

    [Test]
    public async Task AvailableEnvironmentVariables_ReturnsTheEmptySequence(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Delta3Harness.Call("available-environment-variables").Count).IsEqualTo(0);
    }
}
