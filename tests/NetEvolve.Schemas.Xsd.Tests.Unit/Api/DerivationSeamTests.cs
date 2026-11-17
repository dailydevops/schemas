namespace NetEvolve.Schemas.Xsd.Tests.Unit.Api;

using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;

public class DerivationSeamTests
{
    private static readonly XsdRule Rule = FakeRules.Find(XsdRuleCodes.CosCtDerivedOk);

    [Test]
    public async Task Seam_Derivation_ReceivesTypesMethodAndBlockSetInOrder()
    {
        var derived = new FakeComponent(XsdComponentKind.ComplexType, "child");
        var baseType = new FakeComponent(XsdComponentKind.ComplexType, "parent");
        var fake = new FakeDerivationCheck(Rule);
        var check = fake;

        var first = check.Check(derived, baseType, XsdDerivationMethod.Extension, XsdDerivationMethod.None);
        var second = check.Check(derived, baseType, XsdDerivationMethod.Restriction, XsdDerivationMethod.Restriction);

        await Assert.That(fake.Calls).Count().IsEqualTo(2);
        await Assert.That(fake.Calls[0].Derived).IsSameReferenceAs(derived);
        await Assert.That(fake.Calls[0].Base).IsSameReferenceAs(baseType);
        await Assert.That(fake.Calls[0].Method).IsEqualTo(XsdDerivationMethod.Extension);
        await Assert.That(fake.Calls[0].Block).IsEqualTo(XsdDerivationMethod.None);
        await Assert.That(fake.Calls[1].Method).IsEqualTo(XsdDerivationMethod.Restriction);
        await Assert.That(fake.Calls[1].Block).IsEqualTo(XsdDerivationMethod.Restriction);
        await Assert.That(first.IsValid).IsTrue();
        await Assert.That(first.ViolatedRule).IsNull();
        await Assert.That(second.IsValid).IsFalse();
    }

    [Test]
    public async Task Seam_Derivation_InvalidResult_NamesTheViolatedRule()
    {
        var check = new FakeDerivationCheck(Rule);
        var type = new FakeComponent(XsdComponentKind.ComplexType, "t");

        var result = check.Check(type, type, XsdDerivationMethod.Extension, XsdDerivationMethod.Extension);

        await Assert.That(result.ViolatedRule).IsSameReferenceAs(Rule);
        await Assert.That(result.ViolatedRule!.Id).IsEqualTo("cos-ct-derived-ok");
    }

    [Test]
    public async Task DerivationResult_Default_IsValid()
    {
        await Assert.That(default(DerivationResult).IsValid).IsTrue();
        await Assert.That(DerivationResult.Valid.IsValid).IsTrue();
        await Assert.That(DerivationResult.Invalid(Rule).IsValid).IsFalse();
    }
}
