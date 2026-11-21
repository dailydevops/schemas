namespace NetEvolve.Schemas.Xsd.Tests.Unit.Compilation;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.Compilation;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;

/// <summary>The component builders through their seam and the lookups of the build context that they share.</summary>
public class BuilderTests
{
    private const string Body =
        "<xs:simpleType name=\"s\"><xs:restriction base=\"xs:int\"/></xs:simpleType>"
        + "<xs:complexType name=\"c\"/>"
        + "<xs:attribute name=\"a\"/>"
        + "<xs:attributeGroup name=\"g\"/>"
        + "<xs:element name=\"e\"/>";

    [Test]
    public async Task Build_ThroughTheSeam_GivesTheComponentOfEachKind()
    {
        var kit = new ContextKit(Body);
        var context = kit.Context;

        await Assert
            .That(context.SimpleTypes.Build(kit.Node(0), kit.Registry, kit.Reporter))
            .IsTypeOf<SimpleTypeDefinition>();
        await Assert
            .That(context.ComplexTypes.Build(kit.Node(1), kit.Registry, kit.Reporter))
            .IsTypeOf<ComplexTypeDefinition>();
        await Assert
            .That(context.Attributes.Build(kit.Node(2), kit.Registry, kit.Reporter))
            .IsTypeOf<AttributeDeclaration>();
        await Assert
            .That(context.AttributeGroups.Build(kit.Node(3), kit.Registry, kit.Reporter))
            .IsTypeOf<AttributeGroupDefinition>();
        await Assert.That(context.SimpleTypes.Kind).IsEqualTo(XsdComponentKind.SimpleType);
        await Assert.That(context.ComplexTypes.Kind).IsEqualTo(XsdComponentKind.ComplexType);
        await Assert.That(context.Attributes.Kind).IsEqualTo(XsdComponentKind.AttributeDeclaration);
        await Assert.That(context.AttributeGroups.Kind).IsEqualTo(XsdComponentKind.AttributeGroup);
        await Assert.That(kit.Codes()).IsEmpty();
    }

    [Test]
    public async Task Build_OfANodeThatIsNotItsKind_IsNotSupported()
    {
        var kit = new ContextKit(Body);
        var element = kit.Node(4);

        await Assert
            .That(() => kit.Context.SimpleTypes.Build(element, kit.Registry, kit.Reporter))
            .Throws<NotSupportedException>();
        await Assert
            .That(() => kit.Context.ComplexTypes.Build(element, kit.Registry, kit.Reporter))
            .Throws<NotSupportedException>();
        await Assert
            .That(() => kit.Context.Attributes.Build(element, kit.Registry, kit.Reporter))
            .Throws<NotSupportedException>();
        await Assert
            .That(() => kit.Context.AttributeGroups.Build(element, kit.Registry, kit.Reporter))
            .Throws<NotSupportedException>();
    }

    [Test]
    public async Task Build_OfAnAttributeDeclarationThatHasAnError_IsNotSupportedAfterTheIssueWasReported()
    {
        var kit = new ContextKit("<xs:attribute name=\"xmlns\"/>");

        await Assert
            .That(() => kit.Context.Attributes.Build(kit.Node(0), kit.Registry, kit.Reporter))
            .Throws<NotSupportedException>();
        await Assert.That(kit.Codes()).IsEquivalentTo([XsdRuleCodes.NoXmlns]);
        await Assert.That(kit.Context.Attributes.Ensure(kit.Context.DefinitionOf(kit.Node(0))!)).IsNull();
        await Assert.That(kit.Codes().Length).IsEqualTo(1);
    }

