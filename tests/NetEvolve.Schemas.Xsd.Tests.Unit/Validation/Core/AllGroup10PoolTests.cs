namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;

using System;
using System.Linq;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.SchemaLocation;
using ValidationResult = NetEvolve.Schemas.ValidationResult;

/// <summary>The run pool of the XSD 1.0 all groups: runs are reused across elements, in any model of the session, and the XSD 1.1 mode never needs the pool.</summary>
public class AllGroup10PoolTests
{
    private const string Head = "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">";

    private const string Schema =
        "<xs:element name=\"r\"><xs:complexType><xs:sequence>"
        + "<xs:element name=\"g\" maxOccurs=\"unbounded\"><xs:complexType><xs:all>"
        + "<xs:element name=\"a\" type=\"xs:string\"/><xs:element name=\"b\" type=\"xs:string\" minOccurs=\"0\"/>"
        + "</xs:all></xs:complexType></xs:element></xs:sequence></xs:complexType></xs:element>";

    private static async Task<ValidationResult> Validate(
        XsdVersion version,
        string instance,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var validator = await SchemaValidator
            .ForXsd()
            .WithVersion(version)
            .AddSchema((Head + Schema + "</xs:schema>").AsMemory(), "file:///s.xsd")
            .BuildAsync(cancellationToken);
        return await validator.ValidateAsync(instance.AsMemory(), "file:///i.xml", cancellationToken);
    }

    [Test]
    [Arguments(XsdVersion.V10)]
    [Arguments(XsdVersion.V11)]
    public async Task Validate_RepeatedAllGroups_ReusePooledRuns(
        XsdVersion version,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await Validate(version, "<r><g><a/></g><g><b/><a/></g><g><a/><b/></g></r>", cancellationToken);

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    [Arguments(XsdVersion.V10)]
    [Arguments(XsdVersion.V11)]
    public async Task Validate_AllGroupAfterAnInvalidOne_StartsFromAResetRun(
        XsdVersion version,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await Validate(version, "<r><g><b/></g><g><a/></g><g><a/><a/></g></r>", cancellationToken);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Issues.Count(i => i.Severity == IssueSeverity.Error)).IsEqualTo(2);
    }

    [Test]
    public async Task Validate_XsdV10_HintExtendsTheModelAfterAnAllGroupWasPooled(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        const string schema =
            "<xs:schema xmlns:xs=\""
            + HintFixture.Xs
            + "\" targetNamespace=\""
            + HintFixture.Base
            + "\" elementFormDefault=\"qualified\"><xs:element name=\"r\"><xs:complexType><xs:sequence>"
            + "<xs:element name=\"g\"><xs:complexType><xs:all><xs:element name=\"a\" type=\"xs:string\"/></xs:all></xs:complexType></xs:element>"
            + "<xs:any namespace=\"##other\" processContents=\"lax\" minOccurs=\"0\" maxOccurs=\"unbounded\"/>"
            + "</xs:sequence></xs:complexType></xs:element></xs:schema>";
        var validator = await SchemaValidator
            .ForXsd()
            .WithVersion(XsdVersion.V10)
            .WithSchemaLocationHints(true)
            .WithResolver(HintFixture.Resolver())
            .AddSchema(schema.AsMemory(), "file:///s.xsd")
            .BuildAsync(cancellationToken);
        var instance = HintFixture.BaseRoot(
            string.Empty,
            "<g><a/></g><h:root xsi:schemaLocation=\"urn:h h.xsd\"><h:a>1</h:a></h:root>"
        );

        var result = HintFixture.Run(validator, instance);

        await Assert.That(HintFixture.Errors(result)).IsEmpty();
    }
}
