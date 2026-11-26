namespace NetEvolve.Schemas.XPath.Tests.Unit.Api;

using System;
using System.Text;
using System.Threading.Tasks;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Atomization;
using NetEvolve.Schemas.XPath.Limits;
using NetEvolve.Schemas.XPath.Xdm.Values;

public class XPathExpressionEvaluateBooleanTests
{
    private const string Document = "<r><a>1</a><a>2</a><a/><b x=\"y\">text</b><c>0</c></r>";

    private static readonly XPathQName X = new(string.Empty, "x");

    // Representative expressions of every node kind, including the effective boolean value errors (FORG0006).
    private static readonly string[] Expressions =
    [
        "true()",
        "false()",
        "()",
        "1",
        "0",
        "'a'",
        "''",
        "'0'",
        "xs:double('NaN')",
        "(1, 2)",
        "(1, /r/a)",
        "(/r/a, 1)",
        "xs:date('2020-01-01')",
        "/r/a",
        "/r/zzz",
        "/r/a[. = 2]",
        "/r/a[. = 9]",
        "//a",
        "//zzz",
        ".//a",
        "/r/b/@x",
        "/r/a/text()",
        "/r/a/string()",
        "/r/a/number()",
        "/r/c",
        "string(/r/c)",
        "/r/a = 2",
        "/r/a = 9",
        "/r/zzz = 1",
        "(1, 'a') = 1",
        "('a', 1) = 1",
        "/r/a != 1",
        "/r/a eq 1",
        "/r/a[1] eq '1'",
        "/r/a[1] eq 1",
        "/r/zzz eq 1",
        "1 eq ()",
        "(1, 2) eq 1",
        "count(/r/a) > 2",
        "/r/a and /r/b",
        "/r/zzz and /r/b",
        "/r/zzz or /r/b",
        "/r/zzz or /r/zzz",
        "/r/a or xs:date('2020-01-01')",
        "/r/zzz or xs:date('2020-01-01')",
        "xs:date('2020-01-01') and false()",
        "not(/r/a)",
        "if (/r/a) then /r/b else ()",
        "if (/r/zzz) then 1 else 0",
        "if (/r/zzz) then () else (1, 2)",
        "if ((1, 2)) then 1 else 0",
        "some $a in /r/a satisfies $a = 2",
        "every $a in /r/a satisfies $a = 2",
        "some $a in /r/a satisfies $a",
        "every $a in /r/a satisfies string($a)",
        "some $a in (1, 2) satisfies (1, 2)",
        "every $a in () satisfies (1, 2)",
        "for $a in /r/a return $a",
        "for $a in /r/zzz return $a",
        "$x",
        "$x = 5",
        "$x and /r/b",
        ". = 1",
        "/r/a[position() = last()]",
        "/r/a[last()]",
    ];

    private static XPathExpression Compile(string text) =>
        ApiFixture.Compile(text, ApiFixture.Builder().WithVariable(X).Build());

    private static XPathEvaluationContext CreateContext()
    {
        var context = new XPathEvaluationContext();
        context.SetContextItem(XdmItem.FromNode(ApiFixture.Load(Document)));
        context.Variables.Set(X, XdmItem.FromInteger(5));
        return context;
    }

    // The outcome of the specified route: Evaluate, then the effective boolean value.
    private static string Reference(XPathExpression expression, XPathEvaluationContext context)
    {
        try
        {
            return XPathAtomization.EffectiveBooleanValue(expression.Evaluate(context)) ? "true" : "false";
        }
        catch (XPathException failure)
        {
            return "error:" + failure.Diagnostic.Code;
        }
    }

    private static string Actual(XPathExpression expression, XPathEvaluationContext context)
    {
        try
        {
            return expression.EvaluateBoolean(context) ? "true" : "false";
        }
        catch (XPathException failure)
        {
            return "error:" + failure.Diagnostic.Code;
        }
    }

    private static string Pick(Random random) => Expressions[random.Next(Expressions.Length)];

