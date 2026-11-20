namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Grouping;

using System.Threading.Tasks;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.FunctionTree;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Grouping.Gr;

/// <summary>
/// <c>xsl:sort</c> inside <c>xsl:for-each-group</c> (XSLT 2.0 section 14.3): the sort keys order the processing of the groups and not the
/// items of a group. A key is evaluated once per group with the initial item of the group as the context item, its position among the
/// initial items in population order, the number of groups as the size, and the current group and grouping key of the group.
/// </summary>
public class GroupSortTests
{
    private const string Doc = "i=b i=a i=b i=c";

    [Test]
    public async Task Sort_ByTheGroupingKey_ChangesTheProcessingOrderOfTheGroups()
    {
        var harness = new GroupHarness(
            By("*", "@k", 10, Sort("current-grouping-key()", null, 11), KeyAndMembers(12)),
            Doc
        );

        await Assert.That(harness.Result()).IsEqualTo("a:i=a;b:i=b i=b;c:i=c;");
    }

    [Test]
    public async Task Sort_Descending_ReversesTheOrderOfTheKeys()
    {
        var harness = new GroupHarness(
            By("*", "@k", 10, Sort("current-grouping-key()", "descending", 11), KeyAndMembers(12)),
            Doc
        );

        await Assert.That(harness.Result()).IsEqualTo("c:i=c;b:i=b i=b;a:i=a;");
    }

    [Test]
    public async Task Sort_ByTheSizeOfTheGroup_UsesTheCurrentGroup_AndKeepsTheOrderOfFirstAppearanceOnATie()
    {
        var harness = new GroupHarness(
            By("*", "@k", 10, Sort("count(current-group())", "descending", 11), KeyAndMembers(12)),
            "i=a i=b i=b i=c"
        );

        await Assert.That(harness.Result()).IsEqualTo("b:i=b i=b;a:i=a;c:i=c;");
    }

    [Test]
    public async Task Sort_DoesNotChangeTheOrderOfTheItemsInAGroup()
    {
        var harness = new GroupHarness(
            By("*", "@k", 10, Sort("current-grouping-key()", "descending", 11), Members(12)),
            "i=a/2 i=a/1 i=b/3 i=a/0"
        );

        await Assert.That(harness.Result()).IsEqualTo("i=b/3;i=a/2 i=a/1 i=a/0;");
    }

    [Test]
    public async Task Sort_KeyIsEvaluatedWithTheInitialItem_ThePositionAmongTheInitialItems_AndTheNumberOfGroups()
    {
        var harness = new GroupHarness(
            By("*", "@k", 10, Sort("concat('P', position(), '/', last())", null, 11), KeyAndMembers(12)),
            Doc
        );

        _ = harness.Execute();

        var evaluations = harness.Run.Evaluations;
        await Assert.That(evaluations).Contains("concat('P', position(), '/', last())@1/3");
        await Assert.That(evaluations).Contains("concat('P', position(), '/', last())@2/3");
        await Assert.That(evaluations).Contains("concat('P', position(), '/', last())@3/3");
    }

    [Test]
    public async Task Sort_ByTheInitialItem_OrdersTheGroupsByTheirFirstMember()
    {
        var harness = new GroupHarness(By("*", "@k", 10, Sort("join(.)", null, 11), KeyAndMembers(12)), Doc);

        await Assert.That(harness.Result()).IsEqualTo("a:i=a;b:i=b i=b;c:i=c;");
    }

    [Test]
    public async Task Sort_ByThePositionDescending_ReversesTheOrderOfFirstAppearance()
    {
        var harness = new GroupHarness(By("*", "@k", 10, Sort("position()", "descending", 11), KeyAndMembers(12)), Doc);

        await Assert.That(harness.Result()).IsEqualTo("c:i=c;a:i=a;b:i=b i=b;");
    }

    [Test]
    public async Task Body_PositionAndSize_FollowTheProcessingOrder()
    {
        var harness = new GroupHarness(
            By(
                "*",
                "@k",
                10,
                Sort("current-grouping-key()", "descending", 11),
                X(
                    "value-of",
                    12,
                    A("select", "concat(position(), '/', last(), ':', string(current-grouping-key()), ';')")
                )
            ),
            "i=a i=b i=c"
        );

        await Assert.That(harness.Result()).IsEqualTo("1/3:c;2/3:b;3/3:a;");
    }

    [Test]
    public async Task Sort_SeveralKeys_MajorToMinor()
    {
        var harness = new GroupHarness(
            By(
                "*",
                "@k",
                10,
                Sort("count(current-group())", "descending", 11),
                Sort("current-grouping-key()", "descending", 12),
                KeyAndMembers(13)
            ),
            "i=a i=b i=c i=c i=d i=d"
        );

        await Assert.That(harness.Result()).IsEqualTo("d:i=d i=d;c:i=c i=c;b:i=b;a:i=a;");
    }

    [Test]
    public async Task Sort_StartingWith_OrdersTheGroups_AndTheGroupingKeyIsEmptyDuringTheKeyEvaluation()
    {
        var harness = new GroupHarness(
            Starting("*", "h", 10, Sort("count(current-group())", "descending", 11), Members(12)),
            "h h p p h p"
        );

        await Assert.That(harness.Result()).IsEqualTo("h p p;h p;h;");
        await Assert.That(harness.Run.Writes).Contains("key:-");
    }

    [Test]
    public async Task Sort_ASingleGroup_NeedsNoComparison()
    {
        var harness = new GroupHarness(
            By("*", "@k", 10, Sort("current-grouping-key()", null, 11), Members(12)),
            "i=a i=a"
        );

        await Assert.That(harness.Result()).IsEqualTo("i=a i=a;");
    }

    [Test]
    public async Task Sort_KeysOfTwoTypes_AreTheTypeErrorXtde1030()
    {
        var harness = new GroupHarness(
            By("int:1, 'a'", ".", 10, Sort("current-grouping-key()", null, 11), Members(12))
        );

        await Assert
            .That(harness.Result())
            .IsEqualTo("problem:" + SchematronDiagnosticCodes.EvaluationFailed + "/XTDE1030");
    }

    [Test]
    public async Task Sort_KeyProblem_IsReported_AndTheSlotsAreRestored()
    {
        var harness = new GroupHarness(By("*", "@k", 10, Sort("error()", null, 11), Members(12)), Doc);

        var result = harness.Result();

        await Assert.That(result).IsEqualTo("problem:" + SchematronDiagnosticCodes.EvaluationFailed + "/FOER0000");
        await Assert.That(harness.Run.CurrentGroup).IsNull();
        await Assert.That(harness.Run.CurrentGroupingKey).IsNull();
    }
}
