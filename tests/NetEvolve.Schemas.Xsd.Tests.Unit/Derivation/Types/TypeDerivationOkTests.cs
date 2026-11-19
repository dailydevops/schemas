namespace NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Types;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Contracts;

public class TypeDerivationOkTests
{
    private const XsdDerivationMethod None = XsdDerivationMethod.None;
    private const XsdDerivationMethod Ext = XsdDerivationMethod.Extension;
    private const XsdDerivationMethod Res = XsdDerivationMethod.Restriction;
    private const XsdDerivationMethod Sub = XsdDerivationMethod.Substitution;

    private readonly TypeFixture _f = new();

    // a <- b (extension) <- c (restriction)
    private (ComplexTypeDefinition A, ComplexTypeDefinition B, ComplexTypeDefinition C) Chain()
    {
        var a = _f.Root("a", TypeFixture.Sequence(TypeFixture.Element("x")));
        var b = _f.Complex("b", Ext, a, TypeFixture.Sequence(TypeFixture.Element("y")));
        var c = _f.Complex("c", Res, b, b.ContentParticle);
        return (a, b, c);
    }

    private static DerivationResult Substitutable(
        IXsdComponent derived,
        IXsdComponent baseType,
        XsdDerivationMethod block
    ) => TypeFixture.Checker().Check(derived, baseType, Sub, (XsdDerivationMethod)block);

    [Test]
    [Arguments(0, true)]
    [Arguments(1, false)]
    [Arguments(2, false)]
    [Arguments(3, false)]
    public async Task Complex_TwoStepChain_EveryStepMustBeUnblocked(int block, bool valid)
    {
        var (a, _, c) = Chain();

        await Assert.That(Substitutable(c, a, (XsdDerivationMethod)block).IsValid).IsEqualTo(valid);
    }

    [Test]
    [Arguments(0, true)]
    [Arguments(1, false)]
    [Arguments(2, true)]
    [Arguments(3, false)]
    public async Task Complex_ExtensionStep_IsBlockedByExtension(int block, bool valid)
    {
        var (a, b, _) = Chain();

        await Assert.That(Substitutable(b, a, (XsdDerivationMethod)block).IsValid).IsEqualTo(valid);
    }

    [Test]
    [Arguments(0, true)]
    [Arguments(1, true)]
    [Arguments(2, false)]
    [Arguments(3, false)]
    public async Task Complex_RestrictionStep_IsBlockedByRestriction(int block, bool valid)
    {
        var (_, b, c) = Chain();

        await Assert.That(Substitutable(c, b, (XsdDerivationMethod)block).IsValid).IsEqualTo(valid);
    }

    [Test]
    public async Task Complex_SameType_IsValidWhateverIsBlocked()
    {
        var (a, _, _) = Chain();

        await Assert.That(Substitutable(a, a, Ext | Res).IsValid).IsTrue();
    }

