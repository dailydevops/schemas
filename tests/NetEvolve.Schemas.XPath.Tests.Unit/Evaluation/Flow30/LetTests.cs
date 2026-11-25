namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Flow30;

using static Flow30Fixture;

/// <summary>The let expression (XPath 3.0, 3.12): bindings are evaluated in order, a later one sees an earlier one, and an inner one shadows an outer one.</summary>
public class LetTests
{
    [Test]
    [Arguments("let $x := 1 return $x + 1", "integer:2")]
    [Arguments("let $x := (1, 2, 3) return $x", "integer:1, integer:2, integer:3")]
    [Arguments("let $x := () return $x", "()")]
    [Arguments("let $x := 1 return ()", "()")]
    public async Task BindsTheWholeValueOfTheSource(string expression, string expected) =>
        await Assert.That(Run(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("let $x := 1, $y := $x + 1 return $y", "integer:2")]
    [Arguments("let $x := 1, $y := $x + 1, $z := $x + $y return $z", "integer:3")]
    public async Task ALaterBinding_SeesAnEarlierOne(string expression, string expected) =>
        await Assert.That(Run(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("let $x := 1 return let $x := 2 return $x", "integer:2")]
    [Arguments("let $x := 1 return ((let $x := 2 return $x), $x)", "integer:2, integer:1")]
    [Arguments("let $x := 1, $x := $x + 10 return $x", "integer:11")]
    [Arguments("let $x := 1 return (let $x := $x + 1 return $x)", "integer:2")]
    public async Task AnInnerBinding_ShadowsAnOuterOne(string expression, string expected) =>
        await Assert.That(Run(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("let $x := $x return 1")]
    [Arguments("(let $x := 1 return $x) + $x")]
    public async Task ABindingIsNotVisible_OutsideItsScope(string expression) =>
        await Assert.That(Code(expression)).IsEqualTo("XPST0008");

    [Test]
    public async Task ALetInsideAFor_RebindsOnEveryIteration() =>
        await Assert
            .That(Run("for $i in (1, 2, 3) return let $d := $i * 2 return $d"))
            .IsEqualTo("integer:2, integer:4, integer:6");
}
