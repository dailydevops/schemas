namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Grouping;

using System.Threading.Tasks;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Grouping.Gr;

/// <summary>
/// <c>xsl:for-each-group group-adjacent</c> (XSLT 2.0 section 14.3): an item starts a new group whenever its key differs from the key
/// of the item before it, and the key must be exactly one atomic value.
/// </summary>
public class GroupAdjacentTests
{
    private static string Problem(DiagnosticCode code, string? constraint = null) =>
        "problem:" + code + (constraint is null ? string.Empty : "/" + constraint);

    [Test]
    public async Task GroupAdjacent_StartsANewGroup_WheneverTheKeyChanges()
    {
        var harness = new GroupHarness(Adjacent("*", "@k", 10, KeyAndMembers(11)), "i=a i=a i=b i=a i=a");

        await Assert.That(harness.Result()).IsEqualTo("a:i=a i=a;b:i=b;a:i=a i=a;");
    }

    [Test]
    public async Task GroupAdjacent_AllItemsWithOneKey_AreOneGroup()
    {
        var harness = new GroupHarness(Adjacent("*", "@k", 10, KeyAndMembers(11)), "i=a i=a i=a");

        await Assert.That(harness.Result()).IsEqualTo("a:i=a i=a i=a;");
    }

    [Test]
    public async Task GroupAdjacent_NaNIsAdjacentToNaN_AndANumberIsNotAdjacentToItsStringForm()
    {
        var harness = new GroupHarness(Adjacent("dbl:NaN, dbl:NaN, int:1, '1', dbl:1e0", ".", 10, KeyAndMembers(11)));

        await Assert.That(harness.Result()).IsEqualTo("NaN:NaN NaN;1:1;1:1;1:1;");
    }

    [Test]
    public async Task GroupAdjacent_KeyEvaluation_SeesThePositionAndTheSizeOfThePopulation()
    {
        var harness = new GroupHarness(Adjacent("*", "last()", 10, KeyAndMembers(11)), "i i i i");

        await Assert.That(harness.Result()).IsEqualTo("4:i i i i;");
    }

    [Test]
    public async Task GroupAdjacent_AnEmptyKey_IsTheTypeErrorXtte1100()
    {
        var harness = new GroupHarness(Adjacent("*", "@k", 10, Members(11)), "i=a h");

        await Assert.That(harness.Result()).IsEqualTo(Problem(SchematronDiagnosticCodes.EvaluationFailed, "XTTE1100"));
    }

    [Test]
    public async Task GroupAdjacent_MoreThanOneKey_IsTheTypeErrorXtte1100()
    {
        var harness = new GroupHarness(Adjacent("*", "@k, @j", 10, Members(11)), "i=a/x");

        await Assert.That(harness.Result()).IsEqualTo(Problem(SchematronDiagnosticCodes.EvaluationFailed, "XTTE1100"));
    }

    [Test]
    public async Task GroupAdjacent_KeyOfAnUnsupportedType_IsAnEvaluationProblem()
    {
        var harness = new GroupHarness(Adjacent("date:2020-01-01", ".", 10, Members(11)));

        await Assert.That(harness.Result()).IsEqualTo(Problem(SchematronDiagnosticCodes.EvaluationFailed));
    }

    [Test]
    public async Task GroupAdjacent_KeyProblem_IsReported()
    {
        var harness = new GroupHarness(Adjacent("*", "error()", 10, Members(11)), "i=a");

        await Assert.That(harness.Result()).IsEqualTo(Problem(SchematronDiagnosticCodes.EvaluationFailed, "FOER0000"));
    }

    [Test]
    public async Task GroupAdjacent_MoreItemsThanTheSequenceLimit_IsTheCodedLimitError()
    {
        var harness = new GroupHarness(Adjacent("*", "@k", 10, Members(11)), "i=a i=b i=c i=d");
        harness.Frame.MaxSequenceLength = 3;

        await Assert.That(harness.Result()).IsEqualTo(Problem(SchematronDiagnosticCodes.EvaluationLimitStop));
    }
}
