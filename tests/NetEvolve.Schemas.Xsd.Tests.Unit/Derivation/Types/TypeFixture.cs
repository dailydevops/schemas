namespace NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Types;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Derivation.Types;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;
using NetEvolve.Schemas.Xsd.Wildcards.Core;

/// <summary>Builds component fixtures for the derivation checks; building reports to a collector of its own.</summary>
internal sealed class TypeFixture
{
    public const string Tns = "urn:test";
    public const string SchemaNamespace = "http://www.w3.org/2001/XMLSchema";

    public static readonly DocumentLocation Where = new("file:///a.xsd", 7, 3);

    private static readonly AttributeUse[] NoUses = [];

    private readonly ValidationIssueCollector _built = new(ValidationLimits.Default);
    private readonly ValidationIssueCollector _checked = new(ValidationLimits.Default);

    public XsdIssueReporter Reporter => new(_checked);

    public IReadOnlyList<ValidationIssue> Issues => _checked.ToResult().Issues;

    public IReadOnlyList<ValidationIssue> BuildIssues => _built.ToResult().Issues;

    private XsdIssueReporter Builder => new(_built);

    public static TypeDerivationCheck Checker(XsdVersion version = XsdVersion.V11) => new(version);

    public static XPathQName Name(string name) => name.Length == 0 ? default : new XPathQName(Tns, name);

    /// <summary>The registered built-in type, as a name lookup in a schema set finds it.</summary>
    public static XsdBuiltInType Registered(string localName)
    {
        var types = XsdBuiltInTypes.For(XsdVersion.V11);
        for (var i = 0; i < types.Count; i++)
        {
            if (string.Equals(types[i].Name.LocalName, localName, StringComparison.Ordinal))
            {
                return (XsdBuiltInType)types[i];
            }
        }

        throw new InvalidOperationException("No built-in type " + localName);
    }

    /// <summary>A built-in simple type as the components of a schema see it: a wrapper without a base type.</summary>
    public static SimpleTypeDefinition Xs(string localName)
    {
        var registered = Registered(localName);
        return SimpleTypeDefinition.FromDatatype(registered.Name, Where, registered.Descriptor!);
    }

    public SimpleTypeDefinition Restrict(string name, IXsdComponent baseType) =>
        SimpleTypeDefinition.Restrict(Name(name), Where, XsdDerivationMethod.None, baseType, [], Builder)!;

    public SimpleTypeDefinition ListOf(string name, IXsdComponent itemType) =>
        SimpleTypeDefinition.List(Name(name), Where, XsdDerivationMethod.None, itemType, [], Builder)!;

    public SimpleTypeDefinition UnionOf(string name, params IXsdComponent[] members) =>
        SimpleTypeDefinition.Union(Name(name), Where, XsdDerivationMethod.None, members, [], Builder)!;

    public static ComplexTypeHeader Header(
        string name,
        XsdDerivationMethod final = XsdDerivationMethod.None,
        XsdDerivationMethod block = XsdDerivationMethod.None
    ) => new(Name(name), Where, false, block, final);

    public static Particle Element(string name, long min = 1, long max = 1) =>
        Particle.Create(Where, min, max, new FakeComponent(XsdComponentKind.ElementDeclaration, name));

    public static Particle Group(ModelGroupCompositor compositor, long min, long max, params Particle[] particles) =>
        Particle.Create(Where, min, max, ModelGroup.Create(compositor, Where, particles));

    public static Particle Sequence(params Particle[] particles) =>
        Group(ModelGroupCompositor.Sequence, 1, 1, particles);

    public static Wildcard Wild(NamespaceConstraint constraint, XsdProcessContents process = XsdProcessContents.Lax) =>
        Wildcard.Create(constraint, process, Where);

    public static NamespaceConstraint Namespaces(params string[] names) => NamespaceConstraint.Set(names);

    public AttributeUse Use(
        string name,
        AttributeUseKind kind = AttributeUseKind.Optional,
        string type = "string",
        AttributeValueConstraintKind constraint = AttributeValueConstraintKind.None,
        string? value = null
    ) =>
        AttributeUse.Create(
            Where,
            kind,
            AttributeDeclaration.Global(name, Tns, Where, Xs(type), default, Builder)!,
            new AttributeValueConstraint(constraint, value!),
            Builder
        )!;

    /// <summary>A type without simple or complex content: a restriction of the ur-type.</summary>
    public ComplexTypeDefinition Root(
        string name,
        Particle? content = null,
        bool mixed = false,
        IReadOnlyList<AttributeUse>? uses = null,
        IWildcard? wildcard = null,
        XsdDerivationMethod final = XsdDerivationMethod.None,
        XsdDerivationMethod block = XsdDerivationMethod.None
    ) => ComplexTypeDefinition.Implicit(Header(name, final, block), mixed, content, uses ?? NoUses, wildcard, Builder);

    public ComplexTypeDefinition Complex(
        string name,
        XsdDerivationMethod method,
        IXsdComponent baseType,
        Particle? content = null,
        bool mixed = false,
        IReadOnlyList<AttributeUse>? uses = null,
        IWildcard? wildcard = null
    ) =>
        ComplexTypeDefinition.FromComplexContent(
            Header(name),
            method,
            baseType,
            mixed,
            content,
            uses ?? NoUses,
            wildcard,
            Builder
        )!;

    public ComplexTypeDefinition SimpleContent(
        string name,
        XsdDerivationMethod method,
        IXsdComponent baseType,
        SimpleTypeDefinition? simpleType = null,
        IReadOnlyList<AttributeUse>? uses = null,
        IWildcard? wildcard = null
    ) =>
        ComplexTypeDefinition.FromSimpleContent(
            Header(name),
            method,
            baseType,
            simpleType,
            uses ?? NoUses,
            wildcard,
            Builder
        )!;
}
