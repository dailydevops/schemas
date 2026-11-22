namespace NetEvolve.Schemas.Xsd.Tests.Unit.TypeAlternatives;

using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Types;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;
using NetEvolve.Schemas.Xsd.TypeAlternatives;

public class TypeTableTests
{
    [Test]
    public async Task AnAlternativeTypeThatIsNeitherXsErrorNorDerivedFromTheDeclaredType_IsASchemaError()
    {
        var f = new AlternativeFixture();
        var table = f.Table("<xs:alternative test=\"@k\" type=\"t:x\"/>")!;

        table.Check(f.Declared, TypeFixture.Checker(), f.Reporter);

        await Assert.That(f.Issues.Count).IsEqualTo(1);
        await Assert.That(f.Issues[0].Code).IsEqualTo(XsdRuleCodes.TaPropsCorrect);
        await Assert.That(f.Issues[0].Location.Line).IsGreaterThan(0);
    }

    [Test]
    public async Task TheDefaultAlternative_IsCheckedLikeTheOthers()
    {
        var f = new AlternativeFixture();
        var table = f.Table("<xs:alternative test=\"@k\" type=\"t:a\"/><xs:alternative type=\"t:x\"/>")!;

        table.Check(f.Declared, TypeFixture.Checker(), f.Reporter);

        await Assert.That(f.Issues.Count).IsEqualTo(1);
        await Assert.That(f.Issues[0].Code).IsEqualTo(XsdRuleCodes.TaPropsCorrect);
    }

