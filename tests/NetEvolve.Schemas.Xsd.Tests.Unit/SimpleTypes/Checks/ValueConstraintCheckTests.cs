namespace NetEvolve.Schemas.Xsd.Tests.Unit.SimpleTypes.Checks;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.SimpleTypes.Checks;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;
using NetEvolve.Schemas.Xsd.Tests.Unit.Elements;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;

/// <summary>A default or fixed value of an element or attribute must be valid for the type of the declaration.</summary>
public class ValueConstraintCheckTests
{
    private const string Tns = "urn:test";
    private static readonly XPathQName ElementName = new(Tns, "e");

    private static ComplexTypeHeader Header() =>
        new(new XPathQName(Tns, "c"), FacetFixture.Where, false, XsdDerivationMethod.None, XsdDerivationMethod.None);

    private static Particle Child(long min) =>
        Particle.Create(FacetFixture.Where, min, 1, new FakeComponent(XsdComponentKind.ElementDeclaration, "child"));

    private static ElementDeclaration Element(IXsdComponent type, ElementValueConstraintKind kind, string value)
    {
        var declaration = ElementDeclaration.Create(
            ElementName,
            new DocumentLocation("file:///f.xsd", 5, 3),
            true,
            default,
            null,
            false,
            new ElementValueConstraint(kind, value),
            false,
            XsdDerivationMethod.None,
            XsdDerivationMethod.None
        );
        declaration.BindInlineType(type);
        return declaration;
    }

    // Elements: a simple type, as the resolver binds a type by name.
    [Test]
    [Arguments("int", "5")]
    [Arguments("int", " 5 ")]
    [Arguments("string", "")]
    [Arguments("string", "any text")]
    [Arguments("boolean", "true")]
    [Arguments("date", "2020-01-01")]
    [Arguments("byte", "-128")]
    public async Task ElementValueValidForItsBuiltInType_IsValid(string type, string value)
    {
        var fixture = new FacetFixture();

        foreach (var kind in new[] { ElementValueConstraintKind.Default, ElementValueConstraintKind.Fixed })
        {
            var element = Element(fixture.BuiltIn(type), kind, value);

            await Assert
                .That(ValueConstraintCheck.Check(element, fixture.Assessor, fixture.Version, fixture.Reporter))
                .IsTrue();
        }

        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
    }

    [Test]
    [Arguments("int", "abc")]
    [Arguments("int", "")]
    [Arguments("int", "5.5")]
    [Arguments("byte", "128")]
    [Arguments("unsignedInt", "-1")]
    [Arguments("boolean", "maybe")]
    [Arguments("date", "2020-13-01")]
    [Arguments("decimal", "1,5")]
    public async Task ElementValueInvalidForItsBuiltInType_IsALocatedSchemaError(string type, string value)
    {
        var fixture = new FacetFixture();

        foreach (var kind in new[] { ElementValueConstraintKind.Default, ElementValueConstraintKind.Fixed })
        {
            var element = Element(fixture.BuiltIn(type), kind, value);

            await Assert
                .That(ValueConstraintCheck.Check(element, fixture.Assessor, fixture.Version, fixture.Reporter))
                .IsFalse();
        }

        await Assert.That(fixture.Issues.Count).IsEqualTo(2);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(FacetFixture.CodeOf("cos-valid-default"));
        await Assert.That(fixture.Issues[0].Location.Line).IsEqualTo(5);
        await Assert.That(fixture.Issues[1].Code).IsEqualTo(FacetFixture.CodeOf("cos-valid-default"));
    }

