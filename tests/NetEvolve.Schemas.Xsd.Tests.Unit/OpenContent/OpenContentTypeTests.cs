namespace NetEvolve.Schemas.Xsd.Tests.Unit.OpenContent;

using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.OpenContent;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;
using NetEvolve.Schemas.Xsd.Tests.Unit.SimpleTypes.Definitions;
using NetEvolve.Schemas.Xsd.Wildcards.Core;

/// <summary>
/// The mapping rules of Structures 1.1, 3.4.2.3.3 for the open content of a complex type: the wildcard element (own, or
/// the default of the schema), the effect of mode none, empty content, and the inheritance through extension.
/// </summary>
public class OpenContentTypeTests
{
    private const string Tns = "urn:test";
    private static readonly DocumentLocation Where = new("file:///a.xsd", 7, 3);
    private static readonly AttributeUse[] NoUses = [];

    private static readonly SimpleTypeDefinition StringType = SimpleTypeDefinition.FromDatatype(
        new XPathQName("http://www.w3.org/2001/XMLSchema", "string"),
        Where,
        new FakeDatatype("string", DatatypeVariety.Atomic)
    );

    private readonly ValidationIssueCollector _collector = new(ValidationLimits.Default);

    private XsdIssueReporter Reporter => new(_collector);

    private IReadOnlyList<ValidationIssue> Issues => _collector.ToResult().Issues;

    private static ComplexTypeHeader Header(string name = "t") =>
        new(new XPathQName(Tns, name), Where, false, XsdDerivationMethod.None, XsdDerivationMethod.None);

    private static Particle Sequence(params string[] names)
    {
        var particles = new Particle[names.Length];
        for (var i = 0; i < names.Length; i++)
        {
            particles[i] = Particle.Create(
                Where,
                1,
                1,
                new FakeComponent(XsdComponentKind.ElementDeclaration, names[i])
            );
        }

        return Particle.Create(Where, 1, 1, ModelGroup.Create(ModelGroupCompositor.Sequence, Where, particles));
    }

    private static Wildcard Wild(NamespaceConstraint? constraint = null) =>
        Wildcard.Create(constraint ?? NamespaceConstraint.Any, XsdProcessContents.Lax, Where);

    private static Wildcard Namespaces(params string[] values) => Wild(NamespaceConstraint.Set(values));

    private static OpenContentElement Own(OpenContentMode mode, Wildcard? wildcard = null) =>
        OpenContentElement.Create(
            mode,
            mode == OpenContentMode.None ? null : wildcard ?? Wild(),
            Where,
            new XsdIssueReporter(new ValidationIssueCollector(ValidationLimits.Default))
        )!;

    private static OpenContentElement Default(
        OpenContentMode mode = OpenContentMode.Interleave,
        Wildcard? wildcard = null,
        bool appliesToEmpty = false
    ) => OpenContentElement.CreateDefault(mode, wildcard ?? Wild(), appliesToEmpty, Where);

    private ComplexTypeDefinition Implicit(
        bool mixed = false,
        Particle? content = null,
        OpenContentElement? own = null,
        OpenContentElement? defaultOpenContent = null
    ) => ComplexTypeDefinition.Implicit(Header(), mixed, content, NoUses, null, Reporter, own, defaultOpenContent);

    private ComplexTypeDefinition? Derive(
        XsdDerivationMethod method,
        ComplexTypeDefinition baseType,
        Particle? content = null,
        OpenContentElement? own = null,
        OpenContentElement? defaultOpenContent = null,
        bool mixed = false
    ) =>
        ComplexTypeDefinition.FromComplexContent(
            Header("d"),
            method,
            baseType,
            mixed,
            content,
            NoUses,
            null,
            Reporter,
            own,
            defaultOpenContent
        );

    // A base type with the declared content (a) and the given open content.
    private ComplexTypeDefinition Base(OpenContentMode mode, Wildcard? wildcard = null) =>
        ComplexTypeDefinition.Implicit(Header("b"), false, Sequence("a"), NoUses, null, Reporter, Own(mode, wildcard));

