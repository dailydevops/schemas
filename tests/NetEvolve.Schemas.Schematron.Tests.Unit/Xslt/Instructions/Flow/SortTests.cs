namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Flow;

using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Construction;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Flow.Fl;

/// <summary>
/// <c>xsl:sort</c> under <c>xsl:for-each</c> (XSLT 2.0 section 13, design #150 EP-8): the keys are atomized and compared by the XPath
/// rules, the order is stable in both directions, and the codepoint collation is the only one.
/// </summary>
public class SortTests
{
    private const string Codepoint = "http://www.w3.org/2005/xpath-functions/collation/codepoint";

    private static FlowHarness Loop(string select, XmlElem[] keys, string body = ".", params string[] variables)
    {
        var content = new object[keys.Length + 1];
        for (var i = 0; i < keys.Length; i++)
        {
            content[i] = keys[i];
        }

        content[keys.Length] = ValueOf(body, null, 90);
        return new FlowHarness(ForEach(select, 10, content), variables);
    }

    private static async Task<string> Output(FlowHarness harness)
    {
        var status = harness.Execute();
        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        return harness.Sink.Joined();
    }

    [Test]
    public async Task Sort_WithoutSelect_SortsOnTheItemItself_Ascending()
    {
        var harness = Loop("(3, 1, 2)", [Sort(null, null, 11)]);

        await Assert.That(await Output(harness)).IsEqualTo("123");
    }

    [Test]
    public async Task Sort_Ascending_AndDescending_ByNumber()
    {
        var ascending = Loop("(10, 9, 100)", [Sort(".", "ascending", 11)]);
        var descending = Loop("(10, 9, 100)", [Sort(".", "descending", 11)]);

        await Assert.That(await Output(ascending)).IsEqualTo("910100");
        await Assert.That(await Output(descending)).IsEqualTo("100109");
    }

    [Test]
    public async Task Sort_Strings_AreOrderedByCodepoint()
    {
        var harness = Loop("('b', 'A', 'a', 'B')", [Sort(".", null, 11)]);

        await Assert.That(await Output(harness)).IsEqualTo("ABab");
    }

    [Test]
    public async Task Sort_Strings_SupplementaryCharacterFollowsTheEndOfTheBasicPlane()
    {
        var smile = char.ConvertFromUtf32(0x1F600);
        var harness = Loop($"('{smile}', '�')", [Sort(".", null, 11)]);

        await Assert.That(await Output(harness)).IsEqualTo("�" + smile);
    }

    [Test]
    public async Task Sort_EqualKeys_KeepTheInputOrder_InBothDirections()
    {
        const string items = "(11, 21, 12, 22, 13)";
        var ascending = Loop(items, [Sort(". mod 10", "ascending", 11)]);
        var descending = Loop(items, [Sort(". mod 10", "descending", 11)]);

        await Assert.That(await Output(ascending)).IsEqualTo("1121122213");
        await Assert.That(await Output(descending)).IsEqualTo("1312221121");
    }

    [Test]
    public async Task Sort_TwoKeys_ThePrimaryDecidesAndTheSecondBreaksTies()
    {
        var harness = Loop(
            "('b1', 'a2', 'a1', 'b2')",
            [Sort("substring(., 1, 1)", null, 11), Sort("substring(., 2, 1)", "descending", 12)]
        );

        await Assert.That(await Output(harness)).IsEqualTo("a2a1b2b1");
    }

    [Test]
    public async Task Sort_EmptyKey_SortsBeforeEveryValue_AndEmptyKeysAreEqual()
    {
        var ascending = Loop("(3, 2, 4, 1)", [Sort("if(. = 2 or . = 4) then () else .", null, 11)]);
        var descending = Loop("(3, 2, 4, 1)", [Sort("if(. = 2 or . = 4) then () else .", "descending", 11)]);

        await Assert.That(await Output(ascending)).IsEqualTo("2413");
        await Assert.That(await Output(descending)).IsEqualTo("3124");
    }

