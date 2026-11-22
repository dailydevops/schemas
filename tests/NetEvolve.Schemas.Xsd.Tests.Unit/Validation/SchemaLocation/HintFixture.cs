namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.SchemaLocation;

using System.Linq;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xsd.Tests.Unit.Composition;

/// <summary>Schemas, instances, and validators for the tests of the schema location hints.</summary>
internal static class HintFixture
{
    public const string Xs = "http://www.w3.org/2001/XMLSchema";

    public const string Xsi = "http://www.w3.org/2001/XMLSchema-instance";

    public const string Base = "urn:t";

    public const string Hinted = "urn:h";

    public const string InstanceId = "file:///i.xml";

    public const string HintedId = "file:///h.xsd";

    /// <summary>The configured schema: a root that lets any element of another namespace through, assessed when a declaration is known.</summary>
    public const string BaseSchema =
        "<xs:schema xmlns:xs=\""
        + Xs
        + "\" targetNamespace=\""
        + Base
        + "\" elementFormDefault=\"qualified\">"
        + "<xs:element name=\"r\"><xs:complexType><xs:sequence>"
        + "<xs:any namespace=\"##other\" processContents=\"lax\" minOccurs=\"0\" maxOccurs=\"unbounded\"/>"
        + "</xs:sequence></xs:complexType></xs:element></xs:schema>";

    /// <summary>The hinted schema: a root with one integer child.</summary>
    public const string HintedSchema =
        "<xs:schema xmlns:xs=\""
        + Xs
        + "\" targetNamespace=\""
        + Hinted
        + "\" elementFormDefault=\"qualified\">"
        + "<xs:element name=\"root\"><xs:complexType><xs:sequence>"
        + "<xs:element name=\"a\" type=\"xs:int\"/></xs:sequence></xs:complexType></xs:element></xs:schema>";

    /// <summary>The hinted schema without a target namespace.</summary>
    public const string NoNamespaceSchema =
        "<xs:schema xmlns:xs=\""
        + Xs
        + "\"><xs:element name=\"plain\"><xs:complexType><xs:sequence>"
        + "<xs:element name=\"a\" type=\"xs:int\"/></xs:sequence></xs:complexType></xs:element></xs:schema>";

    public static MapResolver Resolver() => new MapResolver().Add(HintedId, HintedSchema);

    public static IXsdValidator Build(
        IResourceResolver? resolver,
        bool hints = true,
        ValidationLimits? limits = null
    ) => Build(BaseSchema, resolver, hints, limits);

    /// <summary>A validator with the default options: the hints are not switched on.</summary>
    public static IXsdValidator BuildDefault(IResourceResolver? resolver)
    {
        var builder = SchemaValidator.ForXsd();
        if (resolver is not null)
        {
            _ = builder.WithResolver(resolver);
        }

        return builder.AddSchema(BaseSchema.AsMemory(), "file:///s.xsd").Build();
    }

    public static IXsdValidator Build(
        string schema,
        IResourceResolver? resolver,
        bool hints = true,
        ValidationLimits? limits = null
    )
    {
        var builder = SchemaValidator.ForXsd().WithSchemaLocationHints(hints);
        if (resolver is not null)
        {
            _ = builder.WithResolver(resolver);
        }

        if (limits is not null)
        {
            _ = builder.WithLimits(limits);
        }

        return builder.AddSchema(schema.AsMemory(), "file:///s.xsd").Build();
    }

    public static ValidationResult Run(IXsdValidator validator, string instance) =>
        validator.Validate(instance.AsSpan(), InstanceId);

    public static ValidationIssue[] Errors(ValidationResult result) =>
        result.Issues.Where(i => i.Severity == IssueSeverity.Error).ToArray();

    /// <summary>The root of the base schema with the attributes and the content given.</summary>
    public static string BaseRoot(string attributes, string content) =>
        "<r xmlns=\""
        + Base
        + "\" xmlns:h=\""
        + Hinted
        + "\" xmlns:xsi=\""
        + Xsi
        + "\" "
        + attributes
        + ">"
        + content
        + "</r>";

    /// <summary>A root in the hinted namespace with the attributes and the content given.</summary>
    public static string HintedRoot(string attributes, string content) =>
        "<h:root xmlns:h=\"" + Hinted + "\" xmlns:xsi=\"" + Xsi + "\" " + attributes + ">" + content + "</h:root>";
}
