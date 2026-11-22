namespace NetEvolve.Schemas.Xsd.Tests.Unit.Assertions.Facet;

using System;
using NetEvolve.Schemas.Xsd.Assertions.Facet;
using NetEvolve.Schemas.Xsd.Tests.Unit.SimpleTypes.Checks;

/// <summary>A static XPath error is a located schema error; a false test or a dynamic error is a validation issue.</summary>
public class AssertionFacetErrorTests
{
    [Test]
    [Arguments("$value +", "XPST0003")]
    [Arguments("$other = 1", "XPST0008")]
    [Arguments("nosuchfunction($value)", "XPST0017")]
    public async Task ATestThatDoesNotCompile_IsALocatedSchemaError(string test, string code)
    {
        using var fixture = new FacetAssertionFixture();

        var facet = FacetAssertion.TryCreate(
            test,
            FacetAssertionFixture.StaticContext,
            new DocumentLocation("file:///s.xsd", 31, 5),
            fixture.Types.Reporter
        );

        await Assert.That(facet).IsNull();
        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(FacetFixture.CodeOf("xpath-valid"));
        await Assert.That(fixture.Issues[0].Location.Line).IsEqualTo(31);
        await Assert.That(fixture.Issues[0].Message).Contains(code);
    }

    [Test]
    public async Task ATestThatCompiles_KeepsItsTextAndLocation()
    {
        using var fixture = new FacetAssertionFixture();

        var facet = fixture.Assertion("$value > 0", 14);

        await Assert.That(facet.Test).IsEqualTo("$value > 0");
        await Assert.That(facet.Location.Line).IsEqualTo(14);
        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
    }

    [Test]
    [Arguments(". > 1")]
    [Arguments("position() = 1")]
    [Arguments("last() = 1")]
    [Arguments("name() = 'x'")]
    public async Task ATestThatUsesTheContextItem_RaisesTheDynamicError_AndIsAValidationIssue(string test)
    {
        using var fixture = new FacetAssertionFixture();
        var type = fixture.Base("integer", test);

        var holds = fixture.Check(type, "3");

        await Assert.That(holds).IsFalse();
        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(FacetFixture.CodeOf("cvc-assertions-valid"));
        await Assert.That(fixture.Issues[0].Message).Contains("XPDY0002");
    }

    [Test]
    public async Task APathStepOnTheAtomicValue_IsATypeError_AndAValidationIssue()
    {
        using var fixture = new FacetAssertionFixture();
        var type = fixture.Base("integer", "exists($value/child::a)");

        var holds = fixture.Check(type, "3");

        await Assert.That(holds).IsFalse();
        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Message).Contains("XPTY0019");
    }

    [Test]
    public async Task ADynamicError_IsAValidationIssue_AndNeverThrows()
    {
        using var fixture = new FacetAssertionFixture();
        var type = fixture.Base("string", "xs:integer($value) > 0");

        var holds = fixture.Check(type, "abc");

        await Assert.That(holds).IsFalse();
        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Message).Contains("FORG0001");
    }

    [Test]
    public async Task ATestWithoutAnEffectiveBooleanValue_IsAValidationIssue_AndNeverThrows()
    {
        using var fixture = new FacetAssertionFixture();
        var type = fixture.Base("list:integer", "$value");

        var holds = fixture.Check(type, "1 2");

        await Assert.That(holds).IsFalse();
        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Message).Contains("FORG0006");
    }

    [Test]
    public async Task ATestThatEvaluatesToAnEmptySequence_IsFalse()
    {
        using var fixture = new FacetAssertionFixture();
        var type = fixture.Base("integer", "()");

        await Assert.That(fixture.Check(type, "1")).IsFalse();
        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task TheValueVariableOfOneEvaluation_DoesNotLeakIntoTheNext()
    {
        using var fixture = new FacetAssertionFixture();
        var type = fixture.Base("integer", "$value = 1");

        await Assert.That(fixture.Check(type, "1")).IsTrue();
        await Assert.That(fixture.Check(type, "2")).IsFalse();
        await Assert.That(fixture.Check(type, "1")).IsTrue();
    }

    [Test]
    public async Task TheGuards_RejectMissingArguments()
    {
        using var fixture = new FacetAssertionFixture();
        var type = fixture.Base("integer", "true()");
        var table = fixture.Table();
        var reporter = fixture.Types.Reporter;
        var assessor = fixture.Types.Assessor;
        var context = fixture.Context;
        var statics = FacetAssertionFixture.StaticContext;

        await Assert
            .That(() => FacetAssertion.TryCreate(null!, statics, default, reporter))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => FacetAssertion.TryCreate("true()", null!, default, reporter))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => FacetAssertion.TryCreate("true()", statics, default, null!))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => FacetAssertionEvaluator.Check(null!, type, "1", assessor, context, default, reporter))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => FacetAssertionEvaluator.Check(table, null!, "1", assessor, context, default, reporter))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => FacetAssertionEvaluator.Check(table, type, null!, assessor, context, default, reporter))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => FacetAssertionEvaluator.Check(table, type, "1", null!, context, default, reporter))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => FacetAssertionEvaluator.Check(table, type, "1", assessor, null!, default, reporter))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => FacetAssertionEvaluator.Check(table, type, "1", assessor, context, default, null!))
            .Throws<ArgumentNullException>();
        await Assert.That(() => new FacetAssertions(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => table.Of(null!)).Throws<ArgumentNullException>();
    }
}
