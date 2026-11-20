namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.QNames;

using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Functions.Binding;

/// <summary>The QName and URI group stages itself into the default catalog through its registration file, with the arities of the specification.</summary>
public class RegistrationTests
{
    [Test]
    [Arguments("resolve-QName", 2, 2)]
    [Arguments("QName", 2, 2)]
    [Arguments("prefix-from-QName", 1, 1)]
    [Arguments("local-name-from-QName", 1, 1)]
    [Arguments("namespace-uri-from-QName", 1, 1)]
    [Arguments("namespace-uri-for-prefix", 2, 2)]
    [Arguments("in-scope-prefixes", 1, 1)]
    [Arguments("resolve-uri", 1, 2)]
    [Arguments("base-uri", 0, 1)]
    [Arguments("static-base-uri", 0, 0)]
    [Arguments("encode-for-uri", 1, 1)]
    [Arguments("iri-to-uri", 1, 1)]
    public async Task DefaultCatalog_ResolvesTheFunctionForItsArityRangeOnly(string local, int minArity, int maxArity)
    {
        for (var arity = 0; arity <= 4; arity++)
        {
            var found = QNamesHarness.TryResolve(local, arity, out var function);

            await Assert.That(found).IsEqualTo(arity >= minArity && arity <= maxArity);
            if (found)
            {
                await Assert.That(function!.Name).IsEqualTo(new XPathQName(QNamesHarness.FunctionNamespace, local));
                await Assert.That(function.MinArity).IsEqualTo(minArity);
                await Assert.That(function.MaxArity).IsEqualTo(maxArity);
                await Assert.That(function.Versions).IsEqualTo(XPathVersionMask.All);
                await Assert.That(function.HostRequirements).IsEqualTo(XPathHostRequirements.None);
            }
        }
    }

    [Test]
    [Arguments("resolve-QName", XPathVersion.V20)]
    [Arguments("resolve-QName", XPathVersion.V30)]
    [Arguments("resolve-QName", XPathVersion.V31)]
    [Arguments("encode-for-uri", XPathVersion.V20)]
    [Arguments("encode-for-uri", XPathVersion.V31)]
    [Arguments("resolve-uri", XPathVersion.V30)]
    public async Task DefaultCatalog_TheFunctionsAreVisibleInEveryLanguageVersionWithoutAHostFlag(
        string local,
        XPathVersion version
    )
    {
        var found =
            QNamesHarness.TryResolve(local, 2, out _, version) || QNamesHarness.TryResolve(local, 1, out _, version);

        await Assert.That(found).IsTrue();
    }

    [Test]
    public async Task DefaultCatalog_SameLocalNameInAnotherNamespace_IsNotFound()
    {
        var found = FunctionCatalog.Default.Registry.TryResolve(
            XPathStaticContext.Default,
            new XPathQName("urn:other", "resolve-uri"),
            1,
            XPathHostRequirements.None,
            out _
        );

        await Assert.That(found).IsFalse();
    }
}
