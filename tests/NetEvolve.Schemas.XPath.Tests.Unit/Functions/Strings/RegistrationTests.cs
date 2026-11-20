namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Strings;

using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Tests.Unit.Functions.Sequences;

/// <summary>The string group stages itself into the default catalog through its registration file, with the arities of the specification.</summary>
public class RegistrationTests
{
    [Test]
    [Arguments("concat", 2, int.MaxValue)]
    [Arguments("string-join", 2, 2)]
    [Arguments("substring", 2, 3)]
    [Arguments("string-length", 0, 1)]
    [Arguments("normalize-space", 0, 1)]
    [Arguments("normalize-unicode", 1, 2)]
    [Arguments("translate", 3, 3)]
    [Arguments("contains", 2, 3)]
    [Arguments("starts-with", 2, 3)]
    [Arguments("ends-with", 2, 3)]
    [Arguments("substring-before", 2, 3)]
    [Arguments("substring-after", 2, 3)]
    [Arguments("upper-case", 1, 1)]
    [Arguments("lower-case", 1, 1)]
    [Arguments("codepoints-to-string", 1, 1)]
    [Arguments("string-to-codepoints", 1, 1)]
    public async Task DefaultCatalog_ResolvesTheFunctionForItsArityRangeOnly(string local, int minArity, int maxArity)
    {
        for (var arity = 0; arity <= 5; arity++)
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
    public async Task Concat_IsVariadic_SoAHundredArgumentsResolve()
    {
        var found = SequencesHarness.TryResolve("concat", 100, out var function);

        await Assert.That(found).IsTrue();
        await Assert.That(function!.MaxArity).IsEqualTo(int.MaxValue);
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
            new XPathQName(SequencesHarness.FunctionNamespace, "upper-case"),
            1,
            XPathHostRequirements.None,
            out _
        );

        await Assert.That(found).IsTrue();
    }

    [Test]
    public async Task DefaultCatalog_DoesNotRegisterTheFunctionsOfOtherWorkPackages()
    {
        // matches, replace, and tokenize: XP-24; escape-html-uri: DEF-84. (encode-for-uri and iri-to-uri are registered by XP-28, in Functions/QNames.) (compare and codepoint-equal: XP-25 registers them in Collations.)
        string[] names = ["matches", "replace", "tokenize", "escape-html-uri"];
        foreach (var local in names)
        {
            for (var arity = 0; arity <= 4; arity++)
            {
                await Assert
                    .That(SequencesHarness.TryResolve(local, arity, out _))
                    .IsFalse()
                    .Because($"{local}#{arity}");
            }
        }
    }
}
