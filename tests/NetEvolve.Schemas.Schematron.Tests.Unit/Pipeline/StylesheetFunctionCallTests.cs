namespace NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline;

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Testing.Fakes;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline.PipelineFixture;

/// <summary>
/// A stylesheet function called from the XPath expressions of a rule, a variable, and another function body (#628, design #150 EP-13):
/// the real XPath evaluator, the real function table, and the public builder, with the functions in an imported module and in the schema.
/// </summary>
public class StylesheetFunctionCallTests
{
    private const string Xsl = "xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\"";
    private const string Ns =
        "<ns prefix=\"f\" uri=\"urn:f\"/><ns prefix=\"xs\" uri=\"http://www.w3.org/2001/XMLSchema\"/>";

    private const string LenModule =
        "<xsl:function name=\"f:len\" as=\"xs:integer\"><xsl:param name=\"s\" as=\"xs:string\"/><xsl:sequence select=\"string-length($s)\"/></xsl:function>";

    // GS1 GLN: thirteen digits, the first twelve weighted 1 and 3 from the left, the check digit is (10 - sum mod 10) mod 10.
    private const string GlnModule =
        "<xsl:function name=\"f:gln-sum\" as=\"xs:integer\"><xsl:param name=\"s\" as=\"xs:string\"/><xsl:sequence select=\"f:gln-sum($s, 1)\"/></xsl:function>"
        + "<xsl:function name=\"f:gln-sum\" as=\"xs:integer\"><xsl:param name=\"s\" as=\"xs:string\"/><xsl:param name=\"i\" as=\"xs:integer\"/>"
        + "<xsl:sequence select=\"if ($i &gt; 12) then 0 else xs:integer(substring($s, $i, 1)) * (if ($i mod 2 = 1) then 1 else 3) + f:gln-sum($s, $i + 1)\"/></xsl:function>"
        + "<xsl:function name=\"f:is-gln\" as=\"xs:boolean\"><xsl:param name=\"gln\" as=\"xs:string\"/>"
        + "<xsl:variable name=\"digits\" select=\"string-length($gln) = 13 and translate($gln, '0123456789', '') = ''\"/>"
        + "<xsl:sequence select=\"if ($digits) then (10 - f:gln-sum($gln) mod 10) mod 10 = xs:integer(substring($gln, 13, 1)) else false()\"/></xsl:function>";

    // A modulus 11 check digit: the payload weighted 2 to 7 from the right, the last digit is (11 - sum mod 11) mod 11; 10 is invalid.
    private const string Mod11Module =
        "<xsl:function name=\"f:mod11-sum\" as=\"xs:integer\"><xsl:param name=\"s\" as=\"xs:string\"/><xsl:param name=\"pos\" as=\"xs:integer\"/><xsl:param name=\"w\" as=\"xs:integer\"/>"
        + "<xsl:sequence select=\"if ($pos &lt; 1) then 0 else xs:integer(substring($s, $pos, 1)) * $w + f:mod11-sum($s, $pos - 1, if ($w = 7) then 2 else $w + 1)\"/></xsl:function>"
        + "<xsl:function name=\"f:mod11-valid\" as=\"xs:boolean\"><xsl:param name=\"s\" as=\"xs:string\"/>"
        + "<xsl:variable name=\"n\" as=\"xs:integer\" select=\"string-length($s)\"/>"
        + "<xsl:variable name=\"numeric\" select=\"$n &gt;= 2 and translate($s, '0123456789', '') = ''\"/>"
        + "<xsl:sequence select=\"if ($numeric) then (11 - f:mod11-sum($s, $n - 1, 2) mod 11) mod 11 = xs:integer(substring($s, $n, 1)) else false()\"/></xsl:function>";

    private static string Sheet(string body) =>
        $"<xsl:stylesheet version=\"2.0\" {Xsl} xmlns:f=\"urn:f\" xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">{body}</xsl:stylesheet>";

    private static string Fn(string name, string select, string parameters = "x") =>
        $"<xsl:function name=\"f:{name}\">"
        + string.Concat(
            parameters
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(parameter => $"<xsl:param name=\"{parameter}\"/>")
        )
        + $"<xsl:sequence select=\"{select}\"/></xsl:function>";

    private static string Rule(string test) =>
        $"<pattern id=\"p\"><rule context=\"a\"><assert test=\"{test}\">Failed.</assert></rule></pattern>";

    private static string Principal(string body, string binding = "xslt2") =>
        Wrap(Ns + body, $" queryBinding=\"{binding}\" schematronEdition=\"2025\" {Xsl} xmlns:f=\"urn:f\"");