    [Test]
    public async Task EvaluateBoolean_EveryRepresentativeExpression_EqualsTheEffectiveBooleanValueOfEvaluate()
    {
        using var context = CreateContext();
        var sawTrue = false;
        var sawFalse = false;
        var sawError = false;

        foreach (var text in Expressions)
        {
            var expression = Compile(text);
            var expected = Reference(expression, context);

            await Assert.That(Actual(expression, context)).IsEqualTo(expected);

            sawTrue |= expected == "true";
            sawFalse |= expected == "false";
            sawError |= expected.StartsWith("error:FORG0006", StringComparison.Ordinal);
        }

        await Assert.That(sawTrue && sawFalse && sawError).IsTrue();
    }

    [Test]
    public async Task EvaluateBoolean_RandomizedCompositions_EqualTheEffectiveBooleanValueOfEvaluate()
    {
        var random = new Random(1254);
        var statics = ApiFixture.Builder().WithVariable(X).Build();
        using var context = CreateContext();

        for (var i = 0; i < 400; i++)
        {
            var text = random.Next(5) switch
            {
                0 => $"({Pick(random)}) and ({Pick(random)})",
                1 => $"({Pick(random)}) or ({Pick(random)})",
                2 => $"if ({Pick(random)}) then ({Pick(random)}) else ({Pick(random)})",
                3 => $"some $v in ({Pick(random)}) satisfies ({Pick(random)})",
                _ => $"({Pick(random)}) = ({Pick(random)})",
            };
            var compiled = XPathExpression.TryCompile(text, statics);
            if (!compiled.Success)
            {
                continue;
            }

            var expression = compiled.Expression!;

            await Assert.That(Actual(expression, context)).IsEqualTo(Reference(expression, context));
        }
    }

    [Test]
    public async Task EvaluateBoolean_ContextItemAbsent_RaisesXpdy0002LikeEvaluate()
    {
        var expression = Compile("/r/a");
        using var context = new XPathEvaluationContext();

        var failure = Assert.Throws<XPathException>(() => expression.EvaluateBoolean(context));

        await Assert.That(failure.Diagnostic.Code).IsEqualTo("XPDY0002");
    }

    [Test]
    public async Task EvaluateBoolean_UnboundVariable_RaisesXpdy0002LikeEvaluate()
    {
        var expression = Compile("$x");
        using var context = new XPathEvaluationContext();

        var failure = Assert.Throws<XPathException>(() => expression.EvaluateBoolean(context));

        await Assert.That(failure.Diagnostic.Code).IsEqualTo("XPDY0002");
    }

    [Test]
    public async Task EvaluateBoolean_ContextItemOfTheEvaluation_DecidesTheValue()
    {
        using var context = new XPathEvaluationContext();
        context.SetContextItem(XdmItem.FromNode(ApiFixture.Load("<r><a>2</a></r>")));
        var root = context.ContextItem!.Value;

        await Assert.That(Compile("/r/a = 2").EvaluateBoolean(context)).IsTrue();
        await Assert.That(Compile("r/a = 2").EvaluateBoolean(context)).IsTrue();
        await Assert.That(Compile("r/a = 3").EvaluateBoolean(context)).IsFalse();

        context.SetContextItem(root);

        await Assert.That(Compile("r/zzz").EvaluateBoolean(context)).IsFalse();
    }

    [Test]
    public async Task EvaluateBoolean_VariablesRebound_FollowTheBinding()
    {
        var expression = Compile("$x = 5");
        using var context = CreateContext();

        var bound = expression.EvaluateBoolean(context);
        context.Variables.Set(X, XdmItem.FromInteger(6));
        var rebound = expression.EvaluateBoolean(context);

        await Assert.That(bound).IsTrue();
        await Assert.That(rebound).IsFalse();
    }

