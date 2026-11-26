namespace NetEvolve.Schemas.Xsd.Tests.Unit.ComplexTypes.Definitions;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;
using NetEvolve.Schemas.Xsd.Substitution;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;
using NetEvolve.Schemas.Xsd.Wildcards.Core;

public class ComplexTypeDefinitionTests
{
    private const string Tns = "urn:test";
    private static readonly DocumentLocation Where = new("file:///a.xsd", 7, 3);

    private static readonly SimpleTypeDefinition StringType = SimpleTypeDefinition.FromDatatype(
        new XPathQName("http://www.w3.org/2001/XMLSchema", "string"),
        Where,
        new SimpleTypes.Definitions.FakeDatatype("string", DatatypeVariety.Atomic)
    );

    private static readonly AttributeUse[] NoUses = [];

    private readonly ValidationIssueCollector _collector = new(ValidationLimits.Default);

    private XsdIssueReporter Reporter => new(_collector);

    private static ComplexTypeHeader Header(
        string name = "t",
        bool isAbstract = false,
        XsdDerivationMethod block = XsdDerivationMethod.None,
        XsdDerivationMethod final = XsdDerivationMethod.None
    ) => new(name.Length == 0 ? default : new XPathQName(Tns, name), Where, isAbstract, block, final);

    private static Particle Element(string name, long min = 1, long max = 1) =>
        Particle.Create(Where, min, max, new FakeComponent(XsdComponentKind.ElementDeclaration, name));

    private static Particle Group(ModelGroupCompositor compositor, long min, long max, params Particle[] particles) =>
        Particle.Create(Where, min, max, ModelGroup.Create(compositor, Where, particles));

    private static Particle Sequence(params Particle[] particles) =>
        Group(ModelGroupCompositor.Sequence, 1, 1, particles);

    private AttributeUse Use(
        string name,
        AttributeUseKind kind = AttributeUseKind.Optional,
        AttributeDeclaration? declaration = null
    ) =>
        AttributeUse.Create(
            Where,
            kind,
            declaration ?? AttributeDeclaration.Global(name, Tns, Where, StringType, default, Reporter)!,
            default,
            Reporter
        )!;

    private ComplexTypeDefinition Implicit(
        bool mixed = false,
        Particle? content = null,
        IReadOnlyList<AttributeUse>? uses = null,
        IWildcard? wildcard = null
    ) => ComplexTypeDefinition.Implicit(Header(), mixed, content, uses ?? NoUses, wildcard, Reporter);

    private ComplexTypeDefinition? Complex(
        XsdDerivationMethod method,
        IXsdComponent baseType,
        bool mixed = false,
        Particle? content = null,
        IReadOnlyList<AttributeUse>? uses = null,
        IWildcard? wildcard = null
    ) =>
        ComplexTypeDefinition.FromComplexContent(
            Header("d"),
            method,
            baseType,
            mixed,
            content,
            uses ?? NoUses,
            wildcard,
            Reporter
        );

    private ComplexTypeDefinition? Simple(
        XsdDerivationMethod method,
        IXsdComponent baseType,
        SimpleTypeDefinition? simpleType = null,
        IReadOnlyList<AttributeUse>? uses = null,
        IWildcard? wildcard = null
    ) =>
        ComplexTypeDefinition.FromSimpleContent(
            Header("s"),
            method,
            baseType,
            simpleType,
            uses ?? NoUses,
            wildcard,
            Reporter
        );

    private static ComplexTypeDefinition ElementOnlyBase(params Particle[] particles) =>
        ComplexTypeDefinition.Implicit(
            new(new XPathQName(Tns, "b"), Where, false, XsdDerivationMethod.None, XsdDerivationMethod.None),
            false,
            Sequence(particles),
            NoUses,
            null,
            new XsdIssueReporter(new ValidationIssueCollector(ValidationLimits.Default))
        );

    private static Wildcard Wild(NamespaceConstraint constraint, XsdProcessContents process = XsdProcessContents.Lax) =>
        Wildcard.Create(constraint, process, Where);

    private static IXsdComponent Registered(XsdComponentKind kind, string localName)
    {
        var types = XsdBuiltInTypes.For(XsdVersion.V11);
        for (var i = 0; i < types.Count; i++)
        {
            if (types[i].Kind == kind && string.Equals(types[i].Name.LocalName, localName, StringComparison.Ordinal))
            {
                return types[i];
            }
        }

        throw new InvalidOperationException("No built-in type " + localName);
    }

    private async Task AssertSingleIssue(string ruleId, string clause)
    {
        var issues = _collector.ToResult().Issues;
        await Assert.That(issues).Count().IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(XsdRuleLookupOf(ruleId));
        await Assert.That(issues[0].Constraint).IsEqualTo(clause);
        await Assert.That(issues[0].Location.Line).IsEqualTo(7);
    }