    private static ISchematronValidator FromModule(string module, string test, string binding = "xslt2") =>
        Builder(Principal($"<xsl:import href=\"lib.xsl\"/>{Rule(test)}", binding))
            .WithResolver(new InMemoryResourceResolver().Add("file:///lib.xsl", Encoding.UTF8.GetBytes(Sheet(module))))
            .BuildNow();

    private static ISchematronValidator Inline(string declarations, string test, string binding = "xslt2") =>
        Build(Principal(declarations + Rule(test), binding));

    private static string Messages(ValidationResult result) =>
        string.Join("|", result.Issues.Select(issue => issue.Message));

    [Test]
    [Arguments("xslt2")]
    [Arguments("xslt3")]
    [Arguments("xslt")]
    public async Task Call_OfAnImportedFunction_DecidesTheAssertion(string binding)
    {
        var validator = FromModule(Fn("twice", "$x * 2"), "f:twice(number(.)) = 10", binding);

        var valid = Validate(validator, "<a>5</a>");
        var invalid = Validate(validator, "<a>4</a>");

        await Assert.That(valid.IsValid).IsTrue();
        await Assert.That(invalid.IsValid).IsFalse();
        await Assert.That(Messages(invalid)).IsEqualTo("Failed.");
    }

    [Test]
    public async Task Call_OfAFunctionDeclaredInTheSchema_DecidesTheAssertion()
    {
        var validator = Inline(Fn("twice", "$x * 2"), "f:twice(number(.)) = 10");

        await Assert.That(Validate(validator, "<a>5</a>").IsValid).IsTrue();
        await Assert.That(Validate(validator, "<a>6</a>").IsValid).IsFalse();
    }

    [Test]
    public async Task Call_WithAnArityThatNoFunctionDeclares_IsXPST0017()
    {
        var failure = Capture(() => _ = Inline(Fn("twice", "$x * 2"), "f:twice(1, 2) = 4"));

        await Assert.That(failure.Issues.Single().Constraint).IsEqualTo("XPST0017");
    }

    [Test]
    public async Task Call_OfAFunctionThatNobodyDeclares_IsXPST0017()
    {
        var failure = Capture(() => _ = Inline(Fn("twice", "$x * 2"), "f:thrice(1) = 3"));

        await Assert.That(failure.Issues.Single().Constraint).IsEqualTo("XPST0017");
    }

    [Test]
    public async Task Call_OfAFunctionWhoseNamespaceOnlyTheSchemaDeclares_Resolves()
    {
        var schema = Wrap(
            Ns
                + $"<xsl:function name=\"f:twice\" {Xsl}><xsl:param name=\"x\"/><xsl:sequence select=\"$x * 2\"/></xsl:function>"
                + Rule("f:twice(number(.)) = 10"),
            " queryBinding=\"xslt2\" schematronEdition=\"2025\""
        );

        var validator = Build(schema);

        await Assert.That(Validate(validator, "<a>5</a>").IsValid).IsTrue();
        await Assert.That(Validate(validator, "<a>4</a>").IsValid).IsFalse();
    }

    [Test]
    public async Task Declaration_WithoutAPrefix_IsReportedAndNoCallCompiles()
    {
        var failure = Capture(() =>
            _ = Inline(
                $"<xsl:function name=\"twice\"><xsl:param name=\"x\"/><xsl:sequence select=\"$x\"/></xsl:function>",
                "f:twice(1) = 1"
            )
        );

        await Assert.That(failure.Issues.Select(issue => issue.Constraint)).Contains("XTSE0740");
    }

    [Test]
    public async Task Call_OfAFunctionWithTwoArities_ResolvesByArity()
    {
        var validator = Inline(
            Fn("sum", "$a + 1", "a") + Fn("sum", "$a + $b", "a,b"),
            "f:sum(1) = 2 and f:sum(1, 5) = 6"
        );

        await Assert.That(Validate(validator, "<a/>").IsValid).IsTrue();
    }

    [Test]
    public async Task Call_ThatIsNestedAndRecursive_KeepsTheValuesOfEveryActivation()
    {
        var validator = Inline(
            Fn("fact", "if ($n le 1) then 1 else $n * f:fact($n - 1)", "n") + Fn("twice", "$x * 2"),
            "f:fact(5) = 120 and f:twice(f:twice(f:twice(1))) = 8 and f:fact(f:twice(2)) = 24"
        );

        await Assert.That(Validate(validator, "<a/>").IsValid).IsTrue();
    }