    [Test]
    public async Task TryEvaluateBoolean_Success_ReturnsOkAndTheValue()
    {
        var expression = Compile("/r/a = 2");
        using var context = CreateContext();

        var status = expression.TryEvaluateBoolean(context, out var value, out var diagnostic);

        await Assert.That(status).IsEqualTo(XPathEvaluationStatus.Ok);
        await Assert.That(value).IsTrue();
        await Assert.That(diagnostic == default).IsTrue();
    }

    [Test]
    public async Task TryEvaluateBoolean_NoEffectiveBooleanValue_ReportsForg0006AndNeverThrows()
    {
        var expression = Compile("(1, 2)");
        using var context = CreateContext();

        var status = expression.TryEvaluateBoolean(context, out var value, out var diagnostic);

        await Assert.That(status).IsEqualTo(XPathEvaluationStatus.Error);
        await Assert.That(value).IsFalse();
        await Assert.That(diagnostic.Code).IsEqualTo("FORG0006");
    }

    [Test]
    public async Task TryEvaluateBoolean_DynamicError_ReportsTheDiagnosticOfTryEvaluate()
    {
        var expression = Compile("1 + ('x' cast as xs:integer) = 1");
        using var context = CreateContext();

        var expected = expression.TryEvaluate(context).Diagnostic;
        var status = expression.TryEvaluateBoolean(context, out var value, out var diagnostic);

        await Assert.That(status).IsEqualTo(XPathEvaluationStatus.Error);
        await Assert.That(value).IsFalse();
        await Assert.That(diagnostic == expected).IsTrue();
    }

    [Test]
    public async Task EvaluateBoolean_NullAndDisposedContext_FollowTheExistingMembers()
    {
        var expression = Compile("1");
        var disposed = new XPathEvaluationContext();
        disposed.Dispose();

        await Assert.That(() => expression.EvaluateBoolean(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => expression.TryEvaluateBoolean(null!, out _, out _)).Throws<ArgumentNullException>();
        await Assert.That(() => expression.EvaluateBoolean(disposed)).Throws<ObjectDisposedException>();
        await Assert
            .That(() => expression.TryEvaluateBoolean(disposed, out _, out _))
            .Throws<ObjectDisposedException>();
    }

    [Test]
    public async Task EvaluateBoolean_TreeDeeperThanTheLimit_RaisesTheDepthErrorOfEvaluate()
    {
        var expression = Compile("(((1 and 1) and 1) and 1) and 1");
        using var context = CreateContext();
        context.Limits = XPathLimits.Default.WithMaxRecursionDepth(2);

        var expected = Assert.Throws<XPathException>(() => expression.Evaluate(context)).Diagnostic;
        var actual = Assert.Throws<XPathException>(() => expression.EvaluateBoolean(context)).Diagnostic;
        var status = expression.TryEvaluateBoolean(context, out _, out var diagnostic);

        await Assert.That(actual == expected).IsTrue();
        await Assert.That(status).IsEqualTo(XPathEvaluationStatus.Error);
        await Assert.That(diagnostic == expected).IsTrue();
    }

    [Test]
    public async Task EvaluateBoolean_PathOverManyNodes_StopsAtTheFirstNodeAndAllocatesNothingPerNode()
    {
        var xml = new StringBuilder("<r>");
        for (var i = 0; i < 500; i++)
        {
            _ = xml.Append("<a>x</a>");
        }

        _ = xml.Append("</r>");
        var expression = Compile("/r/a");
        using var context = new XPathEvaluationContext();
        context.SetContextItem(XdmItem.FromNode(ApiFixture.Load(xml.ToString())));

        var evaluate = AllocationAssert.Measure(() =>
            _ = XPathAtomization.EffectiveBooleanValue(expression.Evaluate(context))
        );

        // A result array of 500 items costs well over 10 KiB; one node needs none.
        await Assert.That(evaluate).IsGreaterThan(10_000);
        AllocationAssert.AtMost(1_024, () => _ = expression.EvaluateBoolean(context));
        await Assert.That(expression.EvaluateBoolean(context)).IsTrue();
    }
}