    private static DiagnosticCode XsdRuleLookupOf(string id)
    {
        foreach (var rule in XsdRuleCatalog.All)
        {
            if (string.Equals(rule.Id, id, StringComparison.Ordinal))
            {
                return rule.Code;
            }
        }

        throw new InvalidOperationException("No rule " + id);
    }

    // ---- content kinds ----------------------------------------------------------------------------------------------

    [Test]
    public async Task Content_Empty_HasNoParticleAndNoSimpleType()
    {
        var type = Implicit();

        await Assert.That(type.Kind).IsEqualTo(XsdComponentKind.ComplexType);
        await Assert.That(type.ContentKind).IsEqualTo(ComplexContentKind.Empty);
        await Assert.That(type.ContentParticle).IsNull();
        await Assert.That(type.SimpleContentType).IsNull();
    }

    [Test]
    public async Task Content_ElementOnly_KeepsTheExplicitParticle()
    {
        var content = Sequence(Element("a"), Element("b"));

        var type = Implicit(content: content);

        await Assert.That(type.ContentKind).IsEqualTo(ComplexContentKind.ElementOnly);
        await Assert.That(type.ContentParticle).IsSameReferenceAs(content);
        await Assert.That(type.SimpleContentType).IsNull();
    }

    [Test]
    public async Task Content_Mixed_KeepsTheExplicitParticle()
    {
        var content = Sequence(Element("a"));

        var type = Implicit(mixed: true, content: content);

        await Assert.That(type.ContentKind).IsEqualTo(ComplexContentKind.Mixed);
        await Assert.That(type.ContentParticle).IsSameReferenceAs(content);
    }

    [Test]
    public async Task Content_Simple_TakesTheSimpleTypeOfTheBase()
    {
        var type = Simple(XsdDerivationMethod.Extension, StringType)!;

        await Assert.That(type.ContentKind).IsEqualTo(ComplexContentKind.Simple);
        await Assert.That(type.ContentParticle).IsNull();
        await Assert.That(type.SimpleContentType).IsSameReferenceAs(StringType);
        await Assert.That(type.DerivationMethod).IsEqualTo(XsdDerivationMethod.Extension);
        await Assert.That(type.BaseType).IsSameReferenceAs(StringType);
    }

    // ---- effective content rules ------------------------------------------------------------------------------------

    [Test]
    public async Task EffectiveMixed_ComplexContentWins_ThenComplexType_ThenFalse()
    {
        await Assert.That(ComplexTypeDefinition.EffectiveMixed(typeMixed: true, contentMixed: false)).IsFalse();
        await Assert.That(ComplexTypeDefinition.EffectiveMixed(typeMixed: false, contentMixed: true)).IsTrue();
        await Assert.That(ComplexTypeDefinition.EffectiveMixed(typeMixed: true, contentMixed: null)).IsTrue();
        await Assert.That(ComplexTypeDefinition.EffectiveMixed(typeMixed: false, contentMixed: null)).IsFalse();
    }

    [Test]
    [Arguments("Sequence", 1L, 1L)]
    [Arguments("All", 1L, 1L)]
    [Arguments("Sequence", 3L, 5L)]
    [Arguments("Choice", 0L, 1L)]
    [Arguments("Choice", 0L, 4L)]
    public async Task EffectiveContent_EmptyGroup_IsEmpty(string compositor, long min, long max)
    {
        var type = Implicit(content: Group(Enum.Parse<ModelGroupCompositor>(compositor), min, max));

        await Assert.That(type.ContentKind).IsEqualTo(ComplexContentKind.Empty);
        await Assert.That(type.ContentParticle).IsNull();
    }

    [Test]
    public async Task EffectiveContent_EmptyChoiceThatMustOccur_IsNotEmpty()
    {
        var content = Group(ModelGroupCompositor.Choice, 1, 1);

        var type = Implicit(content: content);

        await Assert.That(type.ContentKind).IsEqualTo(ComplexContentKind.ElementOnly);
        await Assert.That(type.ContentParticle).IsSameReferenceAs(content);
    }

    [Test]
    public async Task EffectiveContent_GroupThatMayNotOccur_IsEmpty()
    {
        var type = Implicit(content: Group(ModelGroupCompositor.Sequence, 0, 0, Element("a")));

        await Assert.That(type.ContentKind).IsEqualTo(ComplexContentKind.Empty);
    }