    [Test]
    public async Task AnAlternativeTypeDerivedFromTheDeclaredType_Xs_ErrorAndTheDeclaredTypeItself_AreValid()
    {
        var f = new AlternativeFixture();
        var table = f.Table(
            "<xs:alternative test=\"@k = 'a'\" type=\"t:a\"/>"
                + "<xs:alternative test=\"@k = 'e'\" type=\"xs:error\"/>"
                + "<xs:alternative test=\"@k = 'same'\" type=\"xs:string\"/>"
        );

        table!.Check(f.Declared, TypeFixture.Checker(), f.Reporter);

        await Assert.That(f.Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task AnAlternativeWhoseTypeIsNotBoundYet_IsSkippedByTheCheck()
    {
        var f = new AlternativeFixture();
        var table = TypeTable.FromSyntax(
            AlternativeFixture.Element("<xs:alternative test=\"@k\" type=\"t:a\"/>"),
            AlternativeFixture.Builder(),
            f.Reporter
        )!;

        table.Check(f.Declared, TypeFixture.Checker(), f.Reporter);

        await Assert.That(table[0].TypeDefinition).IsNull();
        await Assert.That(f.Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task AnElementWithoutAlternatives_HasNoTable()
    {
        var f = new AlternativeFixture();

        await Assert.That(f.Table(string.Empty)).IsNull();
    }

    [Test]
    public async Task TheLastAlternativeWithoutATest_IsTheDefault_AndTheOthersKeepTheirOrder()
    {
        var f = new AlternativeFixture();
        var table = f.Table(
            "<xs:alternative test=\"@one\" type=\"t:a\"/><xs:alternative test=\"@two\" type=\"t:b\"/><xs:alternative type=\"t:c\"/>"
        )!;

        await Assert.That(table.Count).IsEqualTo(2);
        await Assert.That(table[0].Test!.Source).IsEqualTo("@one");
        await Assert.That(table[1].Test!.Source).IsEqualTo("@two");
        await Assert.That(table.Default!.IsDefault).IsTrue();
        await Assert.That(table.Default.Test).IsNull();
        await Assert.That(f.Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task AnAlternativeWithoutATestThatIsNotLast_IsReported_AndLeftOut()
    {
        var f = new AlternativeFixture();
        var table = f.Table("<xs:alternative type=\"t:a\"/><xs:alternative test=\"@k\" type=\"t:b\"/>")!;

        await Assert.That(f.Issues.Count).IsEqualTo(1);
        await Assert.That(f.Issues[0].Code).IsEqualTo(XsdRuleCodes.EPropsCorrect);
        await Assert.That(table.Count).IsEqualTo(1);
        await Assert.That(table.Default).IsNull();
    }

    [Test]
    public async Task ATestThatIsNotValidXPath_IsASchemaError_AndNeverMatches()
    {
        var f = new AlternativeFixture();
        var table = f.Table(
            "<xs:alternative test=\"@k =\" type=\"t:a\"/><xs:alternative test=\"undeclared:k\" type=\"t:b\"/>"
        )!;

        var selection = f.Select(table, Attr.Of("k", "1"));

        await Assert.That(f.Issues.Count).IsEqualTo(2);
        await Assert.That(f.Issues[0].Code).IsEqualTo(XsdRuleCodes.XpathValid);
        await Assert.That(f.Issues[1].Code).IsEqualTo(XsdRuleCodes.XpathValid);
        await Assert.That(table.Count).IsEqualTo(2);
        await Assert.That(table.Default).IsNull();
        await Assert.That(selection.Type).IsSameReferenceAs(f.Declared);
    }

    [Test]
    public async Task AnAlternativeWithBothATypeAttributeAndAnAnonymousType_IsASchemaError()
    {
        var f = new AlternativeFixture();
        var table = f.Table(
            "<xs:alternative test=\"@k\" type=\"t:a\"><xs:simpleType><xs:restriction base=\"xs:string\"/></xs:simpleType></xs:alternative>"
        )!;

        await Assert.That(f.Issues.Count).IsEqualTo(1);
        await Assert.That(f.Issues[0].Code).IsEqualTo(XsdRuleCodes.SrcTa);
        await Assert.That(table[0].ReferenceCount).IsEqualTo(1);
    }

    [Test]
    public async Task AnAlternativeWithNeitherATypeAttributeNorAnAnonymousType_IsASchemaError()
    {
        var f = new AlternativeFixture();
        var table = f.Table("<xs:alternative test=\"@k\"/>")!;

        await Assert.That(f.Issues.Count).IsEqualTo(1);
        await Assert.That(f.Issues[0].Code).IsEqualTo(XsdRuleCodes.SrcTa);
        await Assert.That(table[0].ReferenceCount).IsEqualTo(0);
        await Assert.That(table[0].InlineTypeSyntax).IsNull();
    }

    [Test]
    public async Task AnAnonymousType_StaysAsSyntaxUntilItIsBound()
    {
        var f = new AlternativeFixture();
        var table = f.Table(
            "<xs:alternative test=\"@k\"><xs:simpleType><xs:restriction base=\"xs:string\"/></xs:simpleType></xs:alternative>"
        )!;
        var alternative = table[0];

        await Assert.That(f.Issues.Count).IsEqualTo(0);
        await Assert.That(alternative.ReferenceCount).IsEqualTo(0);
        await Assert.That(alternative.InlineTypeSyntax).IsNotNull();
        await Assert.That(alternative.TypeDefinition).IsNull();

        alternative.BindInlineType(f.Type("a"));

        await Assert.That(alternative.TypeDefinition).IsSameReferenceAs(f.Type("a"));
        await Assert.That(alternative.Kind).IsEqualTo(XsdComponentKind.TypeAlternative);
    }

    [Test]
    public async Task ATypeAttributeWithAnUndeclaredPrefix_IsAResolveError()
    {
        var f = new AlternativeFixture();
        _ = f.Table("<xs:alternative test=\"@k\" type=\"nope:a\"/>");

        await Assert.That(f.Issues.Count).IsEqualTo(1);
        await Assert.That(f.Issues[0].Code).IsEqualTo(XsdRuleCodes.SrcResolve);
    }

    [Test]
    public async Task TheTypeAttribute_BecomesAReferenceToTheNamedType()
    {
        var f = new AlternativeFixture();
        var table = f.Table("<xs:alternative test=\"@k\" type=\"t:a\"/>")!;

        var reference = table[0].GetReference(0);

        await Assert.That(reference.Name.NamespaceName).IsEqualTo("urn:test");
        await Assert.That(reference.Name.LocalName).IsEqualTo("a");
        await Assert.That(table[0].Location.Line).IsGreaterThan(0);
    }

    [Test]
    public async Task UnprefixedNamesInTheTest_FollowXpathDefaultNamespace()
    {
        var f = new AlternativeFixture();
        var own = f.Table("<xs:alternative test=\"self::e\" xpathDefaultNamespace=\"urn:test\" type=\"t:a\"/>")!;
        var inherited = f.Table(
            "<xs:alternative test=\"self::e\" type=\"t:a\"/>",
            "xpathDefaultNamespace=\"##targetNamespace\""
        )!;
        var none = f.Table("<xs:alternative test=\"self::e\" type=\"t:a\"/>")!;
        var local = f.Table(
            "<xs:alternative test=\"self::e\" xpathDefaultNamespace=\"##local\" type=\"t:a\"/>",
            "xpathDefaultNamespace=\"urn:test\""
        )!;

        await Assert.That(f.Select(own).Type).IsSameReferenceAs(f.Type("a"));
        await Assert.That(f.Select(inherited).Type).IsSameReferenceAs(f.Type("a"));
        await Assert.That(f.Select(none).Type).IsSameReferenceAs(f.Declared);
        await Assert.That(f.Select(local).Type).IsSameReferenceAs(f.Declared);
        await Assert.That(f.Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task APrefixInTheTest_ResolvesThroughTheDeclarationsInScopeAtTheAlternative()
    {
        var f = new AlternativeFixture();
        var table = f.Table(
            "<xs:alternative test=\"@p:k = 'v' and xs:string(@p:k) = 'v'\" type=\"t:a\" xmlns:p=\"urn:p\"/>"
        )!;

        var selection = f.Select(table, new Attr("urn:p", "k", "v"));

        await Assert.That(f.Issues.Count).IsEqualTo(0);
        await Assert.That(selection.Type).IsSameReferenceAs(f.Type("a"));
    }

    [Test]
    public async Task TheBuilder_BuildsThroughTheComponentBuilderSeam_WithoutRegisteringTheAlternative()
    {
        var f = new AlternativeFixture();
        var element = AlternativeFixture.Element("<xs:alternative test=\"@k\" type=\"t:a\"/>");
        var registry = new FakeComponentRegistry([]);

        var component = AlternativeFixture.Builder().Build(element.GetChildNode(0), registry, f.Reporter);

        await Assert.That(component.Kind).IsEqualTo(XsdComponentKind.TypeAlternative);
        await Assert.That(AlternativeFixture.Builder().Kind).IsEqualTo(XsdComponentKind.TypeAlternative);
        await Assert.That(registry.Count).IsEqualTo(0);
    }
}
