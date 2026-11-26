namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Grouping;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Grouping;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.FunctionTree;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Grouping.Gr;

/// <summary>
/// The compile of <c>xsl:for-each-group</c> (XSLT 2.0 section 14.3): the four grouping attributes are mutually exclusive, the
/// collation is the codepoint collation, the XSLT 3.0 attribute <c>composite</c> is the unsupported construct error, the expressions
/// and the pattern compile in the scope of the element, and every problem is reported without stopping the compile.
/// </summary>
public class GroupCompileTests
{
    private static string Code(DiagnosticCode code, string? constraint = null) =>
        code + (constraint is null ? string.Empty : "/" + constraint);

    [Test]
    public async Task Compile_WithoutSelect_IsTheMissingAttributeError()
    {
        var harness = new GroupHarness(Group(10, [("group-by", "@k")], Members(11)));

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo(Code(SchematronDiagnosticCodes.AttributeMissing));
    }

    [Test]
    public async Task Compile_WithoutAGroupingAttribute_IsXtse1080()
    {
        var harness = new GroupHarness(Group(10, [("select", "*")], Members(11)));

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo(Code(SchematronDiagnosticCodes.AttributeMissing, "XTSE1080"));
    }

    [Test]
    public async Task Compile_WithTwoGroupingAttributes_IsXtse1080()
    {
        var harness = new GroupHarness(
            Group(10, [("select", "*"), ("group-by", "@k"), ("group-adjacent", "@k")], Members(11))
        );

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo(Code(SchematronDiagnosticCodes.AttributeInvalid, "XTSE1080"));
    }

    [Test]
    public async Task Compile_WithAllFourGroupingAttributes_IsOneXtse1080()
    {
        var harness = new GroupHarness(
            Group(
                10,
                [
                    ("select", "*"),
                    ("group-by", "@k"),
                    ("group-adjacent", "@k"),
                    ("group-starting-with", "h"),
                    ("group-ending-with", "h"),
                ],
                Members(11)
            )
        );

        await Assert.That(harness.CompileCodes).IsEqualTo(Code(SchematronDiagnosticCodes.AttributeInvalid, "XTSE1080"));
    }

    [Test]
    [Arguments("group-by")]
    [Arguments("group-adjacent")]
    public async Task Compile_CodepointCollation_WithAKeyAttribute_Compiles(string attribute)
    {
        var harness = new GroupHarness(
            Group(
                10,
                [
                    ("select", "*"),
                    (attribute, "@k"),
                    ("collation", "http://www.w3.org/2005/xpath-functions/collation/codepoint"),
                ],
                Members(11)
            )
        );

        await Assert.That(harness.Executable).IsNotNull();
        await Assert.That(harness.CompileCodes).IsEqualTo(string.Empty);
    }

    [Test]
    [Arguments("group-starting-with")]
    [Arguments("group-ending-with")]
    public async Task Compile_CollationWithAPattern_IsXtse1090(string attribute)
    {
        var harness = new GroupHarness(
            Group(
                10,
                [
                    ("select", "*"),
                    (attribute, "h"),
                    ("collation", "http://www.w3.org/2005/xpath-functions/collation/codepoint"),
                ],
                Members(11)
            )
        );

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo(Code(SchematronDiagnosticCodes.AttributeInvalid, "XTSE1090"));
    }

    [Test]
    public async Task Compile_AnotherCollation_IsTheUnsupportedConstructError()
    {
        var harness = new GroupHarness(
            Group(10, [("select", "*"), ("group-by", "@k"), ("collation", "urn:other")], Members(11))
        );

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo(Code(SchematronDiagnosticCodes.XsltUnsupported));
    }

    [Test]
    [Arguments("yes")]
    [Arguments("no")]
    public async Task Compile_Composite_IsTheUnsupportedConstructError(string value)
    {
        var harness = new GroupHarness(
            Group(10, [("select", "*"), ("group-by", "@k"), ("composite", value)], Members(11))
        );

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo(Code(SchematronDiagnosticCodes.XsltUnsupported));
    }

    [Test]
    [Arguments("xslt2")]
    [Arguments("xslt3")]
    public async Task Compile_Composite_IsTheUnsupportedConstructError_UnderEveryXsltBinding(string binding)
    {
        var harness = new GroupHarness(
            Group(10, [("select", "*"), ("group-adjacent", "@k"), ("composite", "yes")], Members(11)),
            string.Empty,
            binding
        );

        await Assert.That(harness.CompileCodes).IsEqualTo(Code(SchematronDiagnosticCodes.XsltUnsupported));
    }

    [Test]
    public async Task Compile_InvalidExpressions_AreAllReported()
    {
        var harness = new GroupHarness(By("(", "@", 10, Members(11)));

        await Assert.That(harness.Executable).IsNull();
        await Assert
            .That(harness.CompileCodes)
            .IsEqualTo(
                Code(SchematronDiagnosticCodes.ExpressionInvalid, "XPST0003")
                    + ","
                    + Code(SchematronDiagnosticCodes.ExpressionInvalid, "XPST0003")
            );
    }

