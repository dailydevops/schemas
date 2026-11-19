namespace NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Types;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;

public class DerivationValidExtensionTests
{
    private const XsdDerivationMethod None = XsdDerivationMethod.None;
    private const XsdDerivationMethod Ext = XsdDerivationMethod.Extension;
    private const XsdDerivationMethod Res = XsdDerivationMethod.Restriction;

    private readonly TypeFixture _f = new();

    private async Task AssertSingle(string ruleId, string clause)
    {
        await Assert.That(_f.Issues).Count().IsEqualTo(1);
        await Assert.That(_f.Issues[0].Constraint).IsEqualTo(clause);
        await Assert.That(FakeRules.Find(_f.Issues[0].Code).Id).IsEqualTo(ruleId);
        await Assert.That(_f.Issues[0].Location.Line).IsEqualTo(7);
    }

    private static Particle ElementParticle(string name) => TypeFixture.Element(name);

    [Test]
    public async Task Extension_AppendsTheNewParticleAfterTheBaseContentInASequence()
    {
        var baseContent = TypeFixture.Sequence(ElementParticle("x"));
        var ownContent = TypeFixture.Sequence(ElementParticle("y"));
        var parent = _f.Root("a", baseContent);

        var type = _f.Complex("b", Ext, parent, ownContent);
        TypeFixture.Checker().Report(type, _f.Reporter);

        var sequence = (ModelGroup)type.ContentParticle!.Term!;
        await Assert.That(sequence.Compositor).IsEqualTo(ModelGroupCompositor.Sequence);
        await Assert.That(sequence.Particles).Count().IsEqualTo(2);
        await Assert.That(sequence.Particles[0]).IsSameReferenceAs(baseContent);
        await Assert.That(sequence.Particles[1]).IsSameReferenceAs(ownContent);
        await Assert.That(_f.Issues).Count().IsEqualTo(0);
        await Assert.That(TypeFixture.Checker().Check(type, parent, Ext, None).IsValid).IsTrue();
    }

    [Test]
    public async Task Extension_WithoutOwnContent_KeepsTheBaseContent()
    {
        var parent = _f.Root("a", TypeFixture.Sequence(ElementParticle("x")));

        var type = _f.Complex("b", Ext, parent);
        TypeFixture.Checker().Report(type, _f.Reporter);

        await Assert.That(type.ContentParticle).IsSameReferenceAs(parent.ContentParticle);
        await Assert.That(_f.Issues).Count().IsEqualTo(0);
    }

    [Test]
    public async Task Extension_OfAnEmptyBase_TakesTheOwnContent()
    {
        var parent = _f.Root("a");

        var type = _f.Complex("b", Ext, parent, TypeFixture.Sequence(ElementParticle("y")));
        TypeFixture.Checker().Report(type, _f.Reporter);

        await Assert.That(_f.Issues).Count().IsEqualTo(0);
    }

    [Test]
    public async Task Extension_OfTheUrType_IsValid()
    {
        var type = _f.Complex(
            "b",
            Ext,
            TypeFixture.Registered("anyType"),
            TypeFixture.Sequence(ElementParticle("y")),
            mixed: true
        );
        TypeFixture.Checker().Report(type, _f.Reporter);

        await Assert.That(_f.Issues).Count().IsEqualTo(0);
    }

    [Test]
    public async Task Extension_AddsAttributeUses_AndWidensTheAttributeWildcard()
    {
        var parent = _f.Root("a", null, false, [_f.Use("p")], TypeFixture.Wild(TypeFixture.Namespaces("urn:a")));

        var type = _f.Complex(
            "b",
            Ext,
            parent,
            null,
            false,
            [_f.Use("q")],
            TypeFixture.Wild(TypeFixture.Namespaces("urn:b"))
        );
        TypeFixture.Checker().Report(type, _f.Reporter);

        await Assert.That(type.AttributeUses).Count().IsEqualTo(2);
        await Assert.That(type.AttributeWildcard!.IsSubsetOf(parent.AttributeWildcard!)).IsFalse();
        await Assert.That(parent.AttributeWildcard!.IsSubsetOf(type.AttributeWildcard)).IsTrue();
        await Assert.That(_f.Issues).Count().IsEqualTo(0);
    }

    [Test]
    public async Task Extension_OfSimpleContent_KeepsTheSimpleTypeAndAddsAttributes()
    {
        var parent = _f.SimpleContent("a", Ext, TypeFixture.Xs("string"));

        var type = _f.SimpleContent("b", Ext, parent, null, [_f.Use("q")]);
        TypeFixture.Checker().Report(type, _f.Reporter);

        await Assert.That(_f.Issues).Count().IsEqualTo(0);
    }