    [Test]
    public async Task Call_FromAnotherFunctionBody_ReadsAGlobalVariable()
    {
        var validator = Inline(
            "<xsl:variable name=\"offset\" select=\"100\"/>"
                + Fn("shift", "$x + $offset")
                + Fn("twice", "f:shift($x) * 2"),
            "f:twice(1) = 202"
        );

        await Assert.That(Validate(validator, "<a/>").IsValid).IsTrue();
    }

    [Test]
    public async Task Call_FromAGlobalVariableAndFromALetVariable_IsEvaluated()
    {
        var schema = Principal(
            Fn("twice", "$x * 2")
                + "<xsl:variable name=\"g\" select=\"f:twice(4)\"/>"
                + "<pattern id=\"p\"><rule context=\"a\"><let name=\"l\" value=\"f:twice($g)\"/><assert test=\"$l = 16\">Failed.</assert></rule></pattern>"
        );

        await Assert.That(Validate(Build(schema), "<a/>").IsValid).IsTrue();
    }

    [Test]
    public async Task Call_ArgumentsAreEvaluatedAgainstTheFocusOfTheCaller()
    {
        var validator = Inline(Fn("name-of", "name($n)", "n"), "f:name-of(.) = 'a' and f:name-of(b) = 'b'");

        await Assert.That(Validate(validator, "<a><b/></a>").IsValid).IsTrue();
    }

    [Test]
    public async Task Call_ConvertsTheArgumentAndTheResult_ByTheDeclaredTypes()
    {
        var validator = FromModule(LenModule, "f:len(string(.)) = 3");

        await Assert.That(Validate(validator, "<a>abc</a>").IsValid).IsTrue();
        await Assert.That(Validate(validator, "<a>abcd</a>").IsValid).IsFalse();
    }

    [Test]
    public async Task Call_WithAnArgumentOfTheWrongType_FailsWithTheCodeOfTheFunction()
    {
        var validator = FromModule(LenModule, "f:len(1) = 1");

        var issue = Validate(validator, "<a/>").Issues.Single();

        await Assert.That(issue.Constraint).IsEqualTo("XPTY0004");
        await Assert.That(issue.Message).Contains("f:len");
    }

    [Test]
    public async Task Call_ThatFailsInTheBody_ReportsTheProblemOfTheCallee()
    {
        var validator = FromModule(Fn("boom", "$x idiv 0"), "f:boom(1) = 1");

        var issue = Validate(validator, "<a/>").Issues.Single();

        await Assert.That(issue.Constraint).IsEqualTo("FOAR0001");
    }

    [Test]
    public async Task Call_ThatFailsInANestedCall_ReportsTheInnermostProblem()
    {
        var validator = FromModule(Fn("boom", "$x idiv 0") + Fn("outer", "f:boom($x) + 1"), "f:outer(1) = 1");

        var issue = Validate(validator, "<a/>").Issues.Single();

        await Assert.That(issue.Constraint).IsEqualTo("FOAR0001");
    }

    [Test]
    public async Task Body_HasNoFocusAndNoCurrentItem()
    {
        var focus = Inline(Fn("no-focus", "name(.)"), "f:no-focus(1) = 'a'");
        var current = Inline(Fn("no-current", "name(current())"), "f:no-current(1) = 'a'");

        var focusIssue = Validate(focus, "<a/>").Issues.Single();
        var currentIssue = Validate(current, "<a/>").Issues.Single();

        await Assert.That(focusIssue.Constraint).IsEqualTo("XPDY0002");
        await Assert.That(currentIssue.Constraint).IsEqualTo("XPDY0002");
    }

    [Test]
    public async Task Call_FromAPredicate_KeepsTheFocusOfThePath()
    {
        var validator = Inline(Fn("big", "number($n) > 1", "n"), "count(b[f:big(.)]) = 2 and b[f:big(.)][1] = '2'");

        await Assert.That(Validate(validator, "<a><b>1</b><b>2</b><b>3</b></a>").IsValid).IsTrue();
    }

    [Test]
    public async Task Call_AtTheLimitOfNestedCalls_IsAProblemAndNotACrash()
    {
        var validator = Inline(Fn("loop", "f:loop($x + 1)"), "f:loop(1) = 1");

        var issue = Validate(validator, "<a/>").Issues.Single();

        await Assert.That(issue.Code).IsEqualTo(SchematronDiagnosticCodes.EvaluationLimitStop);
    }

