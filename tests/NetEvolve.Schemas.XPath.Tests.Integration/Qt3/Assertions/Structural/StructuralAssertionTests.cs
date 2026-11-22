namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Structural;

using NetEvolve.Schemas.Conformance.Results;
using static NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Structural.StructuralHarness;

/// <summary>The assertion kinds assert-deep-eq, assert-permutation and assert-type: each passes and fails on a hand-written catalog fragment.</summary>
public class StructuralAssertionTests
{
    [Test]
    [Arguments("(1, 2, 3)", "(1, 2, 3)")]
    [Arguments("/doc/a", "/doc/a")]
    [Arguments("()", "()")]
    [Arguments("/doc/a/text()", "'1'")]
    public async Task AssertDeepEq_ResultEqualsTheExpression_Passes(string query, string expected)
    {
        var result = Run(query, $"<assert-deep-eq>{expected}</assert-deep-eq>");

        await Assert.That(result.Outcome).IsEqualTo(CaseOutcome.Pass).Because(result.Reason ?? string.Empty);
    }

    [Test]
    [Arguments("(1, 2, 3)", "(1, 2, 4)")]
    [Arguments("(1, 2, 3)", "(3, 2, 1)")]
    [Arguments("(1, 2)", "(1, 2, 3)")]
    [Arguments("/doc/a", "/doc/b")]
    [Arguments("1", "'1'")]
    public async Task AssertDeepEq_ResultDiffersFromTheExpression_Fails(string query, string expected)
    {
        var result = Run(query, $"<assert-deep-eq>{expected}</assert-deep-eq>");

        await Assert.That(result.Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(result.Reason).IsNotNull();
    }

    [Test]
    [Arguments("(1, 2, 3)", "(3, 1, 2)")]
    [Arguments("(1, 2, 2, 3)", "(2, 3, 2, 1)")]
    [Arguments("(1, 2, 3)", "(1, 2, 3)")]
    [Arguments("()", "()")]
    [Arguments("(/doc/b, /doc/a)", "(/doc/a, /doc/b)")]
    public async Task AssertPermutation_ResultIsAnUnorderedMatch_Passes(string query, string expected)
    {
        var result = Run(query, $"<assert-permutation>{expected}</assert-permutation>");

        await Assert.That(result.Outcome).IsEqualTo(CaseOutcome.Pass).Because(result.Reason ?? string.Empty);
    }

    [Test]
    [Arguments("(1, 2, 2)", "(1, 2, 3)")]
    [Arguments("(1, 2)", "(1, 2, 3)")]
    [Arguments("(1, 2, 2)", "(1, 1, 2)")]
    [Arguments("(1, 2, 3)", "(1, 2)")]
    [Arguments("(/doc/a, /doc/a)", "(/doc/a, /doc/b)")]
    public async Task AssertPermutation_ResultIsNotAPermutation_Fails(string query, string expected)
    {
        var result = Run(query, $"<assert-permutation>{expected}</assert-permutation>");

        await Assert.That(result.Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(result.Reason).IsNotNull();
    }

    [Test]
    [Arguments("1", "xs:integer")]
    [Arguments("(1, 2)", "xs:integer+")]
    [Arguments("()", "empty-sequence()")]
    [Arguments("/doc/a", "element(a)")]
    [Arguments("(1, 'a')", "item()*")]
    [Arguments("1.5e0", "xs:double")]
    public async Task AssertType_ResultMatchesTheSequenceType_Passes(string query, string type)
    {
        var result = Run(query, $"<assert-type>{type}</assert-type>");

        await Assert.That(result.Outcome).IsEqualTo(CaseOutcome.Pass).Because(result.Reason ?? string.Empty);
    }

    [Test]
    [Arguments("'a'", "xs:integer")]
    [Arguments("(1, 2)", "xs:integer")]
    [Arguments("()", "xs:integer+")]
    [Arguments("/doc/a", "element(b)")]
    [Arguments("1", "xs:string?")]
    public async Task AssertType_ResultDoesNotMatchTheSequenceType_Fails(string query, string type)
    {
        var result = Run(query, $"<assert-type>{type}</assert-type>");

        await Assert.That(result.Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(result.Reason).IsNotNull();
    }

    [Test]
    [Arguments("assert-deep-eq", "(1)")]
    [Arguments("assert-permutation", "(1)")]
    [Arguments("assert-type", "xs:integer")]
    public async Task EveryKind_QueryRaisesAnError_Fails(string kind, string value)
    {
        var result = Run("1 div 0", $"<{kind}>{value}</{kind}>");

        await Assert.That(result.Outcome).IsEqualTo(CaseOutcome.Fail);
    }

    [Test]
    [Arguments("assert-deep-eq", "1 div 0")]
    [Arguments("assert-permutation", "1 div 0")]
    [Arguments("assert-type", "xs:nonexistent")]
    public async Task EveryKind_ExpressionRaisesAnError_Fails(string kind, string value)
    {
        var result = Run("1", $"<{kind}>{value}</{kind}>");

        await Assert.That(result.Outcome).IsEqualTo(CaseOutcome.Fail);
    }
}