    [Test]
    public async Task Extension_OfASimpleType_KeepsThatType()
    {
        var type = _f.SimpleContent("b", Ext, TypeFixture.Xs("int"));
        TypeFixture.Checker().Report(type, _f.Reporter);

        await Assert.That(_f.Issues).Count().IsEqualTo(0);
    }

    [Test]
    [Arguments(0, false)]
    [Arguments(1, true)]
    [Arguments(2, false)]
    [Arguments(3, true)]
    public async Task Extension_OfABaseThatIsFinalForExtension_IsALocatedSchemaError(int final, bool error)
    {
        var parent = _f.Root("a", TypeFixture.Sequence(ElementParticle("x")), final: (XsdDerivationMethod)final);
        var type = _f.Complex("b", Ext, parent, TypeFixture.Sequence(ElementParticle("y")));

        TypeFixture.Checker().Report(type, _f.Reporter);

        if (error)
        {
            await AssertSingle("cos-ct-extends", "cos-ct-extends.1.1");
        }
        else
        {
            await Assert.That(_f.Issues).Count().IsEqualTo(0);
        }
    }

    [Test]
    public async Task Extension_FromElementOnlyToMixed_IsAnError()
    {
        var parent = _f.Root("a", TypeFixture.Sequence(ElementParticle("x")));
        var type = _f.Complex("b", Ext, parent, TypeFixture.Sequence(ElementParticle("y")), mixed: true);

        TypeFixture.Checker().Report(type, _f.Reporter);

        await AssertSingle("cos-ct-extends", "cos-ct-extends.1.4.3.2.2.1");
    }

    [Test]
    public async Task Extension_FromMixedToElementOnly_IsAnError()
    {
        var parent = _f.Root("a", TypeFixture.Sequence(ElementParticle("x")), mixed: true);
        var type = _f.Complex("b", Ext, parent, TypeFixture.Sequence(ElementParticle("y")));

        TypeFixture.Checker().Report(type, _f.Reporter);

        await AssertSingle("cos-ct-extends", "cos-ct-extends.1.4.3.2.2.1");
    }

    [Test]
    public async Task Extension_WithElementContent_AgainstASimpleContentBase_IsAnError()
    {
        var other = _f.SimpleContent("o", Ext, TypeFixture.Xs("string"));
        var type = _f.Complex("d", Ext, _f.Root("a"), TypeFixture.Sequence(ElementParticle("y")));

        TypeFixture.Checker().Report(type, other, _f.Reporter);

        await AssertSingle("cos-ct-extends", "cos-ct-extends.1.4.3.2.2.1");
    }

    [Test]
    public async Task Extension_AgainstABaseWhoseAttributeUseIsMissing_IsAnError()
    {
        var other = _f.Root("o", null, false, [_f.Use("p")]);
        var type = _f.Complex("d", Ext, _f.Root("a"), null, false, [_f.Use("q")]);

        TypeFixture.Checker().Report(type, other, _f.Reporter);

        await AssertSingle("cos-ct-extends", "cos-ct-extends.1.2");
    }

    [Test]
    public async Task Extension_AgainstABaseWhoseAttributeHasAnotherType_IsAnError()
    {
        var other = _f.Root("o", null, false, [_f.Use("p", type: "int")]);
        var type = _f.Complex("d", Ext, _f.Root("a"), null, false, [_f.Use("p", type: "string")]);

        TypeFixture.Checker().Report(type, other, _f.Reporter);

        await AssertSingle("cos-ct-extends", "cos-ct-extends.1.2");
    }

    [Test]
    public async Task Extension_AgainstABaseWhoseAttributeHasTheSameTypeByName_IsValid()
    {
        var other = _f.Root("o", null, false, [_f.Use("p", type: "int")]);
        var type = _f.Complex("d", Ext, _f.Root("a"), null, false, [_f.Use("p", type: "int")]);

        TypeFixture.Checker().Report(type, other, _f.Reporter);

        await Assert.That(_f.Issues).Count().IsEqualTo(0);
    }

    [Test]
    public async Task Extension_AgainstABaseWithAWildcard_NeedsAWildcard()
    {
        var other = _f.Root("o", null, false, null, TypeFixture.Wild(TypeFixture.Namespaces("urn:a")));
        var type = _f.Complex("d", Ext, _f.Root("a"));

        TypeFixture.Checker().Report(type, other, _f.Reporter);

        await AssertSingle("cos-ct-extends", "cos-ct-extends.1.3");
    }

    [Test]
    public async Task Extension_AgainstABaseWithAWiderWildcard_IsAnError()
    {
        var other = _f.Root("o", null, false, null, TypeFixture.Wild(TypeFixture.Namespaces("urn:a", "urn:b")));
        var type = _f.Complex(
            "d",
            Ext,
            _f.Root("a"),
            null,
            false,
            null,
            TypeFixture.Wild(TypeFixture.Namespaces("urn:a"))
        );

        TypeFixture.Checker().Report(type, other, _f.Reporter);

        await AssertSingle("cos-ct-extends", "cos-ct-extends.1.3");
    }