    [Test]
    public async Task Builders_GuardTheirArguments()
    {
        var kit = new ContextKit(Body);
        var context = kit.Context;

        await Assert.That(() => new BuildContext(XsdVersion.V10, null!, kit.Reporter)).Throws<ArgumentNullException>();
        await Assert.That(() => new BuildContext(XsdVersion.V10, kit.Registry, null!)).Throws<ArgumentNullException>();
        await Assert
            .That(() => new BuildContext((XsdVersion)5, kit.Registry, kit.Reporter))
            .Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => new SimpleTypeBuilder(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => new ComplexTypeBuilder(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => new AttributeBuilder(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => new AttributeGroupBuilder(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => new UnitScope(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => context.SimpleTypes.Build((Definition)null!)).Throws<ArgumentNullException>();
        await Assert
            .That(() => context.SimpleTypes.BuildAnonymous(null!, kit.Units[0]))
            .Throws<ArgumentNullException>();
        await Assert.That(() => context.SimpleTypes.ReadFacets(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => context.ComplexTypes.Build((Definition)null!)).Throws<ArgumentNullException>();
        await Assert
            .That(() => context.ComplexTypes.BuildAnonymous(kit.Node(1), null!))
            .Throws<ArgumentNullException>();
        await Assert.That(() => context.Attributes.Ensure(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => context.Attributes.BuildUse(null!, kit.Units[0])).Throws<ArgumentNullException>();
        await Assert.That(() => context.AttributeGroups.Ensure(null!)).Throws<ArgumentNullException>();
        await Assert
            .That(() => context.AttributeGroups.ReadWildcard(kit.Node(2), null!))
            .Throws<ArgumentNullException>();
        await Assert.That(() => context.EnsureType(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => context.DefinitionOf(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => context.Index(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => context.AsSimple(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => context.FixedOf(null!)).Throws<ArgumentNullException>();
        await Assert
            .That(() => context.ResolveType(kit.Node(0), kit.Units[0], null!, default))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => context.ResolveAttribute(kit.Node(0), kit.Units[0], null!))
            .Throws<ArgumentNullException>();
        await Assert.That(() => context.ResolveAttributeGroup(null!, kit.Units[0])).Throws<ArgumentNullException>();
        await Assert.That(() => new Definition(null!, kit.Units[0], default)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Context_BuiltInTypes_AreWrappedOncePerType()
    {
        var kit = new ContextKit(Body);
        kit.Registry.TryGet(XsdSymbolSpace.Types, new XPathQName(CompilationFixture.Xs, "int"), out var builtIn);
        kit.Registry.TryGet(XsdSymbolSpace.Types, new XPathQName(CompilationFixture.Xs, "anyType"), out var urType);

        var wrapper = kit.Context.AsSimple(builtIn!);

        await Assert.That(wrapper).IsNotNull();
        await Assert.That(kit.Context.AsSimple(builtIn!)).IsSameReferenceAs(wrapper);
        await Assert.That(kit.Context.AsSimple(wrapper!)).IsSameReferenceAs(wrapper);
        await Assert.That(kit.Context.AsSimple(urType!)).IsNull();
        await Assert
            .That(kit.Context.AnySimpleType().Name)
            .IsEqualTo(new XPathQName(CompilationFixture.Xs, "anySimpleType"));
        await Assert.That(kit.Context.Placeholder(default, default).IsAnonymous).IsTrue();
        await Assert
            .That(kit.Context.FixedOf(builtIn!))
            .IsEqualTo(default(NetEvolve.Schemas.Xml.Datatypes.Facets.Core.FacetKindSet));
    }

    [Test]
    public async Task Context_ReferencesThatDoNotResolve_AreReportedOnceEach()
    {
        var kit = new ContextKit(
            "<xs:element name=\"e\" type=\"u:x\"/><xs:attributeGroup name=\"g\"><xs:attributeGroup/></xs:attributeGroup>"
        );
        var node = kit.Node(0);
        var unit = kit.Units[0];

        await Assert.That(kit.Context.ResolveType(node, unit, "u:x", default)).IsNull();
        await Assert.That(kit.Context.ResolveType(node, unit, "xs:nope", default)).IsNull();
        await Assert.That(kit.Context.ResolveAttribute(node, unit, "u:x")).IsNull();
        await Assert.That(kit.Context.ResolveAttribute(node, unit, "nope")).IsNull();
        await Assert.That(kit.Context.ResolveAttributeGroup(node, unit)).IsNull();
        await Assert.That(BuildContext.TryNameOf(node, unit, out _)).IsFalse();
        await Assert.That(kit.Codes().Length).IsEqualTo(5);
        await Assert.That(kit.Codes()).Contains(XsdRuleCodes.SrcResolve);
        await Assert.That(kit.Codes()).Contains(XsdRuleCodes.SrcAttributeGroup);
    }

    [Test]
    public async Task Context_AttributeGroupReferenceWithAnUndeclaredPrefixOrAMissingGroup_IsUnresolved()
    {
        var kit = new ContextKit(
            "<xs:attributeGroup name=\"g\"/><xs:attributeGroup name=\"h\"><xs:attributeGroup ref=\"u:g\"/><xs:attributeGroup ref=\"t:missing\"/><xs:attributeGroup ref=\"t:g\"/></xs:attributeGroup>"
        );
        var group = kit.Node(1);
        var unit = kit.Units[0];

        await Assert.That(kit.Context.ResolveAttributeGroup(group.GetChildNode(0), unit)).IsNull();
        await Assert.That(kit.Context.ResolveAttributeGroup(group.GetChildNode(1), unit)).IsNull();
        await Assert.That(kit.Context.ResolveAttributeGroup(group.GetChildNode(2), unit)).IsNotNull();
        await Assert.That(BuildContext.TryNameOf(group.GetChildNode(2), unit, out var name)).IsTrue();
        await Assert.That(name).IsEqualTo(new XPathQName(CompilationFixture.Tns, "g"));
        await Assert.That(kit.Codes()).IsEquivalentTo([XsdRuleCodes.SrcResolve, XsdRuleCodes.SrcResolve]);
    }

    [Test]
    public async Task Context_DefinitionsWithoutAName_AreIndexedButNotByName()
    {
        var kit = new ContextKit(
            "<xs:simpleType name=\"\"><xs:restriction base=\"xs:int\"/></xs:simpleType><xs:attribute/>"
        );

        await Assert.That(kit.Context.Definitions.Count).IsEqualTo(2);
        await Assert.That(kit.Context.Definitions[0].Name).IsEqualTo(default(XPathQName));
        await Assert.That(kit.Context.DefinitionOf(kit.Node(1))).IsSameReferenceAs(kit.Context.Definitions[1]);
        await Assert.That(kit.Context.DefinitionOf(kit.Document.Root!)).IsNull();
    }

    [Test]
    public async Task EnsureType_ForADefinitionThatIsOnThePath_IsACycleReportedOnce()
    {
        var kit = new ContextKit(
            "<xs:simpleType name=\"a\"><xs:restriction base=\"t:a\"/></xs:simpleType><xs:complexType name=\"c\"><xs:complexContent><xs:extension base=\"t:c\"/></xs:complexContent></xs:complexType>"
        );

        var simple = kit.Context.EnsureType(kit.Context.Definitions[0]);
        var complex = kit.Context.EnsureType(kit.Context.Definitions[1]);

        await Assert.That(simple).IsTypeOf<SimpleTypeDefinition>();
        await Assert.That(complex).IsTypeOf<ComplexTypeDefinition>();
        await Assert.That(kit.Codes()).IsEquivalentTo([XsdRuleCodes.StPropsCorrect, XsdRuleCodes.CtPropsCorrect]);
        await Assert.That(kit.Context.EnsureType(kit.Context.Definitions[0])).IsSameReferenceAs(simple);
    }

    [Test]
    public async Task EnsureType_WhileTheDefinitionIsBuilding_GivesAPlaceholder()
    {
        var kit = new ContextKit(
            "<xs:simpleType name=\"a\"><xs:restriction base=\"xs:int\"/></xs:simpleType><xs:complexType name=\"c\"/>"
        );
        kit.Context.Definitions[0].IsBuilding = true;
        kit.Context.Definitions[1].IsBuilding = true;

        var simple = kit.Context.EnsureType(kit.Context.Definitions[0]);
        var complex = kit.Context.EnsureType(kit.Context.Definitions[1]);

        await Assert.That(simple).IsTypeOf<SimpleTypeDefinition>();
        await Assert.That(complex).IsSameReferenceAs(ComplexTypeDefinition.AnyType);
        await Assert.That(kit.Codes()).IsEquivalentTo([XsdRuleCodes.StPropsCorrect, XsdRuleCodes.CtPropsCorrect]);
    }
}
