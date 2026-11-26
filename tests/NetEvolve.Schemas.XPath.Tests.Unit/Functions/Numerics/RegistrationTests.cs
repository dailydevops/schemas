namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Numerics;

using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Functions.Binding;

/// <summary>The boolean and numeric group stages itself into the default catalog through its registration file, with the arities of the specification.</summary>
public class RegistrationTests
{
    [Test]
    [Arguments("abs", 1, 1)]
    [Arguments("ceiling", 1, 1)]
    [Arguments("floor", 1, 1)]
    [Arguments("round", 1, 1)]
    [Arguments("round-half-to-even", 1, 2)]
    [Arguments("number", 0, 1)]
    [Arguments("true", 0, 0)]
    [Arguments("false", 0, 0)]
    [Arguments("boolean", 1, 1)]
    [Arguments("not", 1, 1)]
    public async Task DefaultCatalog_ResolvesTheFunctionForItsArityRangeOnly(string local, int minArity, int maxArity)
    {
        for (var arity = 0; arity <= 3; arity++)
        {
            var found = NumericsHarness.TryResolve(local, arity, out var function);

            await Assert.That(found).IsEqualTo(arity >= minArity && arity <= maxArity);
            if (found)
            {
                await Assert.That(function!.Name).IsEqualTo(new XPathQName(NumericsHarness.FunctionNamespace, local));
                await Assert.That(function.MinArity).IsEqualTo(minArity);
                await Assert.That(function.MaxArity).IsEqualTo(maxArity);
                await Assert.That(function.Versions).IsEqualTo(XPathVersionMask.All);
                await Assert.That(function.HostRequirements).IsEqualTo(XPathHostRequirements.None);
            }
        }
    }

    [Test]
    [Arguments("abs", 1, XPathVersion.V20)]
    [Arguments("ceiling", 1, XPathVersion.V30)]
    [Arguments("floor", 1, XPathVersion.V31)]
    [Arguments("round", 1, XPathVersion.V20)]
    [Arguments("round-half-to-even", 2, XPathVersion.V20)]
    [Arguments("round-half-to-even", 2, XPathVersion.V31)]
    [Arguments("number", 0, XPathVersion.V20)]
    [Arguments("true", 0, XPathVersion.V30)]
    [Arguments("false", 0, XPathVersion.V31)]
    [Arguments("boolean", 1, XPathVersion.V20)]
    [Arguments("not", 1, XPathVersion.V31)]
    public async Task DefaultCatalog_TheFunctionsAreVisibleInEveryLanguageVersionWithoutAHostFlag(
        string local,
        int arity,
        XPathVersion version
    )
    {
        var found = NumericsHarness.TryResolve(local, arity, out _, version);

        await Assert.That(found).IsTrue();
    }

    // fn:round with a precision is a function of Functions and Operators 3.0 and later (#727); XPath 2.0 leaves it out, and QT3
    // K-RoundFunc-2 expects XPST0017 for round(1, 2) there.
    [Test]
    [Arguments(XPathVersion.V30, true)]
    [Arguments(XPathVersion.V31, true)]
    [Arguments(XPathVersion.V20, false)]
    public async Task DefaultCatalog_RoundWithAPrecision_IsVisibleFromXPath30On(XPathVersion version, bool visible)
    {
        await Assert.That(NumericsHarness.TryResolve("round", 2, out var function, version)).IsEqualTo(visible);
        if (visible)
        {
            await Assert.That(function!.Versions).IsEqualTo(XPathVersionMask.V30 | XPathVersionMask.V31);
        }
    }

    [Test]
    [Arguments(XPathVersion.V20)]
    [Arguments(XPathVersion.V30)]
    [Arguments(XPathVersion.V31)]
    public async Task DefaultCatalog_RoundWithThreeArguments_IsNotRegistered(XPathVersion version) =>
        await Assert.That(NumericsHarness.TryResolve("round", 3, out _, version)).IsFalse();

    [Test]
    public async Task RoundWithAPrecision_InXPath20_RaisesXPST0017AtCompileTime()
    {
        var statics = XPathStaticContext.Default.ToBuilder().WithVersion(XPathVersion.V20).Build();

        await Assert.That(XPathExpression.TryCompile("round(1, 2)", statics).Diagnostic.Code).IsEqualTo("XPST0017");
    }

    [Test]
    [Arguments(XPathVersion.V30)]
    [Arguments(XPathVersion.V31)]
    public async Task RoundWithAPrecision_InXPath30And31_Compiles(XPathVersion version)
    {
        var statics = XPathStaticContext.Default.ToBuilder().WithVersion(version).Build();

        await Assert.That(XPathExpression.TryCompile("round(1.25, 1)", statics).Diagnostic.Code).IsNull();
    }

    [Test]
    public async Task DefaultCatalog_SameLocalNameInAnotherNamespace_IsNotFound()
    {
        var found = FunctionCatalog.Default.Registry.TryResolve(
            XPathStaticContext.Default,
            new XPathQName("urn:other", "abs"),
            1,
            XPathHostRequirements.None,
            out _
        );

        await Assert.That(found).IsFalse();
    }
}