    [Test]
    [Arguments("h[")]
    public async Task Compile_InvalidPattern_IsReported(string pattern)
    {
        var harness = new GroupHarness(Starting("*", pattern, 10, Members(11)));

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).StartsWith(SchematronDiagnosticCodes.ExpressionInvalid.ToString());
    }

    [Test]
    public async Task Compile_APatternBeyondTheSubsetOfTheSharedMatcher_IsTheUnsupportedPatternError()
    {
        var harness = new GroupHarness(Ending("*", "id('v')", 10, Members(11)));

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).StartsWith(SchematronDiagnosticCodes.PatternUnsupported.ToString());
    }

    [Test]
    public async Task Compile_ASortAfterTheFirstInstruction_IsMisplaced()
    {
        var harness = new GroupHarness(By("*", "@k", 10, Members(11), Sort("@k", null, 12)));

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).StartsWith(SchematronDiagnosticCodes.ElementMisplaced.ToString());
    }

    [Test]
    public async Task Compile_ASortKeyThatIsNotSupported_IsReported()
    {
        var harness = new GroupHarness(
            By("*", "@k", 10, X("sort", 11, A("select", "@k"), A("data-type", "number")), Members(12))
        );

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo(Code(SchematronDiagnosticCodes.XsltUnsupported));
    }

    [Test]
    public async Task Compile_AProblemInTheBody_IsReported()
    {
        var harness = new GroupHarness(By("*", "@k", 10, X("if", 11, A("test", "("))));

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).StartsWith(SchematronDiagnosticCodes.ExpressionInvalid.ToString());
    }

    [Test]
    public async Task Compile_AnEmptyBody_RunsNothing_ButTheGroupsAreStillFormed()
    {
        var harness = new GroupHarness(By("*", "@k", 10), "i=a i=b");

        await Assert.That(harness.Result()).IsEqualTo(string.Empty);
        await Assert.That(harness.Run.Writes).Contains("group:1");
    }

    [Test]
    public async Task Executable_ReportsTheSlotsOfTheSelect_TheKey_TheSortKeys_AndTheBody()
    {
        var harness = new GroupHarness(
            By("$a", "$b", 10, X("sort", 11, A("select", "$c")), X("value-of", 12, A("select", "$d"))),
            string.Empty,
            "xslt2",
            ["a", "b", "c", "d"]
        );

        var slots = ((IXsltSlotReader)harness.Executable!).ReferencedSlots.ToArray();

        await Assert.That(string.Join(",", slots)).IsEqualTo("0,1,2,3");
    }

    [Test]
    public async Task Executable_ReportsTheSlotsOfTheKeyAndOfThePattern()
    {
        var adjacent = new GroupHarness(Adjacent("*", "$b", 10, Members(11)), string.Empty, "xslt2", ["a", "b"]);
        var starting = new GroupHarness(
            Starting("*", "i[@k = $b]", 10, Members(11)),
            string.Empty,
            "xslt2",
            ["a", "b"]
        );

        await Assert.That(((IXsltSlotReader)adjacent.Executable!).ReferencedSlots.ToArray()).Contains(1);
        await Assert.That(((IXsltSlotReader)starting.Executable!).ReferencedSlots.ToArray()).Contains(1);
    }

    [Test]
    public async Task Executable_LocalVariablesOfTheBody_TakeTheSlotsAboveTheVisibleVariables()
    {
        var harness = new GroupHarness(
            By(
                "*",
                "@k",
                10,
                X("variable", 11, A("name", "n"), A("select", "count(current-group())")),
                X("value-of", 12, A("select", "concat($n, ';')"))
            ),
            "i=a i=a i=b",
            "xslt2",
            ["g"]
        );

        await Assert.That(((IXsltLocalSlots)harness.Executable!).SlotEnd).IsEqualTo(2);
        await Assert.That(harness.Result()).IsEqualTo("2;1;");
    }

    [Test]
    public async Task Compile_ACollationThatIsAnAttributeValueTemplate_IsTheUnsupportedConstructError()
    {
        var harness = new GroupHarness(
            Group(10, [("select", "*"), ("group-by", "@k"), ("collation", "{$c}")], Members(11))
        );

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo(Code(SchematronDiagnosticCodes.XsltUnsupported));
    }

    [Test]
    public async Task Compile_WithoutABindingInTheContext_IsAnInvalidOperation()
    {
        var node = Declaration(By("*", "@k", 10, Members(11)));
        var scope = new QueryStaticScope([], [], GroupHarness.Module, SchematronEdition.Iso2016);
        var context = new XsltCompileContext(node, scope, _ => null, new FakeProblemSink());

        await Assert.That(() => TryCompile(context)).Throws<InvalidOperationException>();
    }

    private static bool TryCompile(XsltCompileContext context) =>
        new XsltForEachGroupInstruction().TryCompile(context, out _, out _);
}
