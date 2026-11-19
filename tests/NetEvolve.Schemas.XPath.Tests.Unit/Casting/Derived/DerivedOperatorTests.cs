namespace NetEvolve.Schemas.XPath.Tests.Unit.Casting.Derived;

using System;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Tests.Unit.Casting.Core;

/// <summary>A value of a derived type takes part in the comparisons of its base type: the operator tables resolve by derivation, so the annotation does not hide the value.</summary>
public class DerivedOperatorTests
{
    private static readonly ComparisonOperatorTable Operators = CreateOperators();

    [Test]
    [Arguments("xs:int(1) eq 1", true)]
    [Arguments("xs:int(1) eq xs:short(1)", true)]
    [Arguments("xs:byte(5) lt 6", true)]
    [Arguments("xs:unsignedByte(200) gt xs:byte(100)", true)]
    [Arguments("xs:long(1) ne 1.0", false)]
    [Arguments("xs:token('a') eq 'a'", true)]
    [Arguments("xs:token('a') eq xs:NCName('a')", true)]
    [Arguments("xs:token(' a  b ') eq 'a b'", true)]
    [Arguments("xs:normalizedString(' a') eq 'a'", false)]
    [Arguments("xs:token('b') gt 'a'", true)]
    [Arguments("xs:positiveInteger(5) = (1, 5)", true)]
    public async Task Comparison_OfDerivedValues_UsesTheValueOfTheBaseType(string expression, bool expected)
    {
        var registry = CastFixture.Registry();
        ComparisonLowerings.Register(registry, Operators);
        _ = XPathParser.TryParse(expression.AsSpan(), out var syntax, out _);
        _ = ExpressionCompiler.TryCompile(
            syntax!,
            expression,
            CastFixture.Statics,
            registry,
            out var compiled,
            out var diagnostic
        );

        await Assert.That(compiled).IsNotNull().Because(diagnostic.Code);
        using var context = new XPathEvaluationContext();
        var result = compiled!.Evaluate(context);
        await Assert.That(result.Count).IsEqualTo(1);
        _ = result[0].TryGetBoolean(out var value);
        await Assert.That(value).IsEqualTo(expected);
    }

    private static ComparisonOperatorTable CreateOperators()
    {
        var table = new ComparisonOperatorTable();
        ComparisonOperators.Register(table);
        return table;
    }
}
