namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Flow;

using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Limits;
using static FlowFixture;

/// <summary>The comma operator and the empty sequence (XPath 2.0, 3.3.1): operands are concatenated into one flat sequence, in order.</summary>
public class SequenceTests
{
    [Test]
    [Arguments("()", "()")]
    [Arguments("(1)", "integer:1")]
    [Arguments("(1, 2, 3)", "integer:1, integer:2, integer:3")]
    [Arguments("(3, 1, 2)", "integer:3, integer:1, integer:2")]
    [Arguments("((1, 2), (3, ()))", "integer:1, integer:2, integer:3")]
    [Arguments("(1, ())", "integer:1")]
    [Arguments("((), ())", "()")]
    [Arguments("(1, 'a', 2.5)", "integer:1, string:a, decimal:2.5")]
    [Arguments("($numbers, 4)", "integer:1, integer:2, integer:3, integer:4")]
    [Arguments("(1 to 3, 7)", "integer:1, integer:2, integer:3, integer:7")]
    [Arguments("1, 2", "integer:1, integer:2")]
    [Arguments("(0, 1 to 2, 3)", "integer:0, integer:1, integer:2, integer:3")]
    public async Task Concatenates_TheOperandsInOrder(string expression, string expected) =>
        await Assert.That(Run(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("(1, tick(), 3)", 1)]
    [Arguments("(tick(), tick(), tick())", 3)]
    public async Task EvaluatesEveryOperandOnce(string expression, int calls)
    {
        var probe = new Probe();

        _ = Run(expression, probe);

        await Assert.That(probe.First).IsEqualTo(calls);
    }

    [Test]
    public async Task AnErrorInAnOperand_IsRaised() => await Assert.That(Code("(1, boom(), 3)")).IsEqualTo("FORG0001");

    [Test]
    public async Task TheSequenceSizeLimit_BoundsTheResult()
    {
        var limits = XPathLimits.Default.WithMaxSequenceSize(2);

        await Assert.That(Code("(1, 2)", limits: limits)).IsEqualTo(string.Empty);
        await Assert.That(Code("(1, 2, 3)", limits: limits)).IsEqualTo(XPathLimitErrors.SequenceSizeExceeded.LocalName);
        await Assert
            .That(Code("((1, 2), 3)", limits: limits))
            .IsEqualTo(XPathLimitErrors.SequenceSizeExceeded.LocalName);
        await Assert.That(Code("(1 to 2, ())", limits: limits)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task TheSequenceSizeError_LocatesTheComma()
    {
        var compiled = Compile("(1, 2, 3)");
        using var context = NewContext(XPathLimits.Default.WithMaxSequenceSize(2));

        var error = Assert.Throws<XPathException>(() => compiled.Evaluate(context));

        await Assert.That(error.Diagnostic.Offset).IsEqualTo(1);
        await Assert.That(error.Diagnostic.Length).IsEqualTo(7);
    }
}
