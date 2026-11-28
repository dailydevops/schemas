namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Flow;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>Variables that the host declares with a slot are read from the slot array of the context, not from the name-keyed variables.</summary>
public class HostVariableSlotTests
{
    private static readonly XPathQName X = new(string.Empty, "x");
    private static readonly XPathQName Y = new(string.Empty, "y");

    private static XPathExpression Compile(string text) =>
        XPathExpression.Compile(
            text,
            XPathStaticContext.Default.ToBuilder().WithVariable(X, 0).WithVariable(Y, 1).Build()
        );

    private static XPathEvaluationContext Attach(params long?[] values)
    {
        var slots = new XdmSequence[values.Length];
        var bound = new bool[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            if (values[i] is { } value)
            {
                slots[i] = XdmSequence.Create(XdmItem.FromInteger(value));
                bound[i] = true;
            }
        }

        var context = new XPathEvaluationContext();
        context.Variables.AttachSlots(slots, bound);
        return context;
    }

    [Test]
    public async Task AReference_ReadsTheSlot()
    {
        using var context = Attach(3, 4);

        var result = Compile("$x * 10 + $y").TryEvaluate(context);

        await Assert.That(result.Status).IsEqualTo(XPathEvaluationStatus.Ok);
        await Assert.That(result.Value[0].TryGetInteger(out var number)).IsTrue();
        await Assert.That(number).IsEqualTo(34L);
    }

    [Test]
    public async Task AReference_SeesAValueStoredAfterTheAttachment()
    {
        var slots = new XdmSequence[1];
        var bound = new bool[1];
        using var context = new XPathEvaluationContext();
        context.Variables.AttachSlots(slots, bound);
        var expression = Compile("$x");
        slots[0] = XdmSequence.Create(XdmItem.FromInteger(5));
        bound[0] = true;

        var result = expression.TryEvaluate(context);

        await Assert.That(result.Value[0].TryGetInteger(out var number)).IsTrue();
        await Assert.That(number).IsEqualTo(5L);
    }

    [Test]
    public async Task AReferenceToAnUnboundSlot_RaisesXpdy0002()
    {
        using var context = Attach(1, null);

        var result = Compile("$y").TryEvaluate(context);

        await Assert.That(result.Diagnostic.Code).IsEqualTo("XPDY0002");
    }

    [Test]
    public async Task AReferenceBeyondTheSlotArray_RaisesXpdy0002()
    {
        using var context = Attach(1);

        var result = Compile("$y").TryEvaluate(context);

        await Assert.That(result.Diagnostic.Code).IsEqualTo("XPDY0002");
    }

    [Test]
    public async Task ARangeVariableOfTheSameName_ShadowsTheSlotOnlyInsideItsScope()
    {
        using var context = Attach(100);

        var result = Compile("(for $x in (1, 2) return $x), $x").TryEvaluate(context);

        await Assert.That(result.Status).IsEqualTo(XPathEvaluationStatus.Ok);
        await Assert.That(result.Value.Count).IsEqualTo(3);
        await Assert.That(result.Value[0].TryGetInteger(out var first)).IsTrue();
        await Assert.That(first).IsEqualTo(1L);
        await Assert.That(result.Value[2].TryGetInteger(out var last)).IsTrue();
        await Assert.That(last).IsEqualTo(100L);
    }

    [Test]
    public async Task AnInlineFunction_CapturesTheSlotValueWhenTheItemIsCreated()
    {
        const string text = "(function() { $x })()";
        _ = XPathParser.TryParse(text.AsSpan(), XPathVersion.V30, out var syntax, out _);
        var statics = XPathStaticContext.Default.ToBuilder().WithVersion(XPathVersion.V30).WithVariable(X, 0).Build();
        _ = ExpressionCompiler.TryCompile(
            syntax!,
            text,
            statics,
            XPathEngine.CreateRegistry(XPathEngine.AllSlots),
            out var compiled,
            out _
        );
        using var context = Attach(7);

        var result = compiled!.Evaluate(context);

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].TryGetInteger(out var number)).IsTrue();
        await Assert.That(number).IsEqualTo(7L);
    }
}
