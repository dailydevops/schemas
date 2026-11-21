namespace NetEvolve.Schemas.XPath.Tests.Unit.Api;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Values;

public class XPathExpressionEvaluateTests
{
    private const string Document = "<r><a>1</a><a>2</a><b x=\"y\">text</b></r>";

    private static readonly XPathQName X = new(string.Empty, "x");

    [Test]
    [Arguments("1 + 2", "3")]
    [Arguments("'a'", "a")]
    [Arguments("(1, 2, 3)", "1|2|3")]
    [Arguments("1 to 3", "1|2|3")]
    [Arguments("if (1 lt 2) then 'yes' else 'no'", "yes")]
    [Arguments("for $i in (1, 2) return $i * 10", "10|20")]
    [Arguments("some $i in (1, 2) satisfies $i = 2", "true")]
    [Arguments("1 eq 1 and 2 gt 1", "true")]
    [Arguments("xs:integer('5') + 1", "6")]
    [Arguments("'5' cast as xs:integer", "5")]
    [Arguments("5 instance of xs:integer", "true")]
    [Arguments("count((1, 2, 3))", "3")]
    [Arguments("fn:string-length('abc')", "3")]
    [Arguments("concat('a', 'b', 'c')", "abc")]
    [Arguments("true()", "true")]
    [Arguments("xs:date('2024-01-02') + xs:dayTimeDuration('P1D') = xs:date('2024-01-03')", "true")]
    public async Task Evaluate_Expression_GivesTheValue(string expression, string expected) =>
        await Assert.That(ApiFixture.Run(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("count(/r/a)", "2")]
    [Arguments("/r/a", "1|2")]
    [Arguments("string(/r/b/@x)", "y")]
    [Arguments("/r/a[2]", "2")]
    [Arguments("/r/a | /r/b", "1|2|text")]
    [Arguments("/r/b/text()", "text")]
    public async Task Evaluate_PathOverTheContextNode_GivesTheValue(string expression, string expected) =>
        await Assert.That(ApiFixture.Run(expression, Document)).IsEqualTo(expected);

    [Test]
    public async Task Evaluate_ContextItem_IsTheItemSetOnTheContext()
    {
        var expression = ApiFixture.Compile(". * 2");
        using var context = new XPathEvaluationContext();
        context.SetContextItem(XdmItem.FromInteger(21));

        await Assert.That(ApiFixture.Render(expression.Evaluate(context))).IsEqualTo("42");
    }

    [Test]
    public async Task Evaluate_Variable_IsTheValueBoundOnTheContext()
    {
        var expression = ApiFixture.Compile("$x * 2", ApiFixture.Builder().WithVariable(X).Build());
        using var context = new XPathEvaluationContext();
        context.Variables.Set(X, XdmItem.FromInteger(5));
        var first = ApiFixture.Render(expression.Evaluate(context));
        context.Variables.Set(X, XdmItem.FromInteger(7));

        await Assert.That(first).IsEqualTo("10");
        await Assert.That(ApiFixture.Render(expression.Evaluate(context))).IsEqualTo("14");
    }

    [Test]
    public async Task TryEvaluate_Success_ReportsOkWithoutADiagnostic()
    {
        var expression = ApiFixture.Compile("1 + 2");
        using var context = new XPathEvaluationContext();

        var result = expression.TryEvaluate(context);

        await Assert.That(result.Status).IsEqualTo(XPathEvaluationStatus.Ok);
        await Assert.That(result.Diagnostic == default).IsTrue();
        await Assert.That(ApiFixture.Render(result.Value)).IsEqualTo("3");
    }

    [Test]
    public async Task TryEvaluate_UnboundVariable_ReportsXpdy0002AndNeverThrows()
    {
        var expression = ApiFixture.Compile("$x", ApiFixture.Builder().WithVariable(X).Build());
        using var context = new XPathEvaluationContext();

        var result = expression.TryEvaluate(context);

        await Assert.That(result.Status).IsEqualTo(XPathEvaluationStatus.Error);
        await Assert.That(result.Diagnostic.Code).IsEqualTo("XPDY0002");
        await Assert.That(result.Diagnostic.Kind).IsEqualTo(XPathErrorKind.Dynamic);
        await Assert.That(result.Value.IsEmpty).IsTrue();
    }

    [Test]
    public async Task TryEvaluate_DynamicErrorWithoutALocation_ReportsTheCodeAndAnUnknownOffset()
    {
        var expression = ApiFixture.Compile("1 idiv 0");
        using var context = new XPathEvaluationContext();

        var result = expression.TryEvaluate(context);

        await Assert.That(result.Status).IsEqualTo(XPathEvaluationStatus.Error);
        await Assert.That(result.Diagnostic.Code).IsEqualTo("FOAR0001");
        await Assert.That(result.Diagnostic.Kind).IsEqualTo(XPathErrorKind.Function);
        await Assert.That(result.Diagnostic.Offset).IsEqualTo(-1);
    }

    [Test]
    public async Task TryEvaluate_DynamicErrorWithALocation_ReportsTheCodeAndTheLocation()
    {
        var expression = ApiFixture.Compile("1 + ('x' cast as xs:integer)");
        using var context = new XPathEvaluationContext();

        var result = expression.TryEvaluate(context);

        await Assert.That(result.Status).IsEqualTo(XPathEvaluationStatus.Error);
        await Assert.That(result.Diagnostic.Code).IsEqualTo("FORG0001");
        await Assert.That(result.Diagnostic.Offset).IsGreaterThanOrEqualTo(0);
        await Assert.That(result.Diagnostic.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task TryEvaluate_TreatMismatch_ReportsXpdy0050WithTheLocationOfTheExpression()
    {
        var expression = ApiFixture.Compile("$x treat as xs:string", ApiFixture.Builder().WithVariable(X).Build());
        using var context = new XPathEvaluationContext();
        context.Variables.Set(X, XdmItem.FromInteger(5));

        var result = expression.TryEvaluate(context);

        await Assert.That(result.Diagnostic.Code).IsEqualTo("XPDY0050");
        await Assert.That(result.Diagnostic.Offset).IsEqualTo(0);
        await Assert.That(result.Diagnostic.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task Evaluate_DynamicError_ThrowsXPathExceptionWithTheDiagnostic()
    {
        var expression = ApiFixture.Compile("1 idiv 0");
        using var context = new XPathEvaluationContext();

        var failure = Assert.Throws<XPathException>(() => expression.Evaluate(context));

        await Assert.That(failure.Diagnostic.Code).IsEqualTo("FOAR0001");
    }

    [Test]
    public async Task Evaluate_NullContext_IsAProgrammingError()
    {
        var expression = ApiFixture.Compile("1");

        await Assert.That(() => expression.Evaluate(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => expression.TryEvaluate(null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Evaluate_AfterAnError_TheContextEvaluatesAgain()
    {
        var failing = ApiFixture.Compile("1 idiv 0");
        var working = ApiFixture.Compile("1 + 1");
        using var context = new XPathEvaluationContext();

        _ = failing.TryEvaluate(context);

        await Assert.That(ApiFixture.Render(working.Evaluate(context))).IsEqualTo("2");
    }

    [Test]
    public async Task Run_SyntaxError_IsReportedByTheHelperAsTheCode() =>
        await Assert.That(ApiFixture.Run("1 +")).IsEqualTo("error:XPST0003");
}