    // ---- the wildcard element ----------------------------------------------------------------------------------------

    [Test]
    [Arguments(1)]
    [Arguments(2)]
    public async Task Implicit_OwnOpenContent_GivesTheContentTypeItsModeAndWildcard(int modeValue)
    {
        var mode = (OpenContentMode)modeValue;
        var wildcard = Namespaces("urn:a");

        var type = Implicit(content: Sequence("a"), own: Own(mode, wildcard));

        await Assert.That(type.ContentKind).IsEqualTo(ComplexContentKind.ElementOnly);
        await Assert.That(type.OpenContent).IsNotNull();
        await Assert.That(type.OpenContent!.Mode).IsEqualTo(mode);
        await Assert.That(type.OpenContent.Wildcard).IsSameReferenceAs(wildcard);
        await Assert.That(Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Implicit_WithoutOpenContent_HasNone()
    {
        var type = Implicit(content: Sequence("a"));

        await Assert.That(type.OpenContent).IsNull();
        await Assert.That(ComplexTypeDefinition.AnyType.OpenContent).IsNull();
    }

    [Test]
    public async Task Implicit_OwnOpenContent_OnMixedContent_KeepsTheMixedVariety()
    {
        var type = Implicit(mixed: true, content: Sequence("a"), own: Own(OpenContentMode.Interleave));

        await Assert.That(type.ContentKind).IsEqualTo(ComplexContentKind.Mixed);
        await Assert.That(type.OpenContent).IsNotNull();
    }

    [Test]
    public async Task OwnOpenContent_WinsOverTheDefault()
    {
        var own = Wild(NamespaceConstraint.Set(["urn:own"]));

        var type = Implicit(
            content: Sequence("a"),
            own: Own(OpenContentMode.Suffix, own),
            defaultOpenContent: Default(OpenContentMode.Interleave, Wild())
        );

        await Assert.That(type.OpenContent!.Mode).IsEqualTo(OpenContentMode.Suffix);
        await Assert.That(type.OpenContent.Wildcard).IsSameReferenceAs(own);
    }

    // ---- default open content ----------------------------------------------------------------------------------------

    [Test]
    [Arguments(1)]
    [Arguments(2)]
    public async Task DefaultOpenContent_AppliesToATypeWithoutItsOwn(int modeValue)
    {
        var mode = (OpenContentMode)modeValue;
        var wildcard = Namespaces("urn:a");

        var type = Implicit(content: Sequence("a"), defaultOpenContent: Default(mode, wildcard));

        await Assert.That(type.OpenContent).IsNotNull();
        await Assert.That(type.OpenContent!.Mode).IsEqualTo(mode);
        await Assert.That(type.OpenContent.Wildcard).IsSameReferenceAs(wildcard);
    }

    [Test]
    public async Task DefaultOpenContent_AppliesToMixedContentWithoutExplicitContent_EvenWithoutAppliesToEmpty()
    {
        // The explicit content type of a mixed type with no particle is mixed, not empty (rule 4.1.2 with the effective content).
        var type = Implicit(mixed: true, defaultOpenContent: Default());

        await Assert.That(type.ContentKind).IsEqualTo(ComplexContentKind.Mixed);
        await Assert.That(type.OpenContent).IsNotNull();
    }

    [Test]
    public async Task OwnModeNone_RemovesTheDefaultOpenContent()
    {
        var type = Implicit(content: Sequence("a"), own: Own(OpenContentMode.None), defaultOpenContent: Default());

        await Assert.That(type.OpenContent).IsNull();
        await Assert.That(type.ContentKind).IsEqualTo(ComplexContentKind.ElementOnly);
        await Assert.That(Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task OwnModeNone_OnEmptyContent_KeepsTheContentTypeEmpty_EvenWhenTheDefaultAppliesToEmpty()
    {
        var type = Implicit(own: Own(OpenContentMode.None), defaultOpenContent: Default(appliesToEmpty: true));

        await Assert.That(type.OpenContent).IsNull();
        await Assert.That(type.ContentKind).IsEqualTo(ComplexContentKind.Empty);
    }

    [Test]
    public async Task DefaultOpenContent_DoesNotApplyToEmptyContent_UnlessItAppliesToEmpty()
    {
        var without = Implicit(defaultOpenContent: Default(appliesToEmpty: false));
        var with = Implicit(defaultOpenContent: Default(appliesToEmpty: true));

        await Assert.That(without.ContentKind).IsEqualTo(ComplexContentKind.Empty);
        await Assert.That(without.OpenContent).IsNull();
        await Assert.That(without.ContentParticle).IsNull();

        await Assert.That(with.ContentKind).IsEqualTo(ComplexContentKind.ElementOnly);
        await Assert.That(with.OpenContent).IsNotNull();
        await Assert.That(with.ContentParticle).IsNotNull();
        await Assert.That(with.ContentParticle!.Term).IsTypeOf<ModelGroup>();
        await Assert.That(((ModelGroup)with.ContentParticle.Term!).Compositor).IsEqualTo(ModelGroupCompositor.Sequence);
        await Assert.That(((ModelGroup)with.ContentParticle.Term!).Particles.Count).IsEqualTo(0);
    }

    [Test]
    public async Task OwnOpenContent_OnEmptyContent_AppliesWithoutAppliesToEmpty()
    {
        var type = Implicit(own: Own(OpenContentMode.Interleave));

        await Assert.That(type.ContentKind).IsEqualTo(ComplexContentKind.ElementOnly);
        await Assert.That(type.OpenContent).IsNotNull();
        await Assert.That(type.ContentParticle).IsNotNull();
    }

    [Test]
    public async Task EmptyContentWithOpenContent_ThroughComplexContentRestriction_BecomesElementOnly()
    {
        var type = Derive(
            XsdDerivationMethod.Restriction,
            ComplexTypeDefinition.AnyType,
            defaultOpenContent: Default(appliesToEmpty: true)
        );

        await Assert.That(type).IsNotNull();
        await Assert.That(type!.ContentKind).IsEqualTo(ComplexContentKind.ElementOnly);
        await Assert.That(type.OpenContent).IsNotNull();
    }

    // ---- restriction -------------------------------------------------------------------------------------------------

    [Test]
    public async Task Restriction_DoesNotInheritTheOpenContentOfTheBase()
    {
        var baseType = Base(OpenContentMode.Interleave);

        var type = Derive(XsdDerivationMethod.Restriction, baseType, Sequence("a"));

        await Assert.That(type).IsNotNull();
        await Assert.That(type!.OpenContent).IsNull();
    }

    [Test]
    public async Task Restriction_HasOnlyItsOwnOpenContent()
    {
        var baseType = Base(OpenContentMode.Interleave);
        var wildcard = Namespaces("urn:a");

        var type = Derive(
            XsdDerivationMethod.Restriction,
            baseType,
            Sequence("a"),
            Own(OpenContentMode.Suffix, wildcard)
        );

        await Assert.That(type!.OpenContent!.Mode).IsEqualTo(OpenContentMode.Suffix);
        await Assert.That(type.OpenContent.Wildcard).IsSameReferenceAs(wildcard);
        await Assert.That(Issues.Count).IsEqualTo(0);
    }

    // ---- extension: inheritance --------------------------------------------------------------------------------------

    [Test]
    public async Task Extension_WithoutItsOwn_InheritsTheOpenContentOfTheBase()
    {
        var baseType = Base(OpenContentMode.Suffix, Namespaces("urn:a"));

        var type = Derive(XsdDerivationMethod.Extension, baseType, Sequence("b"));

        await Assert.That(type).IsNotNull();
        await Assert.That(type!.OpenContent).IsSameReferenceAs(baseType.OpenContent);
        await Assert.That(Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Extension_WithoutOwnContent_KeepsTheContentTypeOfTheBaseIncludingItsOpenContent()
    {
        var baseType = Base(OpenContentMode.Interleave);

        var type = Derive(XsdDerivationMethod.Extension, baseType);

        await Assert.That(type!.ContentKind).IsEqualTo(ComplexContentKind.ElementOnly);
        await Assert.That(type.ContentParticle).IsSameReferenceAs(baseType.ContentParticle);
        await Assert.That(type.OpenContent).IsSameReferenceAs(baseType.OpenContent);
    }

    [Test]
    public async Task Extension_OfABaseWithoutOpenContent_HasItsOwn()
    {
        var baseType = ComplexTypeDefinition.Implicit(Header("b"), false, Sequence("a"), NoUses, null, Reporter);
        var wildcard = Namespaces("urn:a");

        var type = Derive(
            XsdDerivationMethod.Extension,
            baseType,
            Sequence("b"),
            Own(OpenContentMode.Suffix, wildcard)
        );

        await Assert.That(type!.OpenContent!.Mode).IsEqualTo(OpenContentMode.Suffix);
        await Assert.That(type.OpenContent.Wildcard).IsSameReferenceAs(wildcard);
    }

    [Test]
    public async Task Extension_OfAnEmptyBase_AppliesTheDefaultOnlyToElementContent()
    {
        var emptyBase = ComplexTypeDefinition.Implicit(Header("b"), false, null, NoUses, null, Reporter);

        var withContent = Derive(
            XsdDerivationMethod.Extension,
            emptyBase,
            Sequence("a"),
            defaultOpenContent: Default()
        );
        var withoutContent = Derive(XsdDerivationMethod.Extension, emptyBase, defaultOpenContent: Default());

        await Assert.That(withContent!.OpenContent).IsNotNull();
        await Assert.That(withoutContent!.OpenContent).IsNull();
        await Assert.That(withoutContent.ContentKind).IsEqualTo(ComplexContentKind.Empty);
    }

    // ---- extension: own wildcard against the inherited one -----------------------------------------------------------

    [Test]
    public async Task Extension_WiderOwnWildcard_ReplacesTheInheritedOne()
    {
        var baseType = Base(OpenContentMode.Interleave, Namespaces("urn:a"));
        var wider = Wild();

        var type = Derive(
            XsdDerivationMethod.Extension,
            baseType,
            Sequence("b"),
            Own(OpenContentMode.Interleave, wider)
        );

        await Assert.That(type).IsNotNull();
        await Assert.That(type!.OpenContent!.Wildcard).IsSameReferenceAs(wider);
        await Assert.That(Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Extension_EqualOwnWildcard_IsValid()
    {
        var baseType = Base(OpenContentMode.Interleave, Namespaces("urn:a"));

        var type = Derive(
            XsdDerivationMethod.Extension,
            baseType,
            Sequence("b"),
            Own(OpenContentMode.Interleave, Namespaces("urn:a"))
        );

        await Assert.That(type).IsNotNull();
        await Assert.That(Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Extension_NarrowerOwnWildcard_IsASchemaError()
    {
        var baseType = Base(OpenContentMode.Interleave, Wild());

        var type = Derive(
            XsdDerivationMethod.Extension,
            baseType,
            Sequence("b"),
            Own(OpenContentMode.Interleave, Namespaces("urn:a"))
        );

        await Assert.That(type).IsNull();
        await Assert.That(Issues.Count).IsEqualTo(1);
        await Assert.That(Issues[0].Code).IsEqualTo(XsdRuleCodes.CosCtExtends);
        await Assert.That(Issues[0].Constraint).IsEqualTo("cos-ct-extends.1.4.3.2.2.4");
        await Assert.That(Issues[0].Location).IsEqualTo(Where);
    }

    [Test]
    public async Task Extension_OwnWildcardThatOnlyOverlapsTheInheritedOne_IsASchemaError()
    {
        var baseType = Base(OpenContentMode.Interleave, Namespaces("urn:a", "urn:b"));

        var type = Derive(
            XsdDerivationMethod.Extension,
            baseType,
            Sequence("b"),
            Own(OpenContentMode.Interleave, Namespaces("urn:b", "urn:c"))
        );

        await Assert.That(type).IsNull();
        await Assert.That(Issues[0].Constraint).IsEqualTo("cos-ct-extends.1.4.3.2.2.4");
    }

    // ---- extension: mode ---------------------------------------------------------------------------------------------

    [Test]
    public async Task Extension_SuffixOverAnInterleaveBase_IsASchemaError()
    {
        var baseType = Base(OpenContentMode.Interleave);

        var type = Derive(XsdDerivationMethod.Extension, baseType, Sequence("b"), Own(OpenContentMode.Suffix));

        await Assert.That(type).IsNull();
        await Assert.That(Issues.Count).IsEqualTo(1);
        await Assert.That(Issues[0].Code).IsEqualTo(XsdRuleCodes.CosCtExtends);
        await Assert.That(Issues[0].Constraint).IsEqualTo("cos-ct-extends.1.4.3.2.2.3");
    }

    [Test]
    [Arguments(2, 2, 2)]
    [Arguments(2, 1, 1)]
    [Arguments(1, 1, 1)]
    public async Task Extension_Mode_IsThatOfTheOwnOpenContent(int baseMode, int ownMode, int expected)
    {
        var type = Derive(
            XsdDerivationMethod.Extension,
            Base((OpenContentMode)baseMode),
            Sequence("b"),
            Own((OpenContentMode)ownMode)
        );

        await Assert.That(type).IsNotNull();
        await Assert.That(type!.OpenContent!.Mode).IsEqualTo((OpenContentMode)expected);
        await Assert.That(Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Extension_OwnModeNone_DoesNotRemoveTheInheritedOpenContent()
    {
        // Rule 6.1 yields the explicit content type, which carries the open content of the base (rule 4.2.3).
        var baseType = Base(OpenContentMode.Suffix);

        var type = Derive(XsdDerivationMethod.Extension, baseType, Sequence("b"), Own(OpenContentMode.None));

        await Assert.That(type!.OpenContent).IsSameReferenceAs(baseType.OpenContent);
    }

    // ---- extension: default open content -----------------------------------------------------------------------------

    [Test]
    public async Task Extension_DefaultNarrowerThanTheInherited_IsUnitedWithIt()
    {
        var baseType = Base(OpenContentMode.Interleave, Wild());

        var type = Derive(
            XsdDerivationMethod.Extension,
            baseType,
            Sequence("b"),
            defaultOpenContent: Default(OpenContentMode.Interleave, Namespaces("urn:a"))
        );

        await Assert.That(type).IsNotNull();
        await Assert
            .That(((Wildcard)type!.OpenContent!.Wildcard).Constraint.Variety)
            .IsEqualTo(NamespaceConstraintVariety.Any);
        await Assert.That(Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Extension_DefaultWithoutExpressibleUnion_ReportsTheUnionRule()
    {
        var baseType = Base(OpenContentMode.Interleave, Wild(NamespaceConstraint.Not("urn:one")));

        var type = Derive(
            XsdDerivationMethod.Extension,
            baseType,
            Sequence("b"),
            defaultOpenContent: Default(OpenContentMode.Interleave, Namespaces(string.Empty))
        );

        await Assert.That(type).IsNull();
        await Assert.That(Issues.Count).IsEqualTo(1);
        await Assert.That(Issues[0].Code).IsEqualTo(XsdRuleCodes.CosAwUnion);
    }

    [Test]
    public async Task Extension_DefaultSuffixOverAnInterleaveBase_IsASchemaError()
    {
        var baseType = Base(OpenContentMode.Interleave);

        var type = Derive(
            XsdDerivationMethod.Extension,
            baseType,
            Sequence("b"),
            defaultOpenContent: Default(OpenContentMode.Suffix)
        );

        await Assert.That(type).IsNull();
        await Assert.That(Issues[0].Constraint).IsEqualTo("cos-ct-extends.1.4.3.2.2.3");
    }

    // ---- simple content ----------------------------------------------------------------------------------------------

    [Test]
    public async Task SimpleContent_HasNoOpenContent()
    {
        var type = ComplexTypeDefinition.FromSimpleContent(
            Header("s"),
            XsdDerivationMethod.Extension,
            StringType,
            null,
            NoUses,
            null,
            Reporter
        );

        await Assert.That(type).IsNotNull();
        await Assert.That(type!.OpenContent).IsNull();
    }
}
