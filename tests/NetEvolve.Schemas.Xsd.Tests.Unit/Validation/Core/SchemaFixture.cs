namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;

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
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;
using NetEvolve.Schemas.Xsd.Tests.Unit.SimpleTypes.Definitions;
using NetEvolve.Schemas.Xsd.Validation.Core;
using NetEvolve.Schemas.Xsd.Wildcards.Core;

/// <summary>Builds components through their factories, the way the component builders will, and a validation model over them.</summary>
internal sealed class SchemaFixture
{
    public const string Xs = "http://www.w3.org/2001/XMLSchema";

    public static readonly DocumentLocation Where = new("file:///s.xsd", 1, 1);

    private readonly ComponentRegistry _registry;

    public SchemaFixture()
    {
        Collector = new ValidationIssueCollector(ValidationLimits.Default);
        Reporter = new XsdIssueReporter(Collector);
        _registry = new ComponentRegistry(Reporter);
    }

    public ValidationIssueCollector Collector { get; }

    public XsdIssueReporter Reporter { get; }

    public static IXsdComponent AnyType => XsdBuiltInTypes.For(XsdVersion.V11)[0];

    public static SimpleTypeDefinition Simple(string local) =>
        SimpleTypeDefinition.FromDatatype(
            new XPathQName(Xs, local),
            Where,
            new FakeDatatype(local, DatatypeVariety.Atomic)
        );

    public static ComplexTypeHeader Header(string name = "", bool isAbstract = false) =>
        new(
            name.Length == 0 ? default : new XPathQName(string.Empty, name),
            Where,
            isAbstract,
            XsdDerivationMethod.None,
            XsdDerivationMethod.None
        );

    public static ElementDeclaration Element(
        string name,
        IXsdComponent? type,
        bool isGlobal = false,
        ElementValueConstraint constraint = default,
        bool isAbstract = false,
        string namespaceName = ""
    )
    {
        var declaration = ElementDeclaration.Create(
            new XPathQName(namespaceName, name),
            Where,
            isGlobal,
            default,
            new FakeSyntaxElement("complexType", 1),
            false,
            constraint,
            isAbstract,
            XsdDerivationMethod.None,
            XsdDerivationMethod.None
        );
        if (type is not null)
        {
            declaration.BindInlineType(type);
        }

        return declaration;
    }

    public static Particle Part(IXsdComponent term, long min = 1, long max = 1) =>
        Particle.Create(Where, min, max, term);

    public static Particle Part(string name, IXsdComponent? type, long min = 1, long max = 1) =>
        Part(Element(name, type ?? AnyType), min, max);

    public static Particle Group(ModelGroupCompositor compositor, long min, long max, params Particle[] particles) =>
        Part(ModelGroup.Create(compositor, Where, particles), min, max);

    public static Particle Seq(params Particle[] particles) => Group(ModelGroupCompositor.Sequence, 1, 1, particles);

    public static Particle Choice(params Particle[] particles) => Group(ModelGroupCompositor.Choice, 1, 1, particles);

    public static Particle All(params Particle[] particles) => Group(ModelGroupCompositor.All, 1, 1, particles);

    public static Wildcard Wild(string namespaces, XsdProcessContents process)
    {
        var keyword = process switch
        {
            XsdProcessContents.Strict => "strict",
            XsdProcessContents.Lax => "lax",
            _ => "skip",
        };
        _ = Wildcard.TryCreate(namespaces, keyword, "urn:t", Where, out var wildcard);
        return wildcard!;
    }

    public AttributeDeclaration AttrDecl(
        string name,
        SimpleTypeDefinition type,
        AttributeValueConstraint constraint = default,
        string namespaceName = ""
    ) => AttributeDeclaration.Global(name, namespaceName, Where, type, constraint, Reporter)!;

    public AttributeUse Use(
        string name,
        string type = "string",
        AttributeUseKind kind = AttributeUseKind.Optional,
        AttributeValueConstraint constraint = default
    ) => AttributeUse.Create(Where, kind, AttrDecl(name, Simple(type), default), constraint, Reporter)!;

    public ComplexTypeDefinition ElementOnly(Particle content, params AttributeUse[] uses) =>
        ComplexTypeDefinition.Implicit(Header(), false, content, uses, null, Reporter);

    public ComplexTypeDefinition Mixed(Particle content, params AttributeUse[] uses) =>
        ComplexTypeDefinition.Implicit(Header(), true, content, uses, null, Reporter);

    public ComplexTypeDefinition Empty(params AttributeUse[] uses) =>
        ComplexTypeDefinition.Implicit(Header(), false, null, uses, null, Reporter);

    public ComplexTypeDefinition WithAttributes(IWildcard? wildcard, params AttributeUse[] uses) =>
        ComplexTypeDefinition.Implicit(Header(), false, null, uses, wildcard, Reporter);

    public ComplexTypeDefinition SimpleContent(string type, params AttributeUse[] uses) =>
        ComplexTypeDefinition.FromSimpleContent(
            Header(),
            XsdDerivationMethod.Extension,
            Simple(type),
            null,
            uses,
            null,
            Reporter
        )!;

    /// <summary>Registers a global element, so the validator finds it as a root and as a wildcard target.</summary>
    public ElementDeclaration Global(
        string name,
        IXsdComponent? type,
        ElementValueConstraint constraint = default,
        bool isAbstract = false,
        string namespaceName = ""
    )
    {
        var declaration = Element(name, type, true, constraint, isAbstract, namespaceName);
        _registry.Add(declaration);
        return declaration;
    }

    /// <summary>Registers a global attribute, so a wildcard finds it by name.</summary>
    public AttributeDeclaration GlobalAttribute(string name, SimpleTypeDefinition type, string namespaceName = "")
    {
        var declaration = AttrDecl(name, type, default, namespaceName);
        _registry.Add(declaration);
        return declaration;
    }

    public ValidationModel Model()
    {
        var set = _registry.Freeze(XsdVersion.V11);
        return ValidationModel.Build(set, _registry.Components, Reporter);
    }
}
