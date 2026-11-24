namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Sequences;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Functions.Sequences;

/// <summary>The sequence and aggregate group stages itself into the default catalog through its registration file, with the arities of the specification.</summary>
public class RegistrationTests
{
    [Test]
    [Arguments("empty", 1, 1)]
    [Arguments("exists", 1, 1)]
    [Arguments("count", 1, 1)]
    [Arguments("sum", 1, 2)]
    [Arguments("avg", 1, 1)]
    [Arguments("min", 1, 2)]
    [Arguments("max", 1, 2)]
    [Arguments("distinct-values", 1, 2)]
    [Arguments("index-of", 2, 3)]
    [Arguments("deep-equal", 2, 3)]
    [Arguments("subsequence", 2, 3)]
    [Arguments("insert-before", 3, 3)]
    [Arguments("remove", 2, 2)]
    [Arguments("reverse", 1, 1)]
    [Arguments("unordered", 1, 1)]
    [Arguments("zero-or-one", 1, 1)]
    [Arguments("one-or-more", 1, 1)]
    [Arguments("exactly-one", 1, 1)]
    [Arguments("error", 0, 3)]
    [Arguments("trace", 2, 2)]
    public async Task DefaultCatalog_ResolvesTheFunctionForItsArityRangeOnly(string local, int minArity, int maxArity)
    {
        for (var arity = 0; arity <= 4; arity++)
        {
            var found = SequencesHarness.TryResolve(local, arity, out var function);

            await Assert.That(found).IsEqualTo(arity >= minArity && arity <= maxArity);
            if (found)
            {
                await Assert.That(function!.Name).IsEqualTo(new XPathQName(SequencesHarness.FunctionNamespace, local));
                await Assert.That(function.MinArity).IsEqualTo(minArity);
                await Assert.That(function.MaxArity).IsEqualTo(maxArity);
                await Assert.That(function.Versions).IsEqualTo(XPathVersionMask.All);
                await Assert.That(function.HostRequirements).IsEqualTo(XPathHostRequirements.None);
            }
        }
    }

    [Test]
    [Arguments(XPathVersion.V20)]
    [Arguments(XPathVersion.V30)]
    [Arguments(XPathVersion.V31)]
    public async Task DefaultCatalog_TheFunctionsAreVisibleInEveryLanguageVersion(XPathVersion version)
    {
        var statics = XPathStaticContext.Default.ToBuilder().WithVersion(version).Build();

        var found = FunctionCatalog.Default.Registry.TryResolve(
            statics,
            new XPathQName(SequencesHarness.FunctionNamespace, "count"),
            1,
            XPathHostRequirements.None,
            out _
        );

        await Assert.That(found).IsTrue();
    }

    [Test]
    public async Task DefaultCatalog_SameLocalNameInAnotherNamespace_IsNotFound()
    {
        var found = FunctionCatalog.Default.Registry.TryResolve(
            XPathStaticContext.Default,
            new XPathQName("urn:other", "count"),
            1,
            XPathHostRequirements.None,
            out _
        );

        await Assert.That(found).IsFalse();
    }

    [Test]
    public async Task Register_NullBuilder_Throws() =>
        await Assert.That(() => Registration.Register(null!)).Throws<ArgumentNullException>();

    [Test]
    public async Task Register_AddsTheGroupToABuilder_AndAddingItTwiceIsAConflict()
    {
        var builder = new FunctionCatalogBuilder();
        Registration.Register(builder);
        _ = builder.Build();

        Registration.Register(builder);

        await Assert.That(() => builder.Build()).Throws<InvalidOperationException>();
    }
}