    [Test]
    public async Task Complex_BaseAsDerived_IsInvalidAndNamesTheComplexRule()
    {
        var (a, _, c) = Chain();

        var result = Substitutable(a, c, None);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.ViolatedRule!.Id).IsEqualTo("cos-ct-derived-ok");
    }

    [Test]
    public async Task Complex_Siblings_AreNotDerivedFromEachOther()
    {
        var left = _f.Root("left");
        var right = _f.Root("right");

        await Assert.That(Substitutable(left, right, None).IsValid).IsFalse();
    }

    [Test]
    public async Task Complex_EveryTypeIsDerivedFromTheUrType_UnderBothIdentitiesOfIt()
    {
        var (a, _, c) = Chain();

        await Assert.That(Substitutable(c, ComplexTypeDefinition.AnyType, None).IsValid).IsTrue();
        await Assert.That(Substitutable(c, TypeFixture.Registered("anyType"), None).IsValid).IsTrue();
        await Assert.That(Substitutable(a, TypeFixture.Registered("anyType"), Ext).IsValid).IsTrue();
        await Assert.That(Substitutable(a, TypeFixture.Registered("anyType"), Res).IsValid).IsFalse();
    }

    [Test]
    public async Task Complex_TheUrType_IsDerivedFromNothingElse()
    {
        var (a, _, _) = Chain();

        await Assert.That(Substitutable(ComplexTypeDefinition.AnyType, a, None).IsValid).IsFalse();
        await Assert.That(Substitutable(TypeFixture.Registered("anyType"), a, None).IsValid).IsFalse();
    }

    [Test]
    [Arguments(0, true)]
    [Arguments(1, true)]
    [Arguments(2, false)]
    [Arguments(3, false)]
    [Arguments(4, true)]
    [Arguments(8, true)]
    [Arguments(12, true)]
    [Arguments(15, false)]
    public async Task Simple_RestrictionChain_IsBlockedByRestrictionOnly(int block, bool valid)
    {
        var myInt = _f.Restrict("myInt", TypeFixture.Xs("int"));

        await Assert
            .That(Substitutable(myInt, TypeFixture.Xs("decimal"), (XsdDerivationMethod)block).IsValid)
            .IsEqualTo(valid);
    }

    [Test]
    [Arguments(XsdVersion.V10)]
    [Arguments(XsdVersion.V11)]
    public async Task Simple_BuiltInChain_IsFollowedByName_ForAWrapperAndForTheRegisteredType(XsdVersion version)
    {
        var check = TypeFixture.Checker(version);
        var myInt = _f.Restrict("myInt", TypeFixture.Xs("int"));

        await Assert.That(check.Check(myInt, TypeFixture.Registered("decimal"), Sub, None).IsValid).IsTrue();
        await Assert
            .That(check.Check(TypeFixture.Xs("int"), TypeFixture.Registered("integer"), Sub, None).IsValid)
            .IsTrue();
        await Assert
            .That(check.Check(TypeFixture.Registered("int"), TypeFixture.Xs("decimal"), Sub, None).IsValid)
            .IsTrue();
        await Assert.That(check.Check(myInt, TypeFixture.Xs("anySimpleType"), Sub, None).IsValid).IsTrue();
        await Assert.That(check.Check(myInt, TypeFixture.Registered("anyType"), Sub, None).IsValid).IsTrue();
    }

    [Test]
    public async Task Simple_UnrelatedBuiltIn_IsInvalidAndNamesTheSimpleRule()
    {
        var result = Substitutable(TypeFixture.Xs("int"), TypeFixture.Xs("string"), None);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.ViolatedRule!.Id).IsEqualTo("cos-st-derived-ok");
    }

    [Test]
    public async Task Simple_BaseAsDerived_IsInvalid() =>
        await Assert.That(Substitutable(TypeFixture.Xs("decimal"), TypeFixture.Xs("int"), None).IsValid).IsFalse();

    [Test]
    public async Task Simple_ComplexBase_OtherThanTheUrType_IsInvalid()
    {
        var (a, _, _) = Chain();

        await Assert.That(Substitutable(TypeFixture.Xs("int"), a, None).IsValid).IsFalse();
    }

    [Test]
    [Arguments(0, true)]
    [Arguments(2, false)]
    public async Task Simple_List_IsDerivedFromTheSimpleUrType(int block, bool valid)
    {
        var list = _f.ListOf("ints", TypeFixture.Xs("int"));

        await Assert
            .That(Substitutable(list, TypeFixture.Xs("anySimpleType"), (XsdDerivationMethod)block).IsValid)
            .IsEqualTo(valid);
        await Assert.That(Substitutable(list, TypeFixture.Xs("int"), (XsdDerivationMethod)block).IsValid).IsFalse();
    }

    [Test]
    [Arguments(0, true)]
    [Arguments(2, false)]
    public async Task Simple_Union_IsDerivedFromTheSimpleUrType(int block, bool valid)
    {
        var union = _f.UnionOf("either", TypeFixture.Xs("int"), TypeFixture.Xs("string"));

        await Assert
            .That(Substitutable(union, TypeFixture.Xs("anySimpleType"), (XsdDerivationMethod)block).IsValid)
            .IsEqualTo(valid);
    }

    [Test]
    [Arguments("int", 0, true)]
    [Arguments("short", 0, true)]
    [Arguments("boolean", 0, false)]
    [Arguments("int", 2, false)]
    public async Task Simple_UnionBase_IsDerivedFromItsMemberTypes(string derived, int block, bool valid)
    {
        var union = _f.UnionOf("either", TypeFixture.Xs("int"), TypeFixture.Xs("string"));

        await Assert
            .That(Substitutable(TypeFixture.Xs(derived), union, (XsdDerivationMethod)block).IsValid)
            .IsEqualTo(valid);
    }

    [Test]
    public async Task Simple_UnionBase_IsDerivedFromTheMemberTypesOfARestrictedUnion()
    {
        var union = _f.UnionOf("either", TypeFixture.Xs("int"), TypeFixture.Xs("string"));
        var narrowed = _f.Restrict("narrowed", union);
        var myInt = _f.Restrict("myInt", TypeFixture.Xs("int"));

        await Assert.That(Substitutable(myInt, union, None).IsValid).IsTrue();
        await Assert.That(Substitutable(narrowed, union, None).IsValid).IsTrue();
    }

    [Test]
    [Arguments(0, true)]
    [Arguments(1, false)]
    [Arguments(2, true)]
    public async Task Complex_SimpleContentExtension_IsDerivedFromItsSimpleBase(int block, bool valid)
    {
        var type = _f.SimpleContent("sc", Ext, TypeFixture.Xs("string"));

        await Assert
            .That(Substitutable(type, TypeFixture.Xs("string"), (XsdDerivationMethod)block).IsValid)
            .IsEqualTo(valid);
    }

    [Test]
    public async Task Complex_SimpleContentExtension_ContinuesIntoTheSimpleChainAndEndsAtTheUrType()
    {
        var type = _f.SimpleContent("sc", Ext, TypeFixture.Xs("int"));

        await Assert.That(Substitutable(type, TypeFixture.Xs("decimal"), None).IsValid).IsTrue();
        await Assert.That(Substitutable(type, TypeFixture.Xs("decimal"), Res).IsValid).IsFalse();
        await Assert.That(Substitutable(type, ComplexTypeDefinition.AnyType, None).IsValid).IsTrue();
        await Assert.That(Substitutable(type, TypeFixture.Xs("string"), None).IsValid).IsFalse();
    }

    [Test]
    public async Task Check_BlockSetBitsBeyondTheTypeDerivations_AreIgnored()
    {
        var (a, _, c) = Chain();

        await Assert
            .That(Substitutable(c, a, XsdDerivationMethod.Substitution | XsdDerivationMethod.List).IsValid)
            .IsTrue();
    }

    [Test]
    public async Task Check_Method_IsOnlyTheContext_TheBlockSetDecides()
    {
        var (a, b, _) = Chain();
        var check = TypeFixture.Checker();

        await Assert.That(check.Check(b, a, None, None).IsValid).IsTrue();
        await Assert.That(check.Check(b, a, XsdDerivationMethod.List, Ext).IsValid).IsFalse();
        await Assert.That(check.Check(b, a, Sub, Ext).IsValid).IsFalse();
    }

    [Test]
    public async Task Complex_FinalOfTheBase_IsNotPartOfTypeDerivationOk_ItIsCheckedWhenTheTypeIsDefined()
    {
        var parent = _f.Root("a", TypeFixture.Sequence(TypeFixture.Element("x")), final: Res);
        var type = _f.Complex("c", Res, parent, TypeFixture.Sequence(TypeFixture.Element("x")));

        await Assert.That(Substitutable(type, parent, None).IsValid).IsTrue();

        TypeFixture.Checker().Report(type, _f.Reporter);
        await Assert.That(_f.Issues).Count().IsEqualTo(1);
    }
}