    [Test]
    public async Task EffectiveContent_GroupReference_IsNotEmpty()
    {
        var reference = Particle.CreateReference(Where, 1, 1, XsdSymbolSpace.Groups, new XPathQName(Tns, "g"));

        var type = Implicit(content: reference);

        await Assert.That(type.ContentKind).IsEqualTo(ComplexContentKind.ElementOnly);
        await Assert.That(type.ContentParticle).IsSameReferenceAs(reference);
    }

    [Test]
    public async Task EffectiveContent_NoContentAndMixed_IsAnEmptySequenceThatMustOccurOnce()
    {
        var type = Implicit(mixed: true);

        await AssertEmptySequence(type);
    }

    [Test]
    public async Task EffectiveContent_EmptyGroupAndMixed_IsAnEmptySequenceThatMustOccurOnce()
    {
        var type = Implicit(mixed: true, content: Group(ModelGroupCompositor.Choice, 0, 1));

        await AssertEmptySequence(type);
    }

    [Test]
    public async Task EffectiveContent_ZeroOccurrenceAndMixed_IsAnEmptySequenceThatMustOccurOnce()
    {
        var type = Implicit(mixed: true, content: Group(ModelGroupCompositor.Sequence, 0, 0, Element("a")));

        await AssertEmptySequence(type);
    }

    private static async Task AssertEmptySequence(ComplexTypeDefinition type)
    {
        await Assert.That(type.ContentKind).IsEqualTo(ComplexContentKind.Mixed);
        var particle = type.ContentParticle!;
        await Assert.That(particle.MinOccurs).IsEqualTo(1L);
        await Assert.That(particle.MaxOccurs).IsEqualTo(1L);
        var group = (ModelGroup)particle.Term!;
        await Assert.That(group.Compositor).IsEqualTo(ModelGroupCompositor.Sequence);
        await Assert.That(group.Particles).Count().IsEqualTo(0);
    }

    // ---- restriction ------------------------------------------------------------------------------------------------

    [Test]
    public async Task Restriction_ReplacesTheContentOfTheBase()
    {
        var baseType = ElementOnlyBase(Element("a"), Element("b"));
        var content = Sequence(Element("a"));

        var type = Complex(XsdDerivationMethod.Restriction, baseType, content: content)!;

        await Assert.That(type.ContentKind).IsEqualTo(ComplexContentKind.ElementOnly);
        await Assert.That(type.ContentParticle).IsSameReferenceAs(content);
        await Assert.That(type.DerivationMethod).IsEqualTo(XsdDerivationMethod.Restriction);
        await Assert.That(type.BaseType).IsSameReferenceAs(baseType);
    }

    [Test]
    public async Task Restriction_WithoutContent_IsEmptyEvenWhenTheBaseHasContent()
    {
        var type = Complex(XsdDerivationMethod.Restriction, ElementOnlyBase(Element("a")))!;

        await Assert.That(type.ContentKind).IsEqualTo(ComplexContentKind.Empty);
        await Assert.That(type.ContentParticle).IsNull();
    }

    // ---- extension --------------------------------------------------------------------------------------------------

    [Test]
    public async Task Extension_WithoutContent_TakesTheContentTypeOfTheBase()
    {
        var baseType = ElementOnlyBase(Element("a"));

        var type = Complex(XsdDerivationMethod.Extension, baseType)!;

        await Assert.That(type.ContentKind).IsEqualTo(ComplexContentKind.ElementOnly);
        await Assert.That(type.ContentParticle).IsSameReferenceAs(baseType.ContentParticle);
    }

    [Test]
    public async Task Extension_OfASimpleContentBase_WithoutContent_KeepsTheSimpleContent()
    {
        var baseType = Simple(XsdDerivationMethod.Extension, StringType)!;

        var type = Complex(XsdDerivationMethod.Extension, baseType)!;

        await Assert.That(type.ContentKind).IsEqualTo(ComplexContentKind.Simple);
        await Assert.That(type.SimpleContentType).IsSameReferenceAs(StringType);
    }

    [Test]
    public async Task Extension_OfAnEmptyBase_TakesTheExplicitContent()
    {
        var content = Sequence(Element("a"));

        var type = Complex(XsdDerivationMethod.Extension, Implicit(), content: content)!;

        await Assert.That(type.ContentKind).IsEqualTo(ComplexContentKind.ElementOnly);
        await Assert.That(type.ContentParticle).IsSameReferenceAs(content);
    }

    [Test]
    public async Task Extension_OfAnEmptyBase_WithMixed_IsMixed()
    {
        var type = Complex(XsdDerivationMethod.Extension, Implicit(), mixed: true, content: Sequence(Element("a")))!;

        await Assert.That(type.ContentKind).IsEqualTo(ComplexContentKind.Mixed);
    }

