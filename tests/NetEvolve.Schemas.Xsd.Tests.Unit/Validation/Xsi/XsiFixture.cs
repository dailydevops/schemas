namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Xsi;

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
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;
using NetEvolve.Schemas.Xsd.Validation.Core;

/// <summary>
/// Builds schema sets for the <c>xsi:type</c> and <c>xsi:nil</c> tests. Types and elements are registered globally, because
/// <c>xsi:type</c> names a global type; the namespace of the named types is <see cref="Ns"/> unless a test says otherwise.
/// </summary>
internal sealed class XsiFixture
{
    public const string Ns = "urn:t";

    public const string Xsi = "http://www.w3.org/2001/XMLSchema-instance";

    /// <summary>The declarations of the prefixes that the instances use.</summary>
    public const string Prefixes = "xmlns:xsi=\"" + Xsi + "\" xmlns:t=\"" + Ns + "\"";

    private readonly ComponentRegistry _registry;

    public XsiFixture(bool builtInTypes = false)
    {
        Collector = new ValidationIssueCollector(ValidationLimits.Default);
        Reporter = new XsdIssueReporter(Collector);
        _registry = new ComponentRegistry(Reporter);
        if (builtInTypes)
        {
            XsdBuiltInTypes.Register(_registry, XsdVersion.V11);
        }
    }

    public ValidationIssueCollector Collector { get; }

    public XsdIssueReporter Reporter { get; }

    /// <summary>Registers a complex type; <paramref name="content"/> is element content, <see langword="null"/> is empty content.</summary>
    public ComplexTypeDefinition Complex(
        string name,
        Particle? content = null,
        bool isAbstract = false,
        XsdDerivationMethod block = XsdDerivationMethod.None,
        string ns = Ns,
        bool mixed = false,
        params AttributeUse[] uses
    )
    {
        var header = new ComplexTypeHeader(
            new XPathQName(ns, name),
            SchemaFixture.Where,
            isAbstract,
            block,
            XsdDerivationMethod.None
        );
        var type = ComplexTypeDefinition.Implicit(header, mixed, content, uses, null, Reporter);
        _registry.Add(type);
        return type;
    }

    /// <summary>Registers a simple type whose datatype is chosen by the local name, see <see cref="ScriptedAssessor"/>.</summary>
    public SimpleTypeDefinition Simple(string name)
    {
        var type = SimpleTypeDefinition.FromDatatype(
            new XPathQName(Ns, name),
            SchemaFixture.Where,
            new FakeDatatype(name, DatatypeVariety.Atomic)
        );
        _registry.Add(type);
        return type;
    }

    /// <summary>Registers a global element declaration.</summary>
    public ElementDeclaration Global(
        string name,
        IXsdComponent type,
        bool nillable = false,
        ElementValueConstraint constraint = default,
        XsdDerivationMethod block = XsdDerivationMethod.None,
        string ns = ""
    )
    {
        var declaration = ElementDeclaration.Create(
            new XPathQName(ns, name),
            SchemaFixture.Where,
            true,
            default,
            new FakeSyntaxElement("complexType", 1),
            nillable,
            constraint,
            false,
            block,
            XsdDerivationMethod.None
        );
        declaration.BindInlineType(type);
        _registry.Add(declaration);
        return declaration;
    }

    /// <summary>A local element particle of a simple type in no namespace.</summary>
    public static Particle Child(string name, string type = "string", long min = 1, long max = 1) =>
        SchemaFixture.Part(name, SchemaFixture.Simple(type), min, max);

    /// <summary>A local element particle that is nillable.</summary>
    public static Particle NillableChild(string name, IXsdComponent type, long min = 1, long max = 1)
    {
        var declaration = ElementDeclaration.Create(
            new XPathQName(string.Empty, name),
            SchemaFixture.Where,
            false,
            default,
            new FakeSyntaxElement("complexType", 1),
            true,
            default,
            false,
            XsdDerivationMethod.None,
            XsdDerivationMethod.None
        );
        declaration.BindInlineType(type);
        return SchemaFixture.Part(declaration, min, max);
    }

    /// <summary>An attribute use in no namespace.</summary>
    public AttributeUse Use(string name, bool required = false, string type = "string") =>
        AttributeUse.Create(
            SchemaFixture.Where,
            required ? AttributeUseKind.Required : AttributeUseKind.Optional,
            AttributeDeclaration.Global(
                name,
                string.Empty,
                SchemaFixture.Where,
                SchemaFixture.Simple(type),
                default,
                Reporter
            )!,
            default,
            Reporter
        )!;

    public ValidationModel Model() =>
        ValidationModel.Build(_registry.Freeze(XsdVersion.V11), _registry.Components, Reporter);
}