    [Test]
    public async Task Sort_NaN_IsEqualToItselfAndLessThanAnyOtherNumber()
    {
        const string select = "if(. = 1 or . = 4) then $nan else if(. = 2) then $two else $one";
        var ascending = Loop("(1, 2, 3, 4)", [Sort(select, null, 11)], ".", "nan", "two", "one");
        var descending = Loop("(1, 2, 3, 4)", [Sort(select, "descending", 11)], ".", "nan", "two", "one");
        foreach (var harness in new[] { ascending, descending })
        {
            harness.Bind("nan", XdmSequence.Create(XdmItem.FromDouble(double.NaN)));
            harness.Bind("two", XdmSequence.Create(XdmItem.FromDouble(2)));
            harness.Bind("one", XdmSequence.Create(XdmItem.FromDouble(1)));
        }

        await Assert.That(await Output(ascending)).IsEqualTo("1432");
        await Assert.That(await Output(descending)).IsEqualTo("2314");
    }

    [Test]
    public async Task Sort_IntegersAndDoubles_CompareAsNumbers()
    {
        var harness = Loop(
            "(1, 2, 3)",
            [Sort("if(. = 1) then 2 else if(. = 2) then $half else 1", null, 11)],
            ".",
            "half"
        );
        harness.Bind("half", XdmSequence.Create(XdmItem.FromDouble(1.5)));

        await Assert.That(await Output(harness)).IsEqualTo("321");
    }

    [Test]
    public async Task Sort_BooleansOrderFalseBeforeTrue()
    {
        var harness = Loop("(1, 2, 3)", [Sort(". = 2", null, 11)]);

        await Assert.That(await Output(harness)).IsEqualTo("132");
    }

    [Test]
    public async Task Sort_NodeKeys_AreComparedByTheirStringValue()
    {
        var harness = Loop("$nodes", [Sort(".", null, 11)], ".", "nodes");
        harness.Bind(
            "nodes",
            XdmSequence.Create([SinkTestSupport.Element("a", "b"), SinkTestSupport.Element("a", "a")])
        );

        await Assert.That(await Output(harness)).IsEqualTo("ab");
    }

