namespace NetEvolve.Schemas.XPath.Tests.Unit.Collations;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Collations;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Tests.Unit.Functions.Sequences;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.XPath.Tests.Unit.Functions.Strings.StringsHarness;

/// <summary>
/// <c>fn:compare</c> and <c>fn:codepoint-equal</c> (Functions and Operators 1.0, 7.3.1 and 7.3.2): the result and the empty sequence, the
/// collation argument through the registry (FOCH0002 for every collation that is not registered, a relative URI against the static base
/// URI, a host collation), the argument types, and the registration in the default catalog.
/// </summary>
public class CollationFunctionTests
{
    private const string Uca = "http://www.w3.org/2013/collation/UCA?lang=en;strength=primary";
    private const string Html = "http://www.w3.org/2005/xpath-functions/collation/html-ascii-case-insensitive";

    private static readonly Uri BaseUri = new("http://example.com/collations/doc.xml");

    [Test]
    [Arguments("a", "b", "integer:-1")]
    [Arguments("b", "a", "integer:1")]
    [Arguments("b", "b", "integer:0")]
    [Arguments("", "", "integer:0")]
    [Arguments("", "A Character String", "integer:-1")]
    [Arguments("A Character String", "", "integer:1")]
    [Arguments("a", "z", "integer:-1")]
    [Arguments("z", "a", "integer:1")]
    [Arguments("A", "a", "integer:-1")]
    [Arguments("12345", "1234", "integer:1")]
    public async Task Compare_TwoArguments_IsMinusOneZeroOrOne(string first, string second, string expected) =>
        await Assert.That(Shown("compare", Str(first), Str(second))).IsEqualTo(expected);

    [Test]
    [Arguments("a", "b", "integer:-1")]
    [Arguments("b", "a", "integer:1")]
    [Arguments("b", "b", "integer:0")]
    public async Task Compare_ThreeArguments_WithTheCodepointCollation_IsMinusOneZeroOrOne(
        string first,
        string second,
        string expected
    ) => await Assert.That(Shown("compare", Str(first), Str(second), Str(Codepoint))).IsEqualTo(expected);

    [Test]
    [Arguments("\U00010000", "�", "integer:1")]
    [Arguments("�", "\U00010000", "integer:-1")]
    [Arguments("\U00010001", "￰", "integer:1")]
    [Arguments("\U00010001", "\U00010002", "integer:-1")]
    public async Task Compare_SupplementaryCharacterAgainstTheBasicPlane_ComparesByCodePoint(
        string first,
        string second,
        string expected
    )
    {
        await Assert.That(Shown("compare", Str(first), Str(second))).IsEqualTo(expected);
        await Assert.That(Shown("compare", Str(first), Str(second), Str(Codepoint))).IsEqualTo(expected);
    }

    [Test]
    public async Task Compare_EmptySequenceAsAnArgument_IsTheEmptySequence()
    {
        await Assert.That(Shown("compare", XdmSequence.Empty, Str("b"))).IsEqualTo(string.Empty);
        await Assert.That(Shown("compare", Str("b"), XdmSequence.Empty)).IsEqualTo(string.Empty);
        await Assert.That(Shown("compare", XdmSequence.Empty, XdmSequence.Empty)).IsEqualTo(string.Empty);
        await Assert.That(Shown("compare", Str("a string"), XdmSequence.Empty, Str(Codepoint))).IsEqualTo(string.Empty);
    }

    [Test]
    [Arguments("http://www.example.com/COLLATION/NOT/SUPPORTED")]
    [Arguments(Uca)]
    [Arguments(Html)]
    public async Task Compare_UnregisteredAbsoluteCollation_IsFOCH0002(string collation)
    {
        await Assert.That(Shown("compare", Str("a"), Str("a"), Str(collation))).IsEqualTo("error:FOCH0002");
        await Assert.That(Shown("compare", XdmSequence.Empty, Str("a"), Str(collation))).IsEqualTo("error:FOCH0002");
    }

    [Test]
    public async Task Compare_RelativeCollationWithoutAStaticBaseUri_IsFOCH0002()
    {
        await Assert.That(Shown("compare", Str("a"), Str("a"), Str("CollationA"))).IsEqualTo("error:FOCH0002");
        await Assert.That(Shown("compare", Str("a"), Str("a"), Str("codepoint"))).IsEqualTo("error:FOCH0002");
    }

    [Test]
    public async Task Compare_RelativeCollation_IsResolvedAgainstTheStaticBaseUriBeforeTheLookup()
    {
        var registry = CollationRegistry.Default.With(new HostCollations.CaseBlind());
        var compare = new CompareFunction(registry, BaseUri);

        await Assert.That(Run(compare, Str("a"), Str("A"), Str("case-blind"))).IsEqualTo("integer:0");
        await Assert.That(Run(compare, Str("a"), Str("A"), Str("../collations/case-blind"))).IsEqualTo("integer:0");
        await Assert.That(Run(compare, Str("a"), Str("A"), Str("CollationA"))).IsEqualTo("error:FOCH0002");
        await Assert
            .That(Run(new CompareFunction(registry, null), Str("a"), Str("A"), Str("case-blind")))
            .IsEqualTo("error:FOCH0002");
    }