    [Test]
    public async Task ElementValueIsCheckedAgainstTheTypeAsTheResolverBindsIt()
    {
        var fixture = new FacetFixture();
        var schema = ElementFixture
            .Build(
                "<xs:element name=\"ok\" type=\"xs:int\" default=\"5\"/>\n<xs:element name=\"bad\" type=\"xs:int\" default=\"abc\"/>\n<xs:element name=\"fixedBad\" type=\"xs:boolean\" fixed=\"x\"/>\n<xs:element name=\"plain\" type=\"xs:int\"/>"
            )
            .Resolve();

        var ok = ValueConstraintCheck.Check(schema.Element("ok"), fixture.Assessor, XsdVersion.V11, fixture.Reporter);
        var bad = ValueConstraintCheck.Check(schema.Element("bad"), fixture.Assessor, XsdVersion.V11, fixture.Reporter);
        var fixedBad = ValueConstraintCheck.Check(
            schema.Element("fixedBad"),
            fixture.Assessor,
            XsdVersion.V11,
            fixture.Reporter
        );
        var plain = ValueConstraintCheck.Check(
            schema.Element("plain"),
            fixture.Assessor,
            XsdVersion.V11,
            fixture.Reporter
        );

        await Assert.That(ok).IsTrue();
        await Assert.That(bad).IsFalse();
        await Assert.That(fixedBad).IsFalse();
        await Assert.That(plain).IsTrue();
        await Assert.That(fixture.Issues.Count).IsEqualTo(2);
        await Assert.That(fixture.Issues[0].Location.Line).IsEqualTo(schema.Element("bad").Location.Line);
        await Assert.That(fixture.Issues[1].Location.Line).IsEqualTo(schema.Element("fixedBad").Location.Line);
    }

    [Test]
    public async Task ElementWithoutAValueConstraintOrWithoutAResolvedType_IsValid()
    {
        var fixture = new FacetFixture();
        var none = Element(fixture.BuiltIn("int"), ElementValueConstraintKind.None, "");
        var unresolved = ElementDeclaration.Create(
            ElementName,
            FacetFixture.Where,
            true,
            new XPathQName(Tns, "missing"),
            null,
            false,
            new ElementValueConstraint(ElementValueConstraintKind.Default, "abc"),
            false,
            XsdDerivationMethod.None,
            XsdDerivationMethod.None
        );

        await Assert
            .That(ValueConstraintCheck.Check(none, fixture.Assessor, fixture.Version, fixture.Reporter))
            .IsTrue();
        await Assert
            .That(ValueConstraintCheck.Check(unresolved, fixture.Assessor, fixture.Version, fixture.Reporter))
            .IsTrue();
        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
    }

    // Elements: a complex type is allowed when its content is simple, or mixed with a particle that can be empty.
    [Test]
    public async Task ElementOfAComplexTypeWithSimpleContent_IsCheckedAgainstTheContentType()
    {
        var fixture = new FacetFixture();
        var complex = ComplexTypeDefinition.FromSimpleContent(
            Header(),
            XsdDerivationMethod.Extension,
            fixture.Wrapped("int"),
            null,
            [],
            null,
            fixture.Reporter
        )!;

        var valid = Element(complex, ElementValueConstraintKind.Default, "7");
        var invalid = Element(complex, ElementValueConstraintKind.Fixed, "seven");

        await Assert
            .That(ValueConstraintCheck.Check(valid, fixture.Assessor, fixture.Version, fixture.Reporter))
            .IsTrue();
        await Assert
            .That(ValueConstraintCheck.Check(invalid, fixture.Assessor, fixture.Version, fixture.Reporter))
            .IsFalse();
        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(FacetFixture.CodeOf("cos-valid-default"));
    }