    [Test]
    public async Task Sort_KeysOfMixedTypes_AreXtde1030()
    {
        var harness = Loop("(1, 2)", [Sort("if(. = 1) then 'a' else 1", null, 11)]);

        var status = harness.Execute();

        var problem = harness.Frame.TakeProblem();
        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.EvaluationFailed);
        await Assert.That(problem.Constraint).IsEqualTo("XTDE1030");
        await Assert.That(problem.Location.Line).IsEqualTo(11);
        await Assert.That(harness.Sink.Events.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Sort_KeyOfMoreThanOneItem_IsXtte1020()
    {
        var harness = Loop("(1, 2)", [Sort("(., .)", null, 11)]);

        var status = harness.Execute();

        var problem = harness.Frame.TakeProblem();
        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(problem.Constraint).IsEqualTo("XTTE1020");
    }

    [Test]
    public async Task Sort_FailingKey_ReturnsTheProblemOfTheRun()
    {
        var harness = Loop("(1, 2)", [Sort("1 idiv 0", null, 11)]);

        var status = harness.Execute();

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(harness.Frame.TakeProblem().Constraint).IsEqualTo("FOAR0001");
    }

    [Test]
    public async Task Sort_KeyIsEvaluatedWithTheFocusOfTheInitialSequence()
    {
        var harness = Loop("(7, 8, 9)", [Sort("position()", "descending", 11)], "concat(., ':', last())");

        var output = await Output(harness);

        await Assert.That(output).IsEqualTo("9:38:37:3");
        await Assert.That(harness.Run.Focuses[1].Position).IsEqualTo(1);
        await Assert.That(harness.Run.Focuses[3].Position).IsEqualTo(3);
    }

    [Test]
    public async Task Sort_BodyFocus_HasThePositionInTheSortedSequence()
    {
        var harness = Loop("(1, 2, 3)", [Sort(".", "descending", 11)], "concat(position(), '/', last(), ':', .)");

        await Assert.That(await Output(harness)).IsEqualTo("1/3:32/3:23/3:1");
    }

    [Test]
    public async Task Sort_DataTypeText_ComparesTheStringsOfTheValues()
    {
        var harness = Loop("(10, 9, 100)", [Sort(".", null, 11, null, "text")]);

        await Assert.That(await Output(harness)).IsEqualTo("10100" + "9");
    }

    [Test]
    public async Task Sort_StableAttribute_IsOnlyAllowedOnTheFirstSort()
    {
        var first = Loop("(1, 2)", [Sort(".", null, 11, "no"), Sort(".", null, 12)]);
        var second = Loop("(1, 2)", [Sort(".", null, 11), Sort(".", null, 12, "yes")]);

        await Assert.That(first.CompileCodes).IsEqualTo(string.Empty);
        await Assert.That(await Output(first)).IsEqualTo("12");
        await Assert.That(second.Executable).IsNull();
        await Assert.That(second.CompileCodes).IsEqualTo("NES3006/XTSE1017");
    }

    [Test]
    [Arguments("order", "sideways")]
    [Arguments("stable", "maybe")]
    public async Task Sort_InvalidValue_IsStaticErrorXtse0020(string attribute, string value)
    {
        var sort = string.Equals(attribute, "order", System.StringComparison.Ordinal)
            ? Sort(".", value, 11)
            : Sort(".", null, 11, value);
        var harness = Loop("(1, 2)", [sort]);

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo("NES3006/XTSE0020");
    }

    [Test]
    public async Task Sort_OrderTemplate_IsUnsupported()
    {
        var harness = Loop("(1, 2)", [Sort(".", "{$d}", 11)], ".", "d");

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo("NES3204");
    }

    [Test]
    [Arguments("number")]
    [Arguments("t:custom")]
    public async Task Sort_DataTypeOtherThanText_IsUnsupported(string dataType)
    {
        var harness = Loop("(1, 2)", [Sort(".", null, 11, null, dataType)]);

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo("NES3204");
    }

    [Test]
    public async Task Sort_Collation_OnlyTheCodepointCollationIsKnown()
    {
        var known = Loop("('b', 'a')", [Sort(".", null, 11, null, null, Codepoint)]);
        var other = Loop("('b', 'a')", [Sort(".", null, 11, null, null, "urn:other")]);

        await Assert.That(await Output(known)).IsEqualTo("ab");
        await Assert.That(other.Executable).IsNull();
        await Assert.That(other.CompileCodes).IsEqualTo("NES3204");
    }

    [Test]
    public async Task Sort_LangAndCaseOrder_AreIgnoredWithACollation_AndUnsupportedWithout()
    {
        var with = Loop("('b', 'a')", [Sort(".", null, 11, null, null, Codepoint, "de", "upper-first")]);
        var lang = Loop("('b', 'a')", [Sort(".", null, 11, null, null, null, "de")]);
        var caseOrder = Loop("('b', 'a')", [Sort(".", null, 11, null, null, null, null, "upper-first")]);

        await Assert.That(await Output(with)).IsEqualTo("ab");
        await Assert.That(lang.CompileCodes).IsEqualTo("NES3204");
        await Assert.That(caseOrder.CompileCodes).IsEqualTo("NES3204");
    }

    [Test]
    public async Task Sort_SelectAndContent_IsStaticErrorXtse1015()
    {
        var harness = Loop("(1, 2)", [Sort(".", null, 11, null, null, null, null, null, Seq("1", 12))]);

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo("NES3003/XTSE1015");
    }

    [Test]
    public async Task Sort_ContentInPlaceOfSelect_IsUnsupported()
    {
        var harness = Loop("(1, 2)", [Sort(null, null, 11, null, null, null, null, null, Seq("1", 12))]);

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo("NES3204");
    }

    [Test]
    public async Task Sort_AfterOtherContent_IsMisplaced()
    {
        var harness = new FlowHarness(ForEach("(1, 2)", 10, ValueOf(".", null, 11), Sort(".", null, 12)));

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo("NES3003/XTSE0010");
    }

    [Test]
    public async Task Sort_OutsideForEach_IsMisplaced()
    {
        var harness = new FlowHarness(Sort(".", null, 10));

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo("NES3003/XTSE0010");
    }

    [Test]
    public async Task Sort_InvalidSelect_ReportsTheExpressionProblem()
    {
        var harness = Loop("(1, 2)", [Sort("1 +", null, 11)]);

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo("NES3202/XPST0003");
    }
}