    [Test]
    public async Task Extension_OfAContentBase_IsTheSequenceOfBaseAndExplicitContent()
    {
        var baseType = ElementOnlyBase(Element("a"));
        var content = Sequence(Element("b"));

        var type = Complex(XsdDerivationMethod.Extension, baseType, content: content)!;

        await Assert.That(type.ContentKind).IsEqualTo(ComplexContentKind.ElementOnly);
        var particle = type.ContentParticle!;
        await Assert.That(particle.MinOccurs).IsEqualTo(1L);
        await Assert.That(particle.MaxOccurs).IsEqualTo(1L);
        var group = (ModelGroup)particle.Term!;
        await Assert.That(group.Compositor).IsEqualTo(ModelGroupCompositor.Sequence);
        await Assert.That(group.Particles).Count().IsEqualTo(2);
        await Assert.That(group.Particles[0]).IsSameReferenceAs(baseType.ContentParticle);
        await Assert.That(group.Particles[1]).IsSameReferenceAs(content);
    }

    private ComplexTypeDefinition? ExtendWith(
        XsdVersion version,
        ComplexTypeDefinition baseType,
        Particle content,
        long minOccurs = 1
    ) =>
        ComplexTypeDefinition.FromComplexContent(
            Header("e"),
            XsdDerivationMethod.Extension,
            baseType,
            false,
            Particle.Create(Where, minOccurs, content.MaxOccurs, content.Term),
            NoUses,
            null,
            Reporter,
            version: version
        );

    private static ComplexTypeDefinition AllBase(params Particle[] particles) =>
        ComplexTypeDefinition.Implicit(
            new(new XPathQName(Tns, "b"), Where, false, XsdDerivationMethod.None, XsdDerivationMethod.None),
            false,
            Group(ModelGroupCompositor.All, 1, 1, particles),
            NoUses,
            null,
            new XsdIssueReporter(new ValidationIssueCollector(ValidationLimits.Default))
        );

    [Test]
    public async Task Extension_OfAnAllBase_WithAllContent_InXsd11_IsOneAllGroup()
    {
        var baseType = AllBase(Element("a"));
        var content = Group(ModelGroupCompositor.All, 0, 1, Element("b"));

        var type = ExtendWith(XsdVersion.V11, baseType, content, 0)!;

        var particle = type.ContentParticle!;
        await Assert.That(particle.MinOccurs).IsEqualTo(0L);
        await Assert.That(particle.MaxOccurs).IsEqualTo(1L);
        var group = (ModelGroup)particle.Term!;
        await Assert.That(group.Compositor).IsEqualTo(ModelGroupCompositor.All);
        await Assert.That(group.Particles).Count().IsEqualTo(2);
    }

    [Test]
    public async Task Extension_OfAnAllBase_WithAllContent_InXsd10_StaysASequence()
    {
        var baseType = AllBase(Element("a"));
        var content = Group(ModelGroupCompositor.All, 1, 1, Element("b"));

        var type = ExtendWith(XsdVersion.V10, baseType, content)!;

        await Assert
            .That(((ModelGroup)type.ContentParticle!.Term!).Compositor)
            .IsEqualTo(ModelGroupCompositor.Sequence);
    }

    [Test]
    public async Task Extension_OfAnAllBase_WithSequenceContent_InXsd11_StaysASequence()
    {
        var baseType = AllBase(Element("a"));

        var type = ExtendWith(XsdVersion.V11, baseType, Sequence(Element("b")))!;

        await Assert
            .That(((ModelGroup)type.ContentParticle!.Term!).Compositor)
            .IsEqualTo(ModelGroupCompositor.Sequence);
    }

    [Test]
    public async Task Extension_OfAContentBase_WithMixed_IsMixed()
    {
        var type = Complex(
            XsdDerivationMethod.Extension,
            ElementOnlyBase(Element("a")),
            mixed: true,
            content: Sequence(Element("b"))
        )!;

        await Assert.That(type.ContentKind).IsEqualTo(ComplexContentKind.Mixed);
    }

    [Test]
    public async Task Extension_OfASimpleContentBase_WithElementContent_IsAnIssue()
    {
        var baseType = Simple(XsdDerivationMethod.Extension, StringType)!;

        var type = Complex(XsdDerivationMethod.Extension, baseType, content: Sequence(Element("a")));

        await Assert.That(type).IsNull();
        await AssertSingleIssue("cos-ct-extends", "cos-ct-extends");
    }

    // ---- anyType ----------------------------------------------------------------------------------------------------

