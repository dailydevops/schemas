namespace NetEvolve.Schemas.Xsd.Tests.Unit.Declarations11;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.Declarations11;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;
using NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Types;

public class IdAttributeUsesTests
{
    private readonly TypeFixture _f = new();
    private readonly ValidationIssueCollector _collector = new(ValidationLimits.Default);

    private XsdIssueReporter Reporter => new(_collector);

    private IReadOnlyList<ValidationIssue> Issues => _collector.ToResult().Issues;

    private AttributeUse Use(
        string name,
        SimpleTypeDefinition type,
        int line,
        AttributeUseKind kind = AttributeUseKind.Optional
    )
    {
        var location = new DocumentLocation("file:///a.xsd", line, 1);
        var declaration = AttributeDeclaration.Global(name, "urn:test", location, type, default, Reporter)!;
        return AttributeUse.Create(location, kind, declaration, default, Reporter)!;
    }

    [Test]
    public async Task CheckType_Strict10_TwoIdAttributes_IsALocatedSchemaErrorAtTheSecond()
    {
        var uses = new[] { Use("a", TypeFixture.Xs("ID"), 3), Use("b", TypeFixture.Xs("ID"), 4) };

        IdAttributeUses.CheckType(uses, XsdVersion.V10, Reporter);

        await Assert.That(Issues.Count).IsEqualTo(1);
        await Assert.That(Issues[0].Code).IsEqualTo(XsdRuleCodes.CtPropsCorrect);
        await Assert.That(Issues[0].Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(Issues[0].Constraint).IsEqualTo("ct-props-correct.5");
        await Assert.That(Issues[0].Location).IsEqualTo(uses[1].Location);
    }

    [Test]
    public async Task CheckType_Strict10_EveryFurtherIdAttributeIsReported()
    {
        var id = TypeFixture.Xs("ID");
        var uses = new[] { Use("a", id, 3), Use("b", id, 4), Use("c", id, 5) };

        IdAttributeUses.CheckType(uses, XsdVersion.V10, Reporter);

        await Assert.That(Issues.Count).IsEqualTo(2);
        await Assert.That(Issues[0].Location).IsEqualTo(uses[1].Location);
        await Assert.That(Issues[1].Location).IsEqualTo(uses[2].Location);
    }

    [Test]
    public async Task CheckType_Strict10_ATypeDerivedFromIdByRestriction_Counts()
    {
        var derived = _f.Restrict("limited", _f.Restrict("shorter", TypeFixture.Xs("ID")));
        var uses = new[] { Use("a", derived, 3), Use("b", TypeFixture.Xs("ID"), 4) };

        IdAttributeUses.CheckType(uses, XsdVersion.V10, Reporter);

        await Assert.That(Issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task CheckType_Strict10_OtherTypesAndProhibitedUsesDoNotCount()
    {
        var uses = new[]
        {
            Use("a", TypeFixture.Xs("ID"), 3),
            Use("b", TypeFixture.Xs("IDREF"), 4),
            Use("c", _f.ListOf("ids", TypeFixture.Xs("ID")), 5),
            Use("d", _f.UnionOf("either", TypeFixture.Xs("ID"), TypeFixture.Xs("string")), 6),
            Use("e", _f.Restrict("name", TypeFixture.Xs("NCName")), 7),
            Use("f", TypeFixture.Xs("ID"), 8, AttributeUseKind.Prohibited),
        };

        IdAttributeUses.CheckType(uses, XsdVersion.V10, Reporter);

        await Assert.That(Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task CheckType_Version11_SeveralIdAttributesAreAccepted()
    {
        var id = TypeFixture.Xs("ID");
        var uses = new[] { Use("a", id, 3), Use("b", id, 4), Use("c", _f.Restrict("limited", id), 5) };

        IdAttributeUses.CheckType(uses, XsdVersion.V11, Reporter);

        await Assert.That(Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task CheckType_AComplexTypeWithTwoIdAttributes_CompilesInVersion11_AndIsAnErrorInStrict10()
    {
        var type = _f.Root("c", uses: [_f.Use("a", type: "ID"), _f.Use("b", type: "ID")]);

        IdAttributeUses.CheckType(type.AttributeUses, XsdVersion.V11, Reporter);
        var lax = Issues.Count;
        IdAttributeUses.CheckType(type.AttributeUses, XsdVersion.V10, Reporter);

        await Assert.That(_f.BuildIssues.Count).IsEqualTo(0);
        await Assert.That(type.AttributeUses.Count).IsEqualTo(2);
        await Assert.That(lax).IsEqualTo(0);
        await Assert.That(Issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task CheckGroup_Strict10_TwoIdAttributes_IsALocatedSchemaErrorAtTheSecond()
    {
        var uses = new[] { Use("a", TypeFixture.Xs("ID"), 3), Use("b", TypeFixture.Xs("ID"), 4) };

        IdAttributeUses.CheckGroup(uses, XsdVersion.V10, Reporter);

        await Assert.That(Issues.Count).IsEqualTo(1);
        await Assert.That(Issues[0].Code).IsEqualTo(XsdRuleCodes.AgPropsCorrect);
        await Assert.That(Issues[0].Constraint).IsEqualTo("ag-props-correct.3");
        await Assert.That(Issues[0].Location).IsEqualTo(uses[1].Location);
    }

    [Test]
    public async Task CheckGroup_Version11_SeveralIdAttributesAreAccepted()
    {
        var uses = new[] { Use("a", TypeFixture.Xs("ID"), 3), Use("b", TypeFixture.Xs("ID"), 4) };

        IdAttributeUses.CheckGroup(uses, XsdVersion.V11, Reporter);

        await Assert.That(Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task IsIdType_FollowsTheRestrictionChainOnly()
    {
        var id = TypeFixture.Xs("ID");

        await Assert.That(IdAttributeUses.IsIdType(id)).IsTrue();
        await Assert.That(IdAttributeUses.IsIdType(_f.Restrict("limited", id))).IsTrue();
        await Assert.That(IdAttributeUses.IsIdType(TypeFixture.Xs("string"))).IsFalse();
        await Assert.That(IdAttributeUses.IsIdType(_f.ListOf("ids", id))).IsFalse();
    }

    [Test]
    public async Task IsIdType_AUserTypeCalledIdInAnotherNamespace_IsNotTheBuiltIn()
    {
        var lookalike = _f.Restrict("ID", TypeFixture.Xs("string"));

        await Assert.That(IdAttributeUses.IsIdType(lookalike)).IsFalse();
    }

    [Test]
    public async Task Check_NullArguments_Throw()
    {
        var uses = new List<AttributeUse>();

        await Assert
            .That(() => IdAttributeUses.CheckType(null!, XsdVersion.V10, Reporter))
            .Throws<ArgumentNullException>();
        await Assert.That(() => IdAttributeUses.CheckType(uses, XsdVersion.V10, null!)).Throws<ArgumentNullException>();
        await Assert
            .That(() => IdAttributeUses.CheckGroup(null!, XsdVersion.V10, Reporter))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => IdAttributeUses.CheckGroup(uses, XsdVersion.V10, null!))
            .Throws<ArgumentNullException>();
        await Assert.That(() => IdAttributeUses.IsIdType(null!)).Throws<ArgumentNullException>();
    }
}
