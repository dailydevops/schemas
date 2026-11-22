namespace NetEvolve.Schemas.Xsd.Tests.Unit.Assertions.ComplexTypes;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Assertions.ComplexTypes;
using NetEvolve.Schemas.Xsd.Diagnostics;

public class ComplexTypeAssertionTests
{
    [Test]
    public async Task TryCreate_ValidTest_KeepsTheTextAndTheLocationOfTheAssertElement()
    {
        var (assertion, issues) = AssertionFixture.TryCompile("a = 1");

        await Assert.That(issues).IsEmpty();
        await Assert.That(assertion!.Test).IsEqualTo("a = 1");
        await Assert.That(assertion.Expression.Source).IsEqualTo("a = 1");
        await Assert.That(assertion.Location.Source).IsEqualTo("file:///t.xsd");
        await Assert.That(assertion.Location.Line).IsEqualTo(1);
    }

    [Test]
    [Arguments("a +")]
    [Arguments("")]
    [Arguments("a = ")]
    [Arguments("(a")]
    public async Task TryCreate_SyntaxError_IsALocatedSchemaError(string test)
    {
        var (assertion, issues) = AssertionFixture.TryCompile(test);

        await Assert.That(assertion).IsNull();
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(XsdRuleCodes.XpathValid);
        await Assert.That(issues[0].Constraint).IsEqualTo("xpath-valid");
        await Assert.That(issues[0].Location.Source).IsEqualTo("file:///t.xsd");
        await Assert.That(issues[0].Location.Line).IsEqualTo(1);
        await Assert.That(issues[0].Message).Contains("XPST");
    }

    [Test]
    public async Task TryCreate_UndeclaredPrefix_IsALocatedSchemaError()
    {
        var (assertion, issues) = AssertionFixture.TryCompile("p:a = 1");

        await Assert.That(assertion).IsNull();
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(XsdRuleCodes.XpathValid);
        await Assert.That(issues[0].Message).Contains("XPST0081");
    }

    [Test]
    public async Task TryCreate_PrefixDeclaredInTheSchemaDocument_Compiles()
    {
        var (assertion, issues) = AssertionFixture.TryCompile("p:a = 1 and p:*", " xmlns:p=\"urn:p\"");

        await Assert.That(issues).IsEmpty();
        await Assert.That(assertion).IsNotNull();
    }

    [Test]
    public async Task TryCreate_UnknownVariable_IsALocatedSchemaError()
    {
        var (assertion, issues) = AssertionFixture.TryCompile("$other = 1");

        await Assert.That(assertion).IsNull();
        await Assert.That(Codes(issues)).IsEquivalentTo([XsdRuleCodes.XpathValid]);
    }

    [Test]
    public async Task TryCreate_ValueVariable_IsDeclared()
    {
        var (assertion, issues) = AssertionFixture.TryCompile("$value = 1");

        await Assert.That(issues).IsEmpty();
        await Assert.That(assertion).IsNotNull();
    }

    [Test]
    [Arguments("fn:count(*) = 0")]
    [Arguments("xs:integer('3') = 3")]
    [Arguments("count(*) = 0")]
    public async Task TryCreate_PredeclaredPrefixesAndFunctions_Compile(string test)
    {
        // The schema document of the fixture declares xs only, so fn shows the prefixes the static context predeclares.
        var (assertion, issues) = AssertionFixture.TryCompile(test);

        await Assert.That(issues).IsEmpty();
        await Assert.That(assertion).IsNotNull();
    }

    [Test]
    public async Task TryCreate_PrefixOfTheSchemaDocument_WinsOverAPredeclaredOne()
    {
        // fn is bound to another namespace here, so fn:count is not a function of that namespace any more.
        var (assertion, issues) = AssertionFixture.TryCompile("fn:count(*) = 0", " xmlns:fn=\"urn:other\"");

        await Assert.That(assertion).IsNull();
        await Assert.That(Codes(issues)).IsEquivalentTo([XsdRuleCodes.XpathValid]);
    }

    [Test]
    public async Task TryCreate_StrictVersion10_StillCompiles()
    {
        // The construct is rejected by the version gate of the schema; the assertion itself reads without the attribute.
        var reporter = new XsdIssueReporter(new ValidationIssueCollector(ValidationLimits.Default));
        var node = AssertionFixture.AssertNodes(
            AssertionFixture.Schema(string.Empty, AssertionFixture.Assert("a = 1"))
        )[0];

        var assertion = ComplexTypeAssertion.TryCreate(node, AssertionFixture.Scope, XsdVersion.V10, reporter);

        await Assert.That(assertion).IsNotNull();
    }

    [Test]
    public async Task TryCreate_NullNode_Throws() =>
        await Assert
            .That(() =>
                ComplexTypeAssertion.TryCreate(
                    null!,
                    AssertionFixture.Scope,
                    XsdVersion.V11,
                    new XsdIssueReporter(new ValidationIssueCollector(ValidationLimits.Default))
                )
            )
            .Throws<ArgumentNullException>();

    [Test]
    public async Task TryCreate_NullReporter_Throws()
    {
        var node = AssertionFixture.AssertNodes(
            AssertionFixture.Schema(string.Empty, AssertionFixture.Assert("a = 1"))
        )[0];

        await Assert
            .That(() => ComplexTypeAssertion.TryCreate(node, AssertionFixture.Scope, XsdVersion.V11, null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task TryCreate_VersionOutsideTheKnownOnes_Throws()
    {
        var node = AssertionFixture.AssertNodes(
            AssertionFixture.Schema(string.Empty, AssertionFixture.Assert("a = 1"))
        )[0];
        var reporter = new XsdIssueReporter(new ValidationIssueCollector(ValidationLimits.Default));

        await Assert
            .That(() => ComplexTypeAssertion.TryCreate(node, AssertionFixture.Scope, (XsdVersion)99, reporter))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Constructor_NullArguments_Throw()
    {
        var expression = AssertionFixture.TryCompile("a = 1").Assertion!.Expression;

        await Assert.That(() => new ComplexTypeAssertion(null!, expression, default)).Throws<ArgumentNullException>();
        await Assert.That(() => new ComplexTypeAssertion("a", null!, default)).Throws<ArgumentNullException>();
    }

    private static DiagnosticCode[] Codes(IReadOnlyList<ValidationIssue> issues) =>
        [.. issues.Select(static issue => issue.Code)];
}