    [Test]
    public async Task AnyType_IsTheUrTypeWithMixedLaxContentAndAttributes()
    {
        var anyType = ComplexTypeDefinition.AnyType;

        await Assert.That(anyType.Name).IsEqualTo(new XPathQName("http://www.w3.org/2001/XMLSchema", "anyType"));
        await Assert.That(anyType.BaseType).IsNull();
        await Assert.That(anyType.DerivationMethod).IsEqualTo(XsdDerivationMethod.Restriction);
        await Assert.That(anyType.IsAbstract).IsFalse();
        await Assert.That(anyType.ContentKind).IsEqualTo(ComplexContentKind.Mixed);
        var group = (ModelGroup)anyType.ContentParticle!.Term!;
        await Assert.That(group.Compositor).IsEqualTo(ModelGroupCompositor.Sequence);
        await Assert.That(group.Particles).Count().IsEqualTo(1);
        await Assert.That(group.Particles[0].MinOccurs).IsEqualTo(0L);
        await Assert.That(group.Particles[0].MaxOccurs).IsEqualTo(Particle.Unbounded);
        var element = (IWildcard)group.Particles[0].Term!;
        await Assert.That(element.ProcessContents).IsEqualTo(XsdProcessContents.Lax);
        await Assert.That(element.Allows(new XPathQName("urn:any", "x"))).IsTrue();
        await Assert.That(anyType.AttributeUses).Count().IsEqualTo(0);
        await Assert.That(anyType.AttributeWildcard!.ProcessContents).IsEqualTo(XsdProcessContents.Lax);
        await Assert.That(anyType.AttributeWildcard!.Allows(new XPathQName("urn:any", "x"))).IsTrue();
    }

    [Test]
    public async Task Implicit_WithoutABase_DerivesFromAnyTypeByRestriction()
    {
        var type = Implicit();

        await Assert.That(type.BaseType).IsSameReferenceAs(ComplexTypeDefinition.AnyType);
        await Assert.That(type.DerivationMethod).IsEqualTo(XsdDerivationMethod.Restriction);
    }

    [Test]
    public async Task Implicit_DoesNotInheritTheAnyTypeWildcard()
    {
        var type = Implicit();

        await Assert.That(type.AttributeWildcard).IsNull();
        await Assert.That(type.AttributeUses).Count().IsEqualTo(0);
    }

    [Test]
    public async Task RegisteredAnyType_IsRecognizedAsTheUrType()
    {
        var registered = Registered(XsdComponentKind.ComplexType, "anyType");

        var type = Complex(XsdDerivationMethod.Restriction, registered, content: Sequence(Element("a")))!;

        await Assert.That(type.BaseType).IsSameReferenceAs(ComplexTypeDefinition.AnyType);
    }

    [Test]
    public async Task Extension_OfAnyType_KeepsTheAnyTypeWildcardAndAddsTheContent()
    {
        var content = Sequence(Element("a"));

        var type = Complex(
            XsdDerivationMethod.Extension,
            ComplexTypeDefinition.AnyType,
            mixed: true,
            content: content
        )!;

        await Assert.That(type.ContentKind).IsEqualTo(ComplexContentKind.Mixed);
        await Assert.That(type.AttributeWildcard).IsNotNull();
    }

    // ---- attribute uses ---------------------------------------------------------------------------------------------

    [Test]
    public async Task Extension_InheritsTheAttributeUsesOfTheBase_ThenAddsItsOwn()
    {
        var inherited = Use("a");
        var baseType = ComplexTypeDefinition.Implicit(Header("b"), false, null, [inherited], null, Reporter);
        var own = Use("b");

        var type = Complex(XsdDerivationMethod.Extension, baseType, uses: [own])!;

        await Assert.That(type.AttributeUses).Count().IsEqualTo(2);
        await Assert.That(type.AttributeUses[0]).IsSameReferenceAs(inherited);
        await Assert.That(type.AttributeUses[1]).IsSameReferenceAs(own);
        await Assert.That(_collector.ToResult().Issues).Count().IsEqualTo(0);
    }

    [Test]
    public async Task Restriction_ReplacesTheUsesWithTheSameName_AndKeepsTheOthers()
    {
        var kept = Use("a");
        var replaced = Use("b");
        var baseType = ComplexTypeDefinition.Implicit(Header("b"), false, null, [kept, replaced], null, Reporter);
        var replacement = Use("b", AttributeUseKind.Required);

        var type = Complex(XsdDerivationMethod.Restriction, baseType, uses: [replacement])!;

        await Assert.That(type.AttributeUses).Count().IsEqualTo(2);
        await Assert.That(type.AttributeUses[0]).IsSameReferenceAs(kept);
        await Assert.That(type.AttributeUses[1]).IsSameReferenceAs(replacement);
    }

