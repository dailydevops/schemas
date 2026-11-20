namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Keys;

using System;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Patterns;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Keys;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Keys.KeyFixture;

/// <summary>
/// The lazy per-document index of the keys (XSLT 2.0 section 16.3): built on the first lookup and once per document, the result of
/// one key in document order without duplicates, the union of several declarations of one name, the two-argument and three-argument
/// forms, and the value comparison.
/// </summary>
public class KeyIndexTests
{
    private static string Lookup(
        XsltKeyProvider provider,
        KeyDocument document,
        string key,
        XdmSequence values,
        string from = "doc",
        bool subtree = false
    )
    {
        var status = provider.TryLookup(Key(key), values, document[from], subtree, out var nodes);
        return status == QueryStatus.Ok ? document.Keys(Nodes(nodes)) : "problem:" + provider.GetProblem().Constraint;
    }

    [Test]
    [Arguments("a", "i1 o1")]
    [Arguments("A", "i3")]
    [Arguments("b", "i2")]
    [Arguments("x", "s1")]
    [Arguments("none", "")]
    public async Task Lookup_UseAttribute_ReturnsTheMatchingNodesInDocumentOrder(string value, string expected)
    {
        var fixture = new KeyFixture();
        var document = new KeyDocument();
        var (_, provider) = fixture.CreateProvider(Kx.Key("k", "*", "@code", 10));

        await Assert.That(Lookup(provider, document, "k", Str(value))).IsEqualTo(expected);
    }