    [Test]
    public async Task Compare_HostRegisteredCollation_IsFoundAndUsed()
    {
        var compare = new CompareFunction(CollationRegistry.Default.With(new HostCollations.CaseBlind()), null);

        await Assert.That(Run(compare, Str("a"), Str("A"), Str(HostCollations.CaseBlindUri))).IsEqualTo("integer:0");
        await Assert.That(Run(compare, Str("a"), Str("B"), Str(HostCollations.CaseBlindUri))).IsEqualTo("integer:-1");
        await Assert.That(Run(compare, Str("b"), Str("A"), Str(HostCollations.CaseBlindUri))).IsEqualTo("integer:1");

        // The two-argument form keeps the codepoint collation, and the codepoint URI still works.
        await Assert.That(Run(compare, Str("a"), Str("A"))).IsEqualTo("integer:1");
        await Assert.That(Run(compare, Str("a"), Str("A"), Str(Codepoint))).IsEqualTo("integer:1");
    }

    [Test]
    public async Task Compare_DefaultCatalog_DoesNotKnowAHostCollation() =>
        await Assert
            .That(Shown("compare", Str("a"), Str("A"), Str(HostCollations.CaseBlindUri)))
            .IsEqualTo("error:FOCH0002");

    [Test]
    [Arguments(42, "integer:1")]
    [Arguments(-7, "integer:-1")]
    [Arguments(0, "integer:0")]
    public async Task Compare_HostCollation_ResultIsReducedToItsSign(int magnitude, string expected)
    {
        const string uri = "http://example.com/collations/magnitude";
        var compare = new CompareFunction(
            CollationRegistry.Default.With(new HostCollations.FixedMagnitude(uri, magnitude)),
            null
        );

        await Assert.That(Run(compare, Str("a"), Str("b"), Str(uri))).IsEqualTo(expected);
    }

    [Test]
    public async Task Compare_AnyUriAndUntypedAtomic_ArePromotedToStrings()
    {
        var uri = XdmSequence.Create(XdmItem.FromAnyUri("http://www.example.com/"));
        var untyped = XdmSequence.Create(XdmItem.FromUntypedAtomic("http://www.example.com/"));

        await Assert.That(Shown("compare", uri, Str("http://www.example.com/"))).IsEqualTo("integer:0");
        await Assert.That(Shown("compare", untyped, Str("http://www.example.com/"))).IsEqualTo("integer:0");
    }

    [Test]
    public async Task Compare_NumbersAreNotStrings_XPTY0004() =>
        await Assert.That(Shown("compare", Int(123), Int(456))).IsEqualTo("error:XPTY0004");

    [Test]
    [Arguments("a", "a", "boolean:true")]
    [Arguments("", "", "boolean:true")]
    [Arguments("a", "b", "boolean:false")]
    [Arguments("aa", "AA", "boolean:false")]
    [Arguments("\U00010000", "\U00010000", "boolean:true")]
    [Arguments("\U00010000", "�", "boolean:false")]
    [Arguments("garçon", "garçon", "boolean:false")]
    public async Task CodepointEqual_IsEqualityOfTheCodePoints(string first, string second, string expected) =>
        await Assert.That(Shown("codepoint-equal", Str(first), Str(second))).IsEqualTo(expected);

    [Test]
    public async Task CodepointEqual_EmptySequenceAsAnArgument_IsTheEmptySequence()
    {
        await Assert.That(Shown("codepoint-equal", XdmSequence.Empty, Str("a"))).IsEqualTo(string.Empty);
        await Assert.That(Shown("codepoint-equal", Str("a"), XdmSequence.Empty)).IsEqualTo(string.Empty);
        await Assert.That(Shown("codepoint-equal", XdmSequence.Empty, XdmSequence.Empty)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task CodepointEqual_NumbersAreNotStrings_XPTY0004()
    {
        await Assert.That(Shown("codepoint-equal", Int(1), Int(1))).IsEqualTo("error:XPTY0004");
        await Assert.That(Shown("codepoint-equal", Str("aa"), Int(1))).IsEqualTo("error:XPTY0004");
    }

    [Test]
    [Arguments("compare", 2, 3)]
    [Arguments("codepoint-equal", 2, 2)]
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

    // Calls a function that the test built over its own registry.
    private static string Run(XPathFunction function, params XdmSequence[] arguments)
    {
        try
        {
            using var context = new XPathEvaluationContext();
            return SequencesHarness.Show(function.Call(context, arguments));
        }
        catch (XPathException failure)
        {
            return "error:" + failure.Diagnostic.Code;
        }
    }
}
