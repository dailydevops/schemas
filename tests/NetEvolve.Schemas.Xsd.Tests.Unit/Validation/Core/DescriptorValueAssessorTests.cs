namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;
using NetEvolve.Schemas.Xsd.Tests.Unit.SimpleTypes.Definitions;
using NetEvolve.Schemas.Xsd.Validation.Core;

public class DescriptorValueAssessorTests
{
    private static readonly DocumentLocation Where = SchemaFixture.Where;

    private static readonly DescriptorValueAssessor Assessor = new(XsdVersion.V11);

    private static XsdBuiltInType Builtin(string local)
    {
        foreach (var type in XsdBuiltInTypes.For(XsdVersion.V11))
        {
            if (string.Equals(type.Name.LocalName, local, StringComparison.Ordinal))
            {
                return (XsdBuiltInType)type;
            }
        }

        throw new InvalidOperationException("No built-in type " + local);
    }

    private static SimpleTypeDefinition Wrapper(string local) =>
        SimpleTypeDefinition.FromDatatype(
            new XPathQName(SchemaFixture.Xs, local),
            Where,
            new FakeDatatype(local, DatatypeVariety.Atomic)
        );

    private static XsdIssueReporter Reporter => new(new ValidationIssueCollector(ValidationLimits.Default));

    [Test]
    [Arguments("42", true)]
    [Arguments("-7", true)]
    [Arguments("4x2", false)]
    public async Task BuiltInType_IsAssessedByItsDescriptor(string lexical, bool valid)
    {
        var result = Assessor.Assess(Builtin("integer"), lexical, out var member);

        await Assert.That(result.IsValid).IsEqualTo(valid);
        await Assert.That(member).IsEqualTo(-1);
    }

    [Test]
    public async Task BuiltInType_FailingALexicalMapping_ReportsTheDatatypeError()
    {
        var result = Assessor.Assess(Builtin("integer"), "x", out _);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Error).IsNotEqualTo(DatatypeError.None);
    }

    [Test]
    public async Task WrappedBuiltInType_IsFoundByItsName()
    {
        var valid = Assessor.Assess(Wrapper("integer"), "1", out _);
        var invalid = Assessor.Assess(Wrapper("integer"), "x", out _);

        await Assert.That(valid.IsValid).IsTrue();
        await Assert.That(invalid.IsValid).IsFalse();
    }

    [Test]
    public async Task RestrictionOfABuiltIn_IsAssessedAgainstItsBase()
    {
        var restricted = SimpleTypeDefinition.Restrict(
            default,
            Where,
            XsdDerivationMethod.None,
            Wrapper("integer"),
            [],
            Reporter
        )!;

        var valid = Assessor.Assess(restricted, "5", out _);
        var invalid = Assessor.Assess(restricted, "five", out _);

        await Assert.That(valid.IsValid).IsTrue();
        await Assert.That(invalid.IsValid).IsFalse();
    }

    [Test]
    public async Task List_IsAssessedItemByItem()
    {
        var list = SimpleTypeDefinition.List(
            default,
            Where,
            XsdDerivationMethod.None,
            Wrapper("integer"),
            [],
            Reporter
        )!;

        var valid = Assessor.Assess(list, " 1  2\t3\n", out _);
        var invalid = Assessor.Assess(list, "1 x 3", out _);

        await Assert.That(valid.IsValid).IsTrue();
        await Assert.That(invalid.IsValid).IsFalse();
        await Assert.That(invalid.Error).IsEqualTo(DatatypeError.InvalidListItem);
    }

    [Test]
    public async Task Union_AcceptsTheFirstMatchingMember_AndReportsItsIndex()
    {
        var union = SimpleTypeDefinition.Union(
            default,
            Where,
            XsdDerivationMethod.None,
            [Wrapper("integer"), Wrapper("boolean")],
            [],
            Reporter
        )!;

        var first = Assessor.Assess(union, "12", out var firstMember);
        var second = Assessor.Assess(union, "true", out var secondMember);
        var none = Assessor.Assess(union, "maybe", out var noMember);

        await Assert.That(first.IsValid).IsTrue();
        await Assert.That(firstMember).IsEqualTo(0);
        await Assert.That(second.IsValid).IsTrue();
        await Assert.That(secondMember).IsEqualTo(1);
        await Assert.That(none.IsValid).IsFalse();
        await Assert.That(none.Error).IsEqualTo(DatatypeError.NoUnionMember);
        await Assert.That(noMember).IsEqualTo(-1);
    }

    [Test]
    public async Task UnknownSimpleType_IsAccepted_BecauseItsOwnFacetsCannotBeEvaluatedYet()
    {
        var result = Assessor.Assess(Wrapper("custom"), "anything", out _);

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task NullType_Throws() =>
        await Assert.That(() => Assessor.Assess(null!, "x", out _)).Throws<ArgumentNullException>();
}