    [Test]
    public async Task Run_OfAFunctionSchema_CanBeRepeatedOnOneValidator()
    {
        var validator = Inline(Fn("twice", "$x * 2"), "f:twice(number(.)) = 10");

        var first = Validate(validator, "<a>5</a>");
        var second = Validate(validator, "<a>4</a>");
        var third = Validate(validator, "<a>5</a>");

        await Assert.That(first.IsValid).IsTrue();
        await Assert.That(second.IsValid).IsFalse();
        await Assert.That(third.IsValid).IsTrue();
    }

    [Test]
    [Arguments("4012345678901", true)]
    [Arguments("0000000000000", true)]
    [Arguments("4000000000006", true)]
    [Arguments("4012345678902", false)]
    [Arguments("4012345678900", false)]
    [Arguments("401234567890", false)]
    [Arguments("40123456789012", false)]
    [Arguments("40123456789O1", false)]
    [Arguments("", false)]
    public async Task Gln_ValidAndInvalidNumbers_AreDecidedByTheRule(string number, bool expected)
    {
        var validator = FromModule(GlnModule, "f:is-gln(string(.))");

        var result = Validate(validator, $"<a>{number}</a>");

        await Assert.That(result.IsValid).IsEqualTo(expected);
    }

    [Test]
    [Arguments("123455", true)]
    [Arguments("00", true)]
    [Arguments("123454", false)]
    [Arguments("12345A", false)]
    [Arguments("5", false)]
    [Arguments("", false)]
    [Arguments("100", false)]
    public async Task Mod11_ValidAndInvalidNumbers_AreDecidedByTheRule(string number, bool expected)
    {
        var validator = FromModule(Mod11Module, "f:mod11-valid(string(.))");

        var result = Validate(validator, $"<a>{number}</a>");

        await Assert.That(result.IsValid).IsEqualTo(expected);
    }

    // A local variable with a sequence constructor in a function body (#629), the shape of the BIS 3 helper functions.
    private const string ContentVariableModule =
        "<xsl:function name=\"f:digits\" as=\"xs:string\"><xsl:param name=\"s\" as=\"xs:string\"/>"
        + "<xsl:variable name=\"d\" as=\"xs:string\"><xsl:sequence select=\"translate($s, 'ABC ', '')\"/></xsl:variable>"
        + "<xsl:sequence select=\"$d\"/></xsl:function>"
        + "<xsl:function name=\"f:tree-text\" as=\"xs:string\"><xsl:param name=\"s\" as=\"xs:string\"/>"
        + "<xsl:variable name=\"t\"><e><xsl:value-of select=\"$s\"/></e></xsl:variable>"
        + "<xsl:sequence select=\"string($t/e)\"/></xsl:function>"
        + "<xsl:function name=\"f:typed\" as=\"xs:integer\"><xsl:param name=\"s\" as=\"xs:string\"/>"
        + "<xsl:variable name=\"n\" as=\"xs:integer\"><xsl:sequence select=\"$s\"/></xsl:variable>"
        + "<xsl:sequence select=\"$n\"/></xsl:function>";

    [Test]
    [Arguments("f:digits(string(.)) = '12'", "AB12 C", true)]
    [Arguments("f:digits(string(.)) = '12'", "13", false)]
    [Arguments("f:tree-text(string(.)) = 'x'", "x", true)]
    [Arguments("f:tree-text(string(.)) = 'x'", "y", false)]
    public async Task Call_OfAFunctionWithAContentVariable_DecidesTheAssertion(string test, string text, bool expected)
    {
        var validator = FromModule(ContentVariableModule, test);

        var result = Validate(validator, $"<a>{text}</a>");

        await Assert.That(result.IsValid).IsEqualTo(expected);
    }

    [Test]
    public async Task Call_OfAFunctionWithAContentVariableOfTheWrongType_ReportsTheTypeError()
    {
        var validator = FromModule(ContentVariableModule, "f:typed(string(.)) = 1");

        var result = Validate(validator, "<a>abc</a>");

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(Messages(result)).Contains("XTTE0570");
    }

    [Test]
    [Arguments("<a>380</a>", true)]
    [Arguments("<a>999</a>", false)]
    public async Task Call_OfAFunctionWithAPrefixedLocalVariable_ResolvesTheVariable(string instance, bool expected)
    {
        const string module =
            "<xsl:function name=\"f:is-code\" as=\"xs:boolean\"><xsl:param name=\"code\" as=\"xs:string?\"/>"
            + "<xsl:variable name=\"f:codes\" as=\"xs:string\" select=\"'380 389'\"/>"
            + "<xsl:sequence select=\"$code = tokenize($f:codes, ' ')\"/></xsl:function>";
        var validator = Inline(module, "f:is-code(string(.))");

        await Assert.That(Validate(validator, instance).IsValid).IsEqualTo(expected);
    }
}
