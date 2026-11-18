namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Nodes;

using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Functions.Binding;

/// <summary>The node and context group stages itself into the default catalog through its registration file, with the arities of the specification.</summary>
public class RegistrationTests
{
    [Test]
    [Arguments("name", 0, 1)]
    [Arguments("local-name", 0, 1)]
    [Arguments("namespace-uri", 0, 1)]
    [Arguments("root", 0, 1)]
    [Arguments("lang", 1, 2)]
    [Arguments("position", 0, 0)]
    [Arguments("last", 0, 0)]
    [Arguments("string", 0, 1)]
    [Arguments("data", 1, 1)]
    public async Task DefaultCatalog_ResolvesTheFunctionForItsArityRangeOnly(string local, int minArity, int maxArity)
    {
        for (var arity = 0; arity <= 3; arity++)
        {
            var found = NodesHarness.TryResolve(local, arity, out var function);

            await Assert.That(found).IsEqualTo(arity >= minArity && arity <= maxArity);
            if (found)
            {
                await Assert.That(function!.Name).IsEqualTo(new XPathQName(NodesHarness.FunctionNamespace, local));
                await Assert.That(function.MinArity).IsEqualTo(minArity);
                await Assert.That(function.MaxArity).IsEqualTo(maxArity);
                await Assert.That(function.Versions).IsEqualTo(XPathVersionMask.All);
                await Assert.That(function.HostRequirements).IsEqualTo(XPathHostRequirements.None);
            }
        }
    }

    [Test]
    [Arguments("name", XPathVersion.V20)]
    [Arguments("name", XPathVersion.V30)]
    [Arguments("name", XPathVersion.V31)]
    [Arguments("lang", XPathVersion.V20)]
    [Arguments("lang", XPathVersion.V31)]
    [Arguments("string", XPathVersion.V20)]
    [Arguments("data", XPathVersion.V30)]
    [Arguments("position", XPathVersion.V20)]
    public async Task DefaultCatalog_TheFunctionsAreVisibleInEveryLanguageVersionWithoutAHostFlag(
        string local,
        XPathVersion version
    )
    {
        var found =
            NodesHarness.TryResolve(local, 1, out _, version) || NodesHarness.TryResolve(local, 0, out _, version);

        await Assert.That(found).IsTrue();
    }

    [Test]
    public async Task DefaultCatalog_SameLocalNameInAnotherNamespace_IsNotFound()
    {
        var found = FunctionCatalog.Default.Registry.TryResolve(
            XPathStaticContext.Default,
            new XPathQName("urn:other", "name"),
            1,
            XPathHostRequirements.None,
            out _
        );

        await Assert.That(found).IsFalse();
    }
}
