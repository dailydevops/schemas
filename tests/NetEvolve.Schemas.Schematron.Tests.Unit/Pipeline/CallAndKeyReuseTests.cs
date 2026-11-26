namespace NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline;

using System.Linq;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Schematron.Tests.Unit.Loading.Inclusion;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline.PipelineFixture;

/// <summary>
/// Characterization of the reuse in stylesheet function calls and key lookups (#1210): sinks of a call level, the resolved function,
/// the buckets of an index, and the memo of the last index must never show one result in place of another.
/// </summary>
public class CallAndKeyReuseTests
{
    private const string Xsl = "xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\"";

    private const string Functions =
        "<xsl:function name=\"f:two\"><xsl:param name=\"n\"/><xsl:sequence select=\"($n, $n + 1)\"/></xsl:function>"
        + "<xsl:function name=\"f:four\"><xsl:param name=\"n\"/><xsl:sequence select=\"(f:two($n), f:two($n + 10))\"/></xsl:function>"
        + "<xsl:function name=\"f:risky\"><xsl:param name=\"s\"/><xsl:sequence select=\"if ($s = 'bad') then xs:integer($s) else ($s, $s)\"/></xsl:function>"
        + "<xsl:function name=\"f:text\"><xsl:param name=\"s\"/><xsl:value-of select=\"$s\"/><xsl:value-of select=\"$s\"/></xsl:function>";

    private const string Keys =
        "<xsl:key name=\"k\" match=\"item\" use=\"@id\"/>"
        + "<xsl:key name=\"any\" match=\"*\" use=\"@id\"/>"
        + "<xsl:key name=\"attr\" match=\"@id\" use=\".\"/>"
        + "<xsl:key name=\"nested\" match=\"item\" use=\"count(key('k', 'x'))\"/>";

    private const string Document =
        "<doc><item id=\"x\"/><item id=\"y\"/><item id=\"x\"/><sub id=\"x\"><item id=\"x\"/></sub></doc>";

    private static SchematronValidatorBuilder BuilderOf(string rule, string context = "doc") =>
        Builder(
            Wrap(
                "<ns prefix=\"f\" uri=\"urn:f\"/><ns prefix=\"xs\" uri=\"http://www.w3.org/2001/XMLSchema\"/>"
                    + Functions
                    + Keys
                    + $"<pattern id=\"p\"><rule context=\"{context}\">{rule}</rule></pattern>",
                $" queryBinding=\"xslt2\" schematronEdition=\"2025\" {Xsl} xmlns:f=\"urn:f\" xmlns:xs=\"http://www.w3.org/2001/XMLSchema\""
            ),
            "file:///schemas/rules.sch"
        );

    private static ISchematronValidator Inline(string test, string context = "doc") =>
        BuilderOf($"<assert test=\"{test}\">Failed.</assert>", context).BuildNow();

    private static async Task Holds(string test, string document = Document, string context = "doc")
    {
        var result = Validate(Inline(test, context), document);

        await Assert.That(result.IsValid).IsTrue().Because(test);
    }

    [Test]
    public async Task Call_TwoCallsInOneExpression_KeepTheirOwnSequences() =>
        await Holds("string-join(for $i in (f:two(1), f:two(10)) return string($i), ',') = '1,2,10,11'");

    [Test]
    public async Task Call_NestedCalls_UseOneSinkPerLevel() =>
        await Holds("string-join(for $i in f:four(1) return string($i), ',') = '1,2,11,12'");

    [Test]
    public async Task Call_ConstructedText_DoesNotLeakIntoTheNextCall() =>
        await Holds("concat(f:text('a'), '|', f:text('b'), '|', f:text('a')) = 'aa|bb|aa'");

    [Test]
    public async Task Call_AfterAFailedCall_StartsFromAnEmptySink()
    {
        var validator = BuilderOf("<assert test=\"count(f:risky(@v)) = 2\">Failed.</assert>", "item").BuildNow();

        var result = Validate(validator, "<doc><item v=\"bad\"/><item v=\"good\"/><item v=\"fine\"/></doc>");

        await Assert.That(result.Issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Call_ConcurrentRunsOfOneValidator_ReturnTheirOwnResults()
    {
        var validator = Inline("string-join(for $i in f:four(count(item)) return string($i), ',') = '3,4,13,14'");

        var results = await Task.WhenAll(
            Enumerable
                .Range(0, 16)
                .Select(_ => Task.Run(() => Enumerable.Range(0, 10).All(__ => Validate(validator, Document).IsValid)))
        );

        await Assert.That(results.All(valid => valid)).IsTrue();
    }

    [Test]
    public async Task Key_OneValue_ReturnsTheBucketInDocumentOrderEveryTime() =>
        await Holds(
            "count(key('k', 'x')) = 3 and count(key('k', 'x')) = 3 and key('k', 'x')[3]/parent::sub and not(key('k', 'x')[1]/parent::sub)"
        );

    [Test]
    public async Task Key_OneValueThatIsNotIndexed_IsEmpty() =>
        await Holds("empty(key('k', 'nope')) and empty(key('k', 5)) and empty(key('k', ()))");

    [Test]
    public async Task Key_SeveralValues_AreMergedInDocumentOrderWithoutDuplicates() =>
        await Holds("count(key('k', ('y', 'x', 'x'))) = 4 and key('k', ('y', 'x'))[2]/@id = 'y'");

    [Test]
    public async Task Key_WithATopNode_KeepsTheSubtreeOfOneValue() =>
        await Holds("count(key('k', 'x', sub)) = 1 and count(key('k', 'x', .)) = 3");

    [Test]
    public async Task Key_ValueOfAnUnsupportedType_IsAProblem()
    {
        var result = Validate(Inline("empty(key('k', xs:date('2020-01-01')))"), Document);

        await Assert.That(result.Issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Key_SeveralValuesOfWhichOneIsUnsupported_IsAProblem()
    {
        var result = Validate(Inline("empty(key('k', ('x', xs:date('2020-01-01'))))"), Document);

        await Assert.That(result.Issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Key_DeclarationsByName_MatchOnlyTheNodesTheyName() =>
        await Holds("count(key('k', 'x')) = 3 and count(key('any', 'x')) = 4 and count(key('attr', 'x')) = 4");

    [Test]
    public async Task Key_BuildThatLooksUpAnotherKey_NeedsItsOwnSink() =>
        await Holds("count(key('nested', 3)) = 4 and count(key('k', 'x')) = 3");

    [Test]
    public async Task Key_OfAnotherDocument_ReplacesAndRestoresTheRememberedIndex()
    {
        var files = new FakeResources().Add("file:///schemas/o.xml", "<doc><item id=\"x\"/></doc>");
        var validator = BuilderOf(
                "<assert test=\"count(key('k', 'x')) = 3 and count(document('o.xml')/doc/key('k', 'x')) = 1 and count(key('k', 'x')) = 3 and count(document('o.xml')/doc/key('any', 'x')) = 1\">Failed.</assert>"
            )
            .WithResolver(files)
            .BuildNow();

        var result = Validate(validator, Document);

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Key_TwoNamesInOneDocument_DoNotShareTheRememberedIndex() =>
        await Holds("count(key('k', 'x')) = 3 and count(key('any', 'x')) = 4 and count(key('k', 'x')) = 3");
}