    [Test]
    public async Task Lookup_MatchWithAPredicate_IndexesOnlyTheNodesThatSatisfyIt()
    {
        var fixture = new KeyFixture();
        var document = new KeyDocument();
        var (_, provider) = fixture.CreateProvider(Kx.Key("k", "item[@group = 'g1']", "@code", 10));

        await Assert.That(Lookup(provider, document, "k", Str("a"))).IsEqualTo("i1");
        await Assert.That(Lookup(provider, document, "k", Str("A"))).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Lookup_ContentForm_UsesTheTextAsTheKeyValue()
    {
        var fixture = new KeyFixture();
        var document = new KeyDocument();
        var (_, provider) = fixture.CreateProvider(Kx.Key("k", "item", null, 10, Kx.Text("key", 11)));

        await Assert.That(Lookup(provider, document, "k", Str("key"))).IsEqualTo("i1 i2 i3");
    }

    [Test]
    public async Task Lookup_UseThatReturnsSeveralValues_IndexesTheNodeUnderEach()
    {
        var fixture = new KeyFixture();
        var document = new KeyDocument();
        var (_, provider) = fixture.CreateProvider(Kx.Key("k", "item", "@code, @group", 10));

        await Assert.That(Lookup(provider, document, "k", Str("g1"))).IsEqualTo("i1 i2");
        await Assert.That(Lookup(provider, document, "k", Str("b"))).IsEqualTo("i2");
    }

    [Test]
    public async Task Lookup_UseThatReturnsTheSameValueTwice_ListsTheNodeOnce()
    {
        var fixture = new KeyFixture();
        var document = new KeyDocument();
        var (_, provider) = fixture.CreateProvider(Kx.Key("k", "item", "@group, @group", 10));

        await Assert.That(Lookup(provider, document, "k", Str("g1"))).IsEqualTo("i1 i2");
    }

    [Test]
    public async Task Lookup_UseThatReturnsTheElement_AtomizesToItsStringValue()
    {
        var fixture = new KeyFixture();
        var document = new KeyDocument();
        var (_, provider) = fixture.CreateProvider(Kx.Key("k", "other", ".", 10));

        await Assert.That(Lookup(provider, document, "k", Str("txt"))).IsEqualTo("o1");
    }

    [Test]
    public async Task Lookup_UseThatReturnsNothing_IndexesNoNode()
    {
        var fixture = new KeyFixture();
        var document = new KeyDocument();
        var (_, provider) = fixture.CreateProvider(Kx.Key("k", "item", "()", 10));

        await Assert.That(Lookup(provider, document, "k", Str("a"))).IsEqualTo(string.Empty);
        await Assert.That(provider.IndexBuilds).IsEqualTo(1);
    }

    [Test]
    [Arguments("/", "root", "doc")]
    [Arguments("catalog", "root", "catalog")]
    [Arguments("@code", "a", "i1@code o1@code")]
    [Arguments("text()", "txt", "o1#text")]
    [Arguments("comment()", "c", "comment")]
    [Arguments("processing-instruction()", "p", "pi")]
    public async Task Lookup_Match_CoversEveryKindOfNode(string match, string value, string expected)
    {
        var fixture = new KeyFixture();
        var document = new KeyDocument();
        var use = match is "/" or "comment()" or "processing-instruction()" or "catalog" ? $"'{value}'" : ".";
        var (_, provider) = fixture.CreateProvider(Kx.Key("k", match, use, 10));

        await Assert.That(Lookup(provider, document, "k", Str(value))).IsEqualTo(expected);
    }

    [Test]
    public async Task Index_IsBuiltOnTheFirstLookup_AndNotAgainForTheNextLookupsOfTheDocument()
    {
        var fixture = new KeyFixture();
        var document = new KeyDocument();
        var (_, provider) = fixture.CreateProvider(Kx.Key("k", "item", "@code", 10));

        await Assert.That(provider.IndexBuilds).IsEqualTo(0);
        await Assert.That(fixture.Binding.Evaluations).IsEqualTo(0);

        _ = Lookup(provider, document, "k", Str("a"));
        await Assert.That(provider.IndexBuilds).IsEqualTo(1);

        // One evaluation of the key value for each of the three matching items, and none for a later lookup.
        await Assert.That(fixture.Binding.Evaluations).IsEqualTo(3);
        _ = Lookup(provider, document, "k", Str("b"));
        _ = Lookup(provider, document, "k", Str("zz"), from: "s1");
        _ = Lookup(provider, document, "k", Str("a"), subtree: true);
        await Assert.That(provider.IndexBuilds).IsEqualTo(1);
        await Assert.That(fixture.Binding.Evaluations).IsEqualTo(3);
    }

    [Test]
    public async Task Index_IsBuiltOncePerDocument()
    {
        var fixture = new KeyFixture();
        var first = new KeyDocument();
        var second = new KeyDocument();
        var (_, provider) = fixture.CreateProvider(Kx.Key("k", "item", "@code", 10));

        _ = Lookup(provider, first, "k", Str("a"));
        await Assert.That(provider.IndexBuilds).IsEqualTo(1);

        // The second document is a second tree, so it gets an index of its own, with its own nodes.
        await Assert.That(Lookup(provider, second, "k", Str("a"))).IsEqualTo("i1");
        await Assert.That(provider.IndexBuilds).IsEqualTo(2);
        _ = Lookup(provider, first, "k", Str("b"));
        _ = Lookup(provider, second, "k", Str("b"));
        await Assert.That(provider.IndexBuilds).IsEqualTo(2);
    }

    [Test]
    public async Task Index_IsBuiltOncePerKeyName()
    {
        var fixture = new KeyFixture();
        var document = new KeyDocument();
        var (_, provider) = fixture.CreateProvider(
            Kx.Key("k", "item", "@code", 10),
            Kx.Key("other", "item", "@group", 20)
        );

        _ = Lookup(provider, document, "k", Str("a"));
        await Assert.That(provider.IndexBuilds).IsEqualTo(1);
        _ = Lookup(provider, document, "other", Str("g1"));
        await Assert.That(provider.IndexBuilds).IsEqualTo(2);
    }

    [Test]
    public async Task Lookup_TwoDeclarationsOfOneName_ReturnTheUnionOfTheirMatches()
    {
        var fixture = new KeyFixture();
        var document = new KeyDocument();
        var (_, provider) = fixture.CreateProvider(Kx.Key("k", "item", "@code", 10), Kx.Key("k", "other", "@code", 20));

        await Assert.That(Lookup(provider, document, "k", Str("a"))).IsEqualTo("i1 o1");
    }

    [Test]
    public async Task Lookup_TwoDeclarations_ReturnTheNodesInDocumentOrderNotDeclarationOrder()
    {
        var fixture = new KeyFixture();
        var document = new KeyDocument();
        var (_, provider) = fixture.CreateProvider(Kx.Key("k", "other", "@code", 10), Kx.Key("k", "item", "@code", 20));

        await Assert.That(Lookup(provider, document, "k", Str("a"))).IsEqualTo("i1 o1");
    }

    [Test]
    public async Task Lookup_ANodeThatTwoDeclarationsMatchWithTheSameValue_IsReturnedOnce()
    {
        var fixture = new KeyFixture();
        var document = new KeyDocument();
        var (_, provider) = fixture.CreateProvider(
            Kx.Key("k", "item", "@group", 10),
            Kx.Key("k", "item[@group = 'g1']", "@group", 20)
        );

        await Assert.That(Lookup(provider, document, "k", Str("g1"))).IsEqualTo("i1 i2");
    }

    [Test]
    [Arguments("b,a", "i1 i2 o1")]
    [Arguments("a,a", "i1 o1")]
    [Arguments("zz,b", "i2")]
    [Arguments("zz,yy", "")]
    public async Task Lookup_ASequenceOfValues_IsTheUnionInDocumentOrderWithoutDuplicates(
        string values,
        string expected
    )
    {
        Argument.ThrowIfNull(values);

        var fixture = new KeyFixture();
        var document = new KeyDocument();
        var (_, provider) = fixture.CreateProvider(Kx.Key("k", "*", "@code", 10));

        await Assert.That(Lookup(provider, document, "k", Str(values.Split(',')))).IsEqualTo(expected);
    }

    [Test]
    public async Task Lookup_AnEmptySequenceOfValues_IsEmpty()
    {
        var fixture = new KeyFixture();
        var document = new KeyDocument();
        var (_, provider) = fixture.CreateProvider(Kx.Key("k", "*", "@code", 10));

        await Assert.That(Lookup(provider, document, "k", XdmSequence.Empty)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Lookup_TwoArguments_SearchesTheDocumentOfTheContextNode()
    {
        var fixture = new KeyFixture();
        var document = new KeyDocument();
        var (_, provider) = fixture.CreateProvider(Kx.Key("k", "*", "@code", 10));

        await Assert.That(Lookup(provider, document, "k", Str("a"), from: "s1")).IsEqualTo("i1 o1");
        await Assert.That(Lookup(provider, document, "k", Str("a"), from: "o1@code")).IsEqualTo("i1 o1");
    }

    [Test]
    [Arguments("i3", "x", "s1")]
    [Arguments("i3", "A", "i3")]
    [Arguments("i3", "a", "")]
    [Arguments("catalog", "a", "i1 o1")]
    [Arguments("s1", "a", "")]
    public async Task Lookup_ThreeArguments_KeepsTheNodesAtOrBelowTheTopNode(string top, string value, string expected)
    {
        var fixture = new KeyFixture();
        var document = new KeyDocument();
        var (_, provider) = fixture.CreateProvider(Kx.Key("k", "*", "@code", 10));

        await Assert.That(Lookup(provider, document, "k", Str(value), top, subtree: true)).IsEqualTo(expected);
    }

    [Test]
    public async Task Lookup_AnUndeclaredKey_IsXtde1260()
    {
        var fixture = new KeyFixture();
        var document = new KeyDocument();
        var (_, provider) = fixture.CreateProvider(Kx.Key("k", "*", "@code", 10));

        await Assert.That(Lookup(provider, document, "missing", Str("a"))).IsEqualTo("problem:XTDE1260");
        await Assert.That(provider.GetProblem().Message).Contains("missing");
        await Assert.That(provider.IndexBuilds).IsEqualTo(0);
    }

    [Test]
    public async Task Lookup_InATreeWithoutADocumentNode_IsXtde1270()
    {
        var fixture = new KeyFixture();
        var (_, provider) = fixture.CreateProvider(Kx.Key("k", "*", "@code", 10));
        using var builder = new XmlTreeBuilder(fixture.Names, null, false);
        builder.Text("text", XmlTextKind.Text);
        var text = new XPathNode(new XmlNode(builder.Complete(), 0));

        var status = provider.TryLookup(Key("k"), Str("a"), text, false, out var nodes);

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(provider.GetProblem().Constraint).IsEqualTo("XTDE1270");
        await Assert.That(nodes.IsEmpty).IsTrue();
    }

    [Test]
    public async Task Lookup_WithoutANode_Throws()
    {
        var fixture = new KeyFixture();
        var (_, provider) = fixture.CreateProvider(Kx.Key("k", "*", "@code", 10));

        await Assert
            .That(() => provider.TryLookup(Key("k"), Str("a"), default, false, out _))
            .Throws<ArgumentNullException>();
    }

    [Test]
    [Arguments("'a'", "str:a", true)]
    [Arguments("@code", "str:a", true)]
    [Arguments("'1'", "int:1", false)]
    [Arguments("int:1", "str:1", false)]
    [Arguments("int:1", "int:1", true)]
    [Arguments("int:1", "dbl:1e0", true)]
    [Arguments("int:1", "dec:1.0", true)]
    [Arguments("dec:1.50", "dbl:1.5", true)]
    [Arguments("flt:1.5", "dec:1.5", true)]
    [Arguments("dbl:1e300", "dbl:1e300", true)]
    [Arguments("dbl:1.25", "dbl:1.25", true)]
    [Arguments("dbl:1.25", "dbl:1.5", false)]
    [Arguments("int:-7", "dbl:-7e0", true)]
    [Arguments("bool:true", "bool:true", true)]
    [Arguments("bool:false", "bool:true", false)]
    [Arguments("bool:true", "int:1", false)]
    [Arguments("dbl:NaN", "dbl:NaN", false)]
    [Arguments("int:1", "dbl:NaN", false)]
    public async Task Lookup_ComparesValuesByTheirTypeFamily(string use, string value, bool expectedMatch)
    {
        Argument.ThrowIfNull(value);

        var fixture = new KeyFixture();
        var document = new KeyDocument();
        var (_, provider) = fixture.CreateProvider(Kx.Key("k", "item", use, 10));
        var lookup = value.StartsWith("str:", StringComparison.Ordinal)
            ? Str(value.Substring(4))
            : KeyBinding.KeyRun.Typed(value);

        var found = Lookup(provider, document, "k", lookup);

        await Assert.That(found.StartsWith("problem", StringComparison.Ordinal)).IsFalse();
        await Assert.That(found.Length > 0).IsEqualTo(expectedMatch);
    }

    [Test]
    public async Task Lookup_AKeyValueOfAnUnsupportedType_IsReportedByTheIndexBuild()
    {
        var fixture = new KeyFixture();
        var document = new KeyDocument();
        var (_, provider) = fixture.CreateProvider(Kx.Key("k", "item", "date:2020-01-01", 10));

        await Assert.That(Lookup(provider, document, "k", Str("a"))).IsEqualTo("problem:");
        await Assert.That(provider.GetProblem().Constraint).IsNull();
        await Assert.That(provider.GetProblem().Message).Contains("'k'");
        await Assert.That(provider.IndexBuilds).IsEqualTo(0);
    }

    [Test]
    public async Task Lookup_AValueOfAnUnsupportedType_IsReported()
    {
        var fixture = new KeyFixture();
        var document = new KeyDocument();
        var (_, provider) = fixture.CreateProvider(Kx.Key("k", "item", "@code", 10));

        var found = Lookup(provider, document, "k", KeyBinding.KeyRun.Typed("date:2020-01-01"));

        await Assert.That(found).IsEqualTo("problem:");
        await Assert.That(provider.GetProblem().Constraint).IsNull();
        await Assert.That(provider.GetProblem().Message).Contains("not supported");
    }

    [Test]
    public async Task Lookup_AMatchThatFails_IsTheProblemOfTheRun_AndTheIndexIsNotKept()
    {
        var fixture = new KeyFixture();
        var document = new KeyDocument();
        var (_, provider) = fixture.CreateProvider(Kx.Key("k", "item[bad]", "@code", 10));

        await Assert.That(Lookup(provider, document, "k", Str("a"))).IsEqualTo("problem:XPDY0002");
        await Assert.That(provider.IndexBuilds).IsEqualTo(0);

        // A failed build is not cached, so the next lookup builds again and fails again.
        await Assert.That(Lookup(provider, document, "k", Str("a"))).IsEqualTo("problem:XPDY0002");
    }

    [Test]
    public async Task Lookup_AMatchThatFailsOnAnAttribute_IsTheProblemOfTheRun()
    {
        var fixture = new KeyFixture();
        var document = new KeyDocument();
        var (_, provider) = fixture.CreateProvider(Kx.Key("k", "@code[bad]", ".", 10));

        await Assert.That(Lookup(provider, document, "k", Str("a"))).IsEqualTo("problem:XPDY0002");
    }

    [Test]
    public async Task Lookup_AUseThatFails_IsTheProblemOfTheRun()
    {
        var fixture = new KeyFixture();
        var document = new KeyDocument();
        var (_, provider) = fixture.CreateProvider(Kx.Key("k", "item", "bad", 10));

        await Assert.That(Lookup(provider, document, "k", Str("a"))).IsEqualTo("problem:XPDY0002");
    }

    [Test]
    public async Task Lookup_AContentThatFails_IsTheProblemOfTheRun()
    {
        var fixture = new KeyFixture();
        var document = new KeyDocument();
        var failing = FunctionTree.X("if", 11, FunctionTree.A("test", "bad"));
        var (_, provider) = fixture.CreateProvider(Kx.Key("k", "item", null, 10, failing));

        await Assert.That(Lookup(provider, document, "k", Str("a"))).IsEqualTo("problem:XPDY0002");
    }

    [Test]
    public async Task Lookup_TheIndexBindsTheSlotsOfEveryDeclarationBeforeItWalksTheDocument()
    {
        var fixture = new KeyFixture("one", "two");
        var document = new KeyDocument();
        var (_, provider) = fixture.CreateProvider(
            Kx.Key("k", "item[@code = $two]", "$one", 10),
            Kx.Key("k", "other", "'x'", 20)
        );

        _ = Lookup(provider, document, "k", Str("x"));

        await Assert.That(fixture.Binder.Calls.Count).IsEqualTo(2);
        await Assert.That(fixture.Binder.Calls[0]).IsEquivalentTo([0, 1]);
        await Assert.That(fixture.Binder.Calls[1]).IsEmpty();
    }

    [Test]
    public async Task Lookup_ASlotThatCannotBeBound_IsAProblem_AndNothingIsWalked()
    {
        var fixture = new KeyFixture();
        var document = new KeyDocument();
        var (_, provider) = fixture.CreateProvider(Kx.Key("k", "item", "@code", 10));
        fixture.Binder.Fail = true;

        var status = provider.TryLookup(Key("k"), Str("a"), document["doc"], false, out _);

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(fixture.Binding.Evaluations).IsEqualTo(0);
    }

    [Test]
    public async Task Lookup_ACanceledToken_StopsTheIndexBuild()
    {
        var fixture = new KeyFixture();
        var document = new KeyDocument();
        using var source = new CancellationTokenSource();
        var (_, provider) = fixture.CreateCancellable(source.Token, Kx.Key("k", "item", "@code", 10));
        await source.CancelAsync();

        await Assert
            .That(() => provider.TryLookup(Key("k"), Str("a"), document["doc"], false, out _))
            .Throws<OperationCanceledException>();
    }

    [Test]
    public async Task Lookup_AUserFunctionInUse_IsCalledForEveryMatchingNode()
    {
        var fixture = new KeyFixture();
        var functions = FunctionFixture.Create();
        _ = functions.CompileAll(
            Fx.Function(
                "f:norm",
                "xs:string",
                30,
                Fx.Param("s", "xs:string", 31),
                Fx.Sequence("translate($s, 'abx', 'ABX')", 32)
            )
        );
        var (miniRun, _) = functions.CreateRun();
        var calls = 0;
        fixture.Binding.Function = (local, argument) =>
        {
            calls++;
            return miniRun.CallFunction(FunctionTree.Functions, local, [argument]);
        };
        var document = new KeyDocument();
        var (_, provider) = fixture.CreateProvider(Kx.Key("k", "item | sub | other", "f:norm(@code)", 10));

        // The function maps the codes a and b to upper case, so the keys of i1 and o1 are A, the one of i2 is B, i3 keeps A.
        await Assert.That(Lookup(provider, document, "k", Str("A"))).IsEqualTo("i1 i3 o1");
        await Assert.That(Lookup(provider, document, "k", Str("B"))).IsEqualTo("i2");
        await Assert.That(Lookup(provider, document, "k", Str("X"))).IsEqualTo("s1");

        // The function is called once for each of the five elements that have a code, and not again for the later lookups.
        await Assert.That(calls).IsEqualTo(5);
        await Assert.That(miniRun.Count("enter")).IsEqualTo(5);
    }

    [Test]
    public async Task Provider_ContractLookup_UsesTheDocumentOfTheProvider()
    {
        var fixture = new KeyFixture();
        var document = new KeyDocument();
        var (_, provider) = fixture.CreateProvider(Kx.Key("k", "*", "@code", 10));
        using var context = new XPathEvaluationContext();
        context.Host.Keys = provider;
        provider.Document = document["doc"];

        var found = context.Host.Keys.TryGetNodes(Key("k"), XdmItem.FromString("a"), out var nodes);

        await Assert.That(found).IsTrue();
        await Assert.That(document.Keys(Nodes(nodes))).IsEqualTo("i1 o1");
        await Assert.That(context.Host.Keys.TryGetNodes(Key("missing"), XdmItem.FromString("a"), out _)).IsFalse();
        await Assert.That(provider.GetProblem().Constraint).IsEqualTo("XTDE1260");
    }

    [Test]
    public async Task Provider_ContractLookup_WithoutADocument_IsFalse()
    {
        var fixture = new KeyFixture();
        var (_, provider) = fixture.CreateProvider(Kx.Key("k", "*", "@code", 10));

        var found = provider.TryGetNodes(Key("k"), XdmItem.FromString("a"), out var nodes);

        await Assert.That(found).IsFalse();
        await Assert.That(nodes.IsEmpty).IsTrue();
        await Assert.That(provider.GetProblem().Message).Contains("no document");
    }

    [Test]
    public async Task Provider_NullArguments_Throw()
    {
        var fixture = new KeyFixture();
        var table = XsltKeyTable.Create([]);
        var matcher = new PatternMatcher(fixture.Binding);
        var run = fixture.Binding.BeginRun(null!, default);

        await Assert
            .That(() => new XsltKeyProvider(null!, matcher, run, fixture.Binder, fixture.Names))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new XsltKeyProvider(table, null!, run, fixture.Binder, fixture.Names))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new XsltKeyProvider(table, matcher, null!, fixture.Binder, fixture.Names))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new XsltKeyProvider(table, matcher, run, null!, fixture.Names))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new XsltKeyProvider(table, matcher, run, fixture.Binder, null!))
            .Throws<ArgumentNullException>();
    }
}