    [Test]
    public async Task Restriction_ProhibitsTheUsesOfTheBase_AndKeepsNoProhibitedUse()
    {
        var kept = Use("a");
        var baseType = ComplexTypeDefinition.Implicit(Header("b"), false, null, [kept, Use("b")], null, Reporter);

        var type = Complex(XsdDerivationMethod.Restriction, baseType, uses: [Use("b", AttributeUseKind.Prohibited)])!;

        await Assert.That(type.AttributeUses).Count().IsEqualTo(1);
        await Assert.That(type.AttributeUses[0]).IsSameReferenceAs(kept);
    }

    [Test]
    public async Task ProhibitedOwnUse_IsNotPartOfTheType()
    {
        var type = Implicit(uses: [Use("a", AttributeUseKind.Prohibited), Use("b")]);

        await Assert.That(type.AttributeUses).Count().IsEqualTo(1);
        await Assert.That(type.AttributeUses[0].Name).IsEqualTo(new XPathQName(Tns, "b"));
    }

    [Test]
    public async Task Extension_OfAnyType_HasNoInheritedUses()
    {
        var own = Use("a");

        var type = Complex(XsdDerivationMethod.Extension, ComplexTypeDefinition.AnyType, uses: [own])!;

        await Assert.That(type.AttributeUses).Count().IsEqualTo(1);
    }

    [Test]
    public async Task SimpleContent_InheritsTheUsesOfAComplexBase_ButNotOfASimpleBase()
    {
        var inherited = Use("a");
        var baseType = Simple(XsdDerivationMethod.Extension, StringType, uses: [inherited])!;

        var derived = Simple(XsdDerivationMethod.Extension, baseType, uses: [Use("b")])!;
        var overString = Simple(XsdDerivationMethod.Extension, StringType)!;

        await Assert.That(derived.AttributeUses).Count().IsEqualTo(2);
        await Assert.That(derived.AttributeUses[0]).IsSameReferenceAs(inherited);
        await Assert.That(overString.AttributeUses).Count().IsEqualTo(0);
    }

    [Test]
    public async Task TwoUsesWithTheSameName_AreAnIssue()
    {
        var type = Implicit(uses: [Use("a"), Use("a")]);

        await Assert.That(type.AttributeUses).Count().IsEqualTo(1);
        await AssertSingleIssue("ct-props-correct", "ct-props-correct.4");
    }

    [Test]
    public async Task TwoUsesOfTheSameDeclaration_AreNotAnIssue()
    {
        var declaration = AttributeDeclaration.Global("a", Tns, Where, StringType, default, Reporter)!;

        var type = Implicit(uses: [Use("a", declaration: declaration), Use("a", declaration: declaration)]);

        await Assert.That(type.AttributeUses).Count().IsEqualTo(1);
        await Assert.That(_collector.ToResult().Issues).Count().IsEqualTo(0);
    }

    [Test]
    public async Task Extension_WithAUseOfAnInheritedName_IsAnIssue()
    {
        var inherited = Use("a");
        var baseType = ComplexTypeDefinition.Implicit(Header("b"), false, null, [inherited], null, Reporter);

        var type = Complex(XsdDerivationMethod.Extension, baseType, uses: [Use("a")])!;

        await Assert.That(type.AttributeUses).Count().IsEqualTo(1);
        await Assert.That(type.AttributeUses[0]).IsSameReferenceAs(inherited);
        await AssertSingleIssue("ct-props-correct", "ct-props-correct.4");
    }

    // ---- attribute wildcard -----------------------------------------------------------------------------------------

    [Test]
    public async Task Extension_UnitesTheWildcardOfTheBaseWithItsOwn()
    {
        var baseType = ComplexTypeDefinition.Implicit(
            Header("b"),
            false,
            null,
            NoUses,
            Wild(NamespaceConstraint.Set(["urn:a"]), XsdProcessContents.Skip),
            Reporter
        );

        var type = Complex(
            XsdDerivationMethod.Extension,
            baseType,
            wildcard: Wild(NamespaceConstraint.Set(["urn:b"]), XsdProcessContents.Strict)
        )!;

        var wildcard = type.AttributeWildcard!;
        await Assert.That(wildcard.Allows(new XPathQName("urn:a", "x"))).IsTrue();
        await Assert.That(wildcard.Allows(new XPathQName("urn:b", "x"))).IsTrue();
        await Assert.That(wildcard.Allows(new XPathQName("urn:c", "x"))).IsFalse();
        await Assert.That(wildcard.ProcessContents).IsEqualTo(XsdProcessContents.Strict);
    }

    [Test]
    public async Task Extension_WithoutAnOwnWildcard_KeepsTheWildcardOfTheBase()
    {
        var inherited = Wild(NamespaceConstraint.Set(["urn:a"]));
        var baseType = ComplexTypeDefinition.Implicit(Header("b"), false, null, NoUses, inherited, Reporter);

        var type = Complex(XsdDerivationMethod.Extension, baseType)!;

        await Assert.That(type.AttributeWildcard).IsSameReferenceAs(inherited);
    }