    [Test]
    public async Task Extension_OfSimpleContent_AgainstAnotherSimpleType_IsAnError()
    {
        var other = _f.SimpleContent("o", Ext, TypeFixture.Xs("int"));
        var type = _f.SimpleContent("d", Ext, TypeFixture.Xs("string"));

        TypeFixture.Checker().Report(type, other, _f.Reporter);

        await AssertSingle("cos-ct-extends", "cos-ct-extends.1.4");
    }

    [Test]
    public async Task Extension_OfSimpleContent_AgainstElementContent_IsAnError()
    {
        var other = _f.Root("o", TypeFixture.Sequence(ElementParticle("x")));
        var type = _f.SimpleContent("d", Ext, TypeFixture.Xs("string"));

        TypeFixture.Checker().Report(type, other, _f.Reporter);

        await AssertSingle("cos-ct-extends", "cos-ct-extends.1.4");
    }

    [Test]
    public async Task Extension_AgainstABaseWithOtherContent_IsAnError()
    {
        var other = _f.Root("o", TypeFixture.Sequence(ElementParticle("z")));
        var type = _f.Complex(
            "d",
            Ext,
            _f.Root("a", TypeFixture.Sequence(ElementParticle("x"))),
            TypeFixture.Sequence(ElementParticle("y"))
        );

        TypeFixture.Checker().Report(type, other, _f.Reporter);

        await AssertSingle("cos-ct-extends", "cos-ct-extends.1.4.3.2.2.2");
    }

    [Test]
    public async Task Extension_AgainstABaseWithIdenticalContent_IsValidEvenForAnotherParticleObject()
    {
        var term = new FakeComponent(XsdComponentKind.ElementDeclaration, "x");
        var other = _f.Root("o", TypeFixture.Sequence(Particle.Create(TypeFixture.Where, 1, 1, term)));
        var type = _f.Complex(
            "d",
            Ext,
            _f.Root("a", TypeFixture.Sequence(Particle.Create(TypeFixture.Where, 1, 1, term))),
            TypeFixture.Sequence(ElementParticle("y"))
        );

        TypeFixture.Checker().Report(type, other, _f.Reporter);

        await Assert.That(_f.Issues).Count().IsEqualTo(0);
    }

    [Test]
    public async Task Extension_AgainstASimpleBase_NeedsThatSimpleContentType()
    {
        var type = _f.SimpleContent("d", Ext, TypeFixture.Xs("string"));

        TypeFixture.Checker().Report(type, TypeFixture.Xs("int"), _f.Reporter);

        await AssertSingle("cos-ct-extends", "cos-ct-extends.2.1");
    }

    [Test]
    public async Task Check_ExtensionMethod_OfAnotherSimpleBase_NamesTheExtensionRule()
    {
        var type = _f.SimpleContent("d", Ext, TypeFixture.Xs("string"));

        var result = TypeFixture.Checker().Check(type, TypeFixture.Xs("int"), Ext, None);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.ViolatedRule!.Id).IsEqualTo("cos-ct-extends");
    }

    [Test]
    public async Task Check_ExtensionMethod_OfASimpleDerivedType_IsInvalid()
    {
        var result = TypeFixture.Checker().Check(TypeFixture.Xs("int"), TypeFixture.Xs("decimal"), Ext, None);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.ViolatedRule!.Id).IsEqualTo("cos-ct-extends");
    }

    [Test]
    public async Task Check_ExtensionMethod_IgnoresTheBlockSet()
    {
        var parent = _f.Root("a", TypeFixture.Sequence(ElementParticle("x")));
        var type = _f.Complex("b", Ext, parent, TypeFixture.Sequence(ElementParticle("y")));

        await Assert.That(TypeFixture.Checker().Check(type, parent, Ext, Ext | Res).IsValid).IsTrue();
    }

    [Test]
    [Arguments(XsdVersion.V10)]
    [Arguments(XsdVersion.V11)]
    public async Task Extension_IsCheckedAlikeInBothVersions(XsdVersion version)
    {
        var parent = _f.Root("a", TypeFixture.Sequence(ElementParticle("x")), false, [_f.Use("p", type: "int")]);
        var valid = _f.Complex("b", Ext, parent, TypeFixture.Sequence(ElementParticle("y")));
        var invalid = _f.Complex("c", Ext, parent, TypeFixture.Sequence(ElementParticle("y")), mixed: true);

        var check = TypeFixture.Checker(version);

        await Assert.That(check.Check(valid, parent, Ext, None).IsValid).IsTrue();
        await Assert.That(check.Check(invalid, parent, Ext, None).IsValid).IsFalse();
    }
}