    [Test]
    public async Task ElementOfAMixedComplexTypeWithAnEmptiableParticle_AcceptsAnyValue()
    {
        var fixture = new FacetFixture();
        var mixed = ComplexTypeDefinition.Implicit(
            Header(),
            true,
            Particle.Create(
                FacetFixture.Where,
                1,
                1,
                ModelGroup.Create(ModelGroupCompositor.Sequence, FacetFixture.Where, [Child(0)])
            ),
            [],
            null,
            fixture.Reporter
        );

        var element = Element(mixed, ElementValueConstraintKind.Default, "any text");

        await Assert
            .That(ValueConstraintCheck.Check(element, fixture.Assessor, fixture.Version, fixture.Reporter))
            .IsTrue();
        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ElementOfAMixedComplexTypeWithoutContent_AcceptsAnyValue()
    {
        var fixture = new FacetFixture();
        var mixed = ComplexTypeDefinition.Implicit(Header(), true, null, [], null, fixture.Reporter);

        var element = Element(mixed, ElementValueConstraintKind.Default, "any text");

        await Assert
            .That(ValueConstraintCheck.Check(element, fixture.Assessor, fixture.Version, fixture.Reporter))
            .IsTrue();
    }

    [Test]
    public async Task ElementOfAMixedComplexTypeWhoseParticleCannotBeEmpty_IsALocatedSchemaError()
    {
        var fixture = new FacetFixture();
        var mixed = ComplexTypeDefinition.Implicit(
            Header(),
            true,
            Particle.Create(
                FacetFixture.Where,
                1,
                1,
                ModelGroup.Create(ModelGroupCompositor.Sequence, FacetFixture.Where, [Child(1)])
            ),
            [],
            null,
            fixture.Reporter
        );

        var element = Element(mixed, ElementValueConstraintKind.Default, "text");

        await Assert
            .That(ValueConstraintCheck.Check(element, fixture.Assessor, fixture.Version, fixture.Reporter))
            .IsFalse();
        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(FacetFixture.CodeOf("cos-valid-default"));
        await Assert.That(fixture.Issues[0].Location.Line).IsEqualTo(5);
    }

    [Test]
    public async Task ElementOfAnElementOnlyOrEmptyComplexType_IsALocatedSchemaError()
    {
        var fixture = new FacetFixture();
        var elementOnly = ComplexTypeDefinition.Implicit(
            Header(),
            false,
            Particle.Create(
                FacetFixture.Where,
                1,
                1,
                ModelGroup.Create(ModelGroupCompositor.Sequence, FacetFixture.Where, [Child(0)])
            ),
            [],
            null,
            fixture.Reporter
        );
        var empty = ComplexTypeDefinition.Implicit(Header(), false, null, [], null, fixture.Reporter);

        var first = Element(elementOnly, ElementValueConstraintKind.Default, "x");
        var second = Element(empty, ElementValueConstraintKind.Fixed, "x");

        await Assert
            .That(ValueConstraintCheck.Check(first, fixture.Assessor, fixture.Version, fixture.Reporter))
            .IsFalse();
        await Assert
            .That(ValueConstraintCheck.Check(second, fixture.Assessor, fixture.Version, fixture.Reporter))
            .IsFalse();
        await Assert.That(fixture.Issues.Count).IsEqualTo(2);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(FacetFixture.CodeOf("cos-valid-default"));
    }

    [Test]
    public async Task ElementOfTheAnyType_AcceptsAnyValue()
    {
        var fixture = new FacetFixture();
        var viaComponent = Element(fixture.BuiltIn("anyType"), ElementValueConstraintKind.Default, "x");
        var viaDefinition = Element(ComplexTypeDefinition.AnyType, ElementValueConstraintKind.Default, "x");

        await Assert
            .That(ValueConstraintCheck.Check(viaComponent, fixture.Assessor, fixture.Version, fixture.Reporter))
            .IsTrue();
        await Assert
            .That(ValueConstraintCheck.Check(viaDefinition, fixture.Assessor, fixture.Version, fixture.Reporter))
            .IsTrue();
        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
    }

    // Attributes.
    [Test]
    [Arguments("int", "5")]
    [Arguments("string", "")]
    [Arguments("boolean", "false")]
    [Arguments("date", "2020-02-29")]
    public async Task AttributeValueValidForItsType_IsValid(string type, string value)
    {
        var fixture = new FacetFixture();

        foreach (var kind in new[] { AttributeValueConstraintKind.Default, AttributeValueConstraintKind.Fixed })
        {
            var attribute = AttributeDeclaration.Global(
                "a",
                Tns,
                FacetFixture.Where,
                fixture.Wrapped(type),
                new AttributeValueConstraint(kind, value),
                fixture.Reporter
            )!;

            await Assert
                .That(ValueConstraintCheck.Check(attribute, fixture.Assessor, fixture.Version, fixture.Reporter))
                .IsTrue();
        }

        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
    }

    [Test]
    [Arguments("int", "abc")]
    [Arguments("byte", "300")]
    [Arguments("boolean", "yes")]
    [Arguments("date", "2020-02-30")]
    public async Task AttributeValueInvalidForItsType_IsALocatedSchemaError(string type, string value)
    {
        var fixture = new FacetFixture(XsdVersion.V10);

        foreach (var kind in new[] { AttributeValueConstraintKind.Default, AttributeValueConstraintKind.Fixed })
        {
            var attribute = AttributeDeclaration.Global(
                "a",
                Tns,
                new DocumentLocation("file:///f.xsd", 6, 4),
                fixture.Wrapped(type),
                new AttributeValueConstraint(kind, value),
                fixture.Reporter
            )!;

            await Assert
                .That(ValueConstraintCheck.Check(attribute, fixture.Assessor, fixture.Version, fixture.Reporter))
                .IsFalse();
        }

        await Assert.That(fixture.Issues.Count).IsEqualTo(2);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(FacetFixture.CodeOf("a-props-correct"));
        await Assert.That(fixture.Issues[0].Location.Line).IsEqualTo(6);
        await Assert.That(fixture.Issues[1].Code).IsEqualTo(FacetFixture.CodeOf("a-props-correct"));
    }

    [Test]
    public async Task AttributeValueInvalidForItsType_Xsd11_NamesTheSimpleDefaultRule()
    {
        var fixture = new FacetFixture(XsdVersion.V11);
        var attribute = AttributeDeclaration.Global(
            "a",
            Tns,
            new DocumentLocation("file:///f.xsd", 6, 4),
            fixture.Wrapped("int"),
            new AttributeValueConstraint(AttributeValueConstraintKind.Default, "abc"),
            fixture.Reporter
        )!;

        await Assert
            .That(ValueConstraintCheck.Check(attribute, fixture.Assessor, fixture.Version, fixture.Reporter))
            .IsFalse();
        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(FacetFixture.CodeOf("cos-valid-simple-default"));
        await Assert.That(fixture.Issues[0].Location.Line).IsEqualTo(6);
    }

    [Test]
    public async Task AttributeWithoutAValueConstraint_IsValid()
    {
        var fixture = new FacetFixture();
        var attribute = AttributeDeclaration.Global(
            "a",
            Tns,
            FacetFixture.Where,
            fixture.Wrapped("int"),
            default,
            fixture.Reporter
        )!;

        await Assert
            .That(ValueConstraintCheck.Check(attribute, fixture.Assessor, fixture.Version, fixture.Reporter))
            .IsTrue();
        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task AttributeUseValue_IsCheckedAgainstTheTypeOfTheDeclaration()
    {
        var fixture = new FacetFixture();
        var declaration = AttributeDeclaration.Global(
            "a",
            Tns,
            FacetFixture.Where,
            fixture.Wrapped("int"),
            default,
            fixture.Reporter
        )!;
        var valid = AttributeUse.Create(
            FacetFixture.Where,
            AttributeUseKind.Optional,
            declaration,
            new AttributeValueConstraint(AttributeValueConstraintKind.Default, "5"),
            fixture.Reporter
        )!;
        var invalid = AttributeUse.Create(
            new DocumentLocation("file:///f.xsd", 8, 2),
            AttributeUseKind.Required,
            declaration,
            new AttributeValueConstraint(AttributeValueConstraintKind.Fixed, "five"),
            fixture.Reporter
        )!;
        var none = AttributeUse.Create(
            FacetFixture.Where,
            AttributeUseKind.Optional,
            declaration,
            default,
            fixture.Reporter
        )!;

        await Assert
            .That(ValueConstraintCheck.Check(valid, fixture.Assessor, fixture.Version, fixture.Reporter))
            .IsTrue();
        await Assert
            .That(ValueConstraintCheck.Check(none, fixture.Assessor, fixture.Version, fixture.Reporter))
            .IsTrue();
        await Assert
            .That(ValueConstraintCheck.Check(invalid, fixture.Assessor, fixture.Version, fixture.Reporter))
            .IsFalse();
        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(FacetFixture.CodeOf("au-props-correct"));
        await Assert.That(fixture.Issues[0].Location.Line).IsEqualTo(8);
    }

    [Test]
    public async Task AttributeUseThatInheritsTheConstraintOfItsDeclaration_IsNotReportedASecondTime()
    {
        var fixture = new FacetFixture();
        var declaration = AttributeDeclaration.Global(
            "a",
            Tns,
            FacetFixture.Where,
            fixture.Wrapped("int"),
            new AttributeValueConstraint(AttributeValueConstraintKind.Default, "abc"),
            fixture.Reporter
        )!;
        var use = AttributeUse.Create(
            FacetFixture.Where,
            AttributeUseKind.Optional,
            declaration,
            default,
            fixture.Reporter
        )!;

        await Assert
            .That(ValueConstraintCheck.Check(declaration, fixture.Assessor, fixture.Version, fixture.Reporter))
            .IsFalse();
        await Assert
            .That(ValueConstraintCheck.Check(use, fixture.Assessor, fixture.Version, fixture.Reporter))
            .IsTrue();
        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task TheElementGuards_RejectNullArgumentsAndAnUndefinedVersion()
    {
        var fixture = new FacetFixture();
        var element = Element(fixture.BuiltIn("int"), ElementValueConstraintKind.None, "");

        await Assert
            .That(() =>
                ValueConstraintCheck.Check(
                    (ElementDeclaration)null!,
                    fixture.Assessor,
                    fixture.Version,
                    fixture.Reporter
                )
            )
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => ValueConstraintCheck.Check(element, null!, fixture.Version, fixture.Reporter))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => ValueConstraintCheck.Check(element, fixture.Assessor, fixture.Version, null!))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => ValueConstraintCheck.Check(element, fixture.Assessor, (XsdVersion)7, fixture.Reporter))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task TheAttributeGuards_RejectNullArgumentsAndAnUndefinedVersion()
    {
        var fixture = new FacetFixture();
        var attribute = AttributeDeclaration.Global(
            "a",
            Tns,
            FacetFixture.Where,
            fixture.Wrapped("int"),
            default,
            fixture.Reporter
        )!;

        await Assert
            .That(() =>
                ValueConstraintCheck.Check(
                    (AttributeDeclaration)null!,
                    fixture.Assessor,
                    fixture.Version,
                    fixture.Reporter
                )
            )
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => ValueConstraintCheck.Check(attribute, null!, fixture.Version, fixture.Reporter))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => ValueConstraintCheck.Check(attribute, fixture.Assessor, fixture.Version, null!))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => ValueConstraintCheck.Check(attribute, fixture.Assessor, (XsdVersion)7, fixture.Reporter))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task TheUseGuards_RejectNullArgumentsAndAnUndefinedVersion()
    {
        var fixture = new FacetFixture();
        var attribute = AttributeDeclaration.Global(
            "a",
            Tns,
            FacetFixture.Where,
            fixture.Wrapped("int"),
            default,
            fixture.Reporter
        )!;
        var use = AttributeUse.Create(
            FacetFixture.Where,
            AttributeUseKind.Optional,
            attribute,
            default,
            fixture.Reporter
        )!;

        await Assert
            .That(() =>
                ValueConstraintCheck.Check((AttributeUse)null!, fixture.Assessor, fixture.Version, fixture.Reporter)
            )
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => ValueConstraintCheck.Check(use, null!, fixture.Version, fixture.Reporter))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => ValueConstraintCheck.Check(use, fixture.Assessor, fixture.Version, null!))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => ValueConstraintCheck.Check(use, fixture.Assessor, (XsdVersion)7, fixture.Reporter))
            .Throws<ArgumentOutOfRangeException>();
    }

    // A declaration whose type is ID, or derived from ID, has no value constraint (a-props-correct, e-props-correct).
    [Test]
    [Arguments("ID", true)]
    [Arguments("derived", true)]
    [Arguments("NCName", false)]
    [Arguments("string", false)]
    public async Task ElementOfAnIdType_CannotCarryAValueConstraint(string type, bool isId)
    {
        var fixture = new FacetFixture();
        var simple = type == "derived" ? fixture.Derive(fixture.Wrapped("ID")) : fixture.Wrapped(type);

        foreach (var kind in new[] { ElementValueConstraintKind.Default, ElementValueConstraintKind.Fixed })
        {
            await Assert
                .That(
                    ValueConstraintCheck.Check(
                        Element(simple, kind, "abc"),
                        fixture.Assessor,
                        fixture.Version,
                        fixture.Reporter
                    )
                )
                .IsEqualTo(!isId);
        }

        await Assert.That(fixture.Issues.Count).IsEqualTo(isId ? 2 : 0);
        if (isId)
        {
            await Assert.That(fixture.Issues[0].Code).IsEqualTo(FacetFixture.CodeOf("e-props-correct"));
            await Assert.That(fixture.Issues[0].Location.Line).IsEqualTo(5);
        }
    }

    [Test]
    public async Task ElementOfAComplexTypeWithIdSimpleContent_CannotCarryAValueConstraint()
    {
        var fixture = new FacetFixture();
        var complex = ComplexTypeDefinition.FromSimpleContent(
            Header(),
            XsdDerivationMethod.Extension,
            fixture.Derive(fixture.Wrapped("ID")),
            null,
            [],
            null,
            fixture.Reporter
        )!;

        await Assert
            .That(
                ValueConstraintCheck.Check(
                    Element(complex, ElementValueConstraintKind.Default, "abc"),
                    fixture.Assessor,
                    fixture.Version,
                    fixture.Reporter
                )
            )
            .IsFalse();
        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(FacetFixture.CodeOf("e-props-correct"));
    }

    [Test]
    [Arguments("ID", true, XsdVersion.V10)]
    [Arguments("derived", true, XsdVersion.V11)]
    [Arguments("NCName", false, XsdVersion.V10)]
    public async Task AttributeOfAnIdType_CannotCarryAValueConstraint(string type, bool isId, XsdVersion version)
    {
        var fixture = new FacetFixture(version);
        var simple = type == "derived" ? fixture.Derive(fixture.Wrapped("ID")) : fixture.Wrapped(type);

        foreach (var kind in new[] { AttributeValueConstraintKind.Default, AttributeValueConstraintKind.Fixed })
        {
            var attribute = AttributeDeclaration.Global(
                "a",
                Tns,
                FacetFixture.Where,
                simple,
                new AttributeValueConstraint(kind, "abc"),
                fixture.Reporter
            )!;

            await Assert
                .That(ValueConstraintCheck.Check(attribute, fixture.Assessor, fixture.Version, fixture.Reporter))
                .IsEqualTo(!isId);
        }

        await Assert.That(fixture.Issues.Count).IsEqualTo(isId ? 2 : 0);
        if (isId)
        {
            await Assert.That(fixture.Issues[0].Code).IsEqualTo(FacetFixture.CodeOf("a-props-correct"));
        }
    }
}