    [Test]
    public async Task Restriction_DoesNotInheritTheWildcardOfTheBase()
    {
        var baseType = ComplexTypeDefinition.Implicit(
            Header("b"),
            false,
            null,
            NoUses,
            Wild(NamespaceConstraint.Set(["urn:a"])),
            Reporter
        );
        var own = Wild(NamespaceConstraint.Set(["urn:b"]));

        var inherited = Complex(XsdDerivationMethod.Restriction, baseType)!;
        var replaced = Complex(XsdDerivationMethod.Restriction, baseType, wildcard: own)!;

        await Assert.That(inherited.AttributeWildcard).IsNull();
        await Assert.That(replaced.AttributeWildcard).IsSameReferenceAs(own);
    }

    [Test]
    public async Task Extension_WithAWildcardUnionThatIsNotExpressible_IsAnIssue()
    {
        var baseType = ComplexTypeDefinition.Implicit(
            Header("b"),
            false,
            null,
            NoUses,
            Wild(NamespaceConstraint.Set([string.Empty])),
            Reporter
        );

        var type = Complex(XsdDerivationMethod.Extension, baseType, wildcard: Wild(NamespaceConstraint.Not("urn:a")))!;

        await Assert.That(type).IsNotNull();
        await AssertSingleIssue("cos-aw-union", "cos-aw-union");
    }

    // ---- simple content ---------------------------------------------------------------------------------------------

    [Test]
    public async Task SimpleContent_Restriction_TakesTheGivenSimpleType()
    {
        var baseType = Simple(XsdDerivationMethod.Extension, StringType)!;
        var restricted = SimpleTypeDefinition.Restrict(
            new XPathQName(Tns, "r"),
            Where,
            XsdDerivationMethod.None,
            StringType,
            [],
            Reporter
        )!;

        var type = Simple(XsdDerivationMethod.Restriction, baseType, restricted)!;

        await Assert.That(type.ContentKind).IsEqualTo(ComplexContentKind.Simple);
        await Assert.That(type.SimpleContentType).IsSameReferenceAs(restricted);
    }

    [Test]
    public async Task SimpleContent_Restriction_WithoutASimpleType_KeepsThatOfTheBase()
    {
        var baseType = Simple(XsdDerivationMethod.Extension, StringType)!;

        var type = Simple(XsdDerivationMethod.Restriction, baseType)!;

        await Assert.That(type.SimpleContentType).IsSameReferenceAs(StringType);
    }

    [Test]
    public async Task SimpleContent_Restriction_OfAMixedBase_NeedsASimpleType()
    {
        var mixedBase = Implicit(mixed: true);

        var withType = Simple(XsdDerivationMethod.Restriction, mixedBase, StringType);
        var withoutType = Simple(XsdDerivationMethod.Restriction, mixedBase);

        await Assert.That(withType!.SimpleContentType).IsSameReferenceAs(StringType);
        await Assert.That(withoutType).IsNull();
        await AssertSingleIssue("src-ct", "src-ct.2");
    }

    [Test]
    public async Task SimpleContent_Extension_OfARegisteredBuiltInSimpleType_WrapsTheDatatype()
    {
        var registered = Registered(XsdComponentKind.SimpleType, "string");

        var type = Simple(XsdDerivationMethod.Extension, registered)!;

        await Assert.That(type.SimpleContentType).IsNotNull();
        await Assert.That(type.SimpleContentType!.Name).IsEqualTo(registered.Name);
    }

    [Test]
    public async Task SimpleContent_OverAnElementOnlyBase_IsAnIssue()
    {
        var type = Simple(XsdDerivationMethod.Extension, ElementOnlyBase(Element("a")));

        await Assert.That(type).IsNull();
        await AssertSingleIssue("src-ct", "src-ct.2");
    }

    [Test]
    public async Task SimpleContent_Restriction_OfASimpleType_IsAnIssue()
    {
        var type = Simple(XsdDerivationMethod.Restriction, StringType);

        await Assert.That(type).IsNull();
        await AssertSingleIssue("src-ct", "src-ct.2");
    }

    [Test]
    public async Task SimpleContent_OverAnUnknownComponent_IsAnIssue()
    {
        var type = Simple(XsdDerivationMethod.Extension, new FakeComponent(XsdComponentKind.ElementDeclaration, "e"));

        await Assert.That(type).IsNull();
        await AssertSingleIssue("src-ct", "src-ct.2");
    }

    [Test]
    public async Task ComplexContent_OverASimpleType_IsAnIssue()
    {
        var type = Complex(XsdDerivationMethod.Extension, StringType);

        await Assert.That(type).IsNull();
        await AssertSingleIssue("src-ct", "src-ct.1");
    }

    // ---- properties -------------------------------------------------------------------------------------------------

    [Test]
    public async Task Properties_AreExposed_AndBlockAndFinalAreLimitedToExtensionAndRestriction()
    {
        var type = ComplexTypeDefinition.Implicit(
            Header(
                "t",
                isAbstract: true,
                block: XsdDerivationMethod.Extension | XsdDerivationMethod.Substitution,
                final: XsdDerivationMethod.Restriction | XsdDerivationMethod.List
            ),
            false,
            null,
            NoUses,
            null,
            Reporter
        );

        await Assert.That(type.Name).IsEqualTo(new XPathQName(Tns, "t"));
        await Assert.That(type.Location.Line).IsEqualTo(7);
        await Assert.That(type.IsAbstract).IsTrue();
        await Assert.That(type.IsAnonymous).IsFalse();
        await Assert.That(type.Block).IsEqualTo(XsdDerivationMethod.Extension);
        await Assert.That(type.Final).IsEqualTo(XsdDerivationMethod.Restriction);
        await Assert.That(((IBlockedType)type).Block).IsEqualTo(XsdDerivationMethod.Extension);
    }

    [Test]
    public async Task AnonymousType_HasNoName()
    {
        var type = ComplexTypeDefinition.Implicit(Header(""), false, null, NoUses, null, Reporter);

        await Assert.That(type.IsAnonymous).IsTrue();
        await Assert.That(type.Name).IsEqualTo(default(XPathQName));
    }

    // ---- references -------------------------------------------------------------------------------------------------

    [Test]
    public async Task References_AreThoseOfTheExplicitContent_AndBindThroughTheType()
    {
        var reference = Particle.CreateReference(Where, 1, 1, XsdSymbolSpace.Groups, new XPathQName(Tns, "g"));
        var type = Implicit(content: Sequence(reference));
        var target = new FakeComponent(XsdComponentKind.ModelGroupDefinition, "g");

        await Assert.That(type.ReferenceCount).IsEqualTo(1);
        await Assert.That(type.GetReference(0).Name).IsEqualTo(new XPathQName(Tns, "g"));
        await Assert.That(type.GetReference(0).Space).IsEqualTo(XsdSymbolSpace.Groups);

        type.Bind(0, target);

        await Assert.That(reference.Term).IsSameReferenceAs(target);
    }

    [Test]
    public async Task References_OfTheBaseContent_AreNotExposedAgain()
    {
        var reference = Particle.CreateReference(Where, 1, 1, XsdSymbolSpace.Groups, new XPathQName(Tns, "g"));
        var baseType = ElementOnlyBase(reference);

        var type = Complex(XsdDerivationMethod.Extension, baseType, content: Sequence(Element("a")))!;

        await Assert.That(baseType.ReferenceCount).IsEqualTo(1);
        await Assert.That(type.ReferenceCount).IsEqualTo(0);
    }

    // ---- guards -----------------------------------------------------------------------------------------------------

    [Test]
    public async Task Factories_RejectNullArguments()
    {
        await Assert
            .That(() => ComplexTypeDefinition.Implicit(Header(), false, null, null!, null, Reporter))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => ComplexTypeDefinition.Implicit(Header(), false, null, NoUses, null, null!))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() =>
                ComplexTypeDefinition.FromComplexContent(
                    Header(),
                    XsdDerivationMethod.Extension,
                    null!,
                    false,
                    null,
                    NoUses,
                    null,
                    Reporter
                )
            )
            .Throws<ArgumentNullException>();
        await Assert
            .That(() =>
                ComplexTypeDefinition.FromSimpleContent(
                    Header(),
                    XsdDerivationMethod.Extension,
                    StringType,
                    null,
                    null!,
                    null,
                    Reporter
                )
            )
            .Throws<ArgumentNullException>();
        await Assert.That(() => Implicit().Bind(0, null!)).Throws<ArgumentNullException>();
    }

    [Test]
    [Arguments(0)]
    [Arguments(4)]
    public async Task Factories_RejectADerivationMethodOtherThanExtensionOrRestriction(int method) =>
        await Assert
            .That(() =>
                ComplexTypeDefinition.FromComplexContent(
                    Header(),
                    (XsdDerivationMethod)method,
                    ComplexTypeDefinition.AnyType,
                    false,
                    null,
                    NoUses,
                    null,
                    Reporter
                )
            )
            .Throws<ArgumentOutOfRangeException>();
}
