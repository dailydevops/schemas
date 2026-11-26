namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;

using System;
using ValidationResult = NetEvolve.Schemas.ValidationResult;

/// <summary>Substitution group members in sequence and choice content models, end to end through the streaming validator.</summary>
public class SubstitutionAutomatonValidationTests
{
    private const string Head = "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">";

    private const string Group =
        "<xs:element name=\"h\" type=\"xs:string\"/><xs:element name=\"m\" type=\"xs:string\" substitutionGroup=\"h\"/>"
        + "<xs:element name=\"ah\" type=\"xs:string\" abstract=\"true\"/>"
        + "<xs:element name=\"am\" type=\"xs:string\" substitutionGroup=\"ah\"/>";

    private const string Sequence =
        "<xs:element name=\"r\"><xs:complexType><xs:sequence>"
        + "<xs:element ref=\"h\"/><xs:element name=\"z\" type=\"xs:string\" minOccurs=\"0\"/>"
        + "</xs:sequence></xs:complexType></xs:element>";

    private const string Choice =
        "<xs:element name=\"r\"><xs:complexType><xs:choice maxOccurs=\"unbounded\">"
        + "<xs:element ref=\"h\"/><xs:element name=\"z\" type=\"xs:string\"/>"
        + "</xs:choice></xs:complexType></xs:element>";

    private const string AbstractHead =
        "<xs:element name=\"r\"><xs:complexType><xs:sequence>"
        + "<xs:element ref=\"ah\"/></xs:sequence></xs:complexType></xs:element>";

    private const string Nested =
        "<xs:element name=\"r\"><xs:complexType><xs:sequence><xs:element name=\"i\"><xs:complexType><xs:choice>"
        + "<xs:element ref=\"h\"/><xs:element name=\"z\" type=\"xs:string\"/>"
        + "</xs:choice></xs:complexType></xs:element></xs:sequence></xs:complexType></xs:element>";

    private static async Task<ValidationResult> Validate(
        string schema,
        string instance,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var validator = await SchemaValidator
            .ForXsd()
            .AddSchema((Head + Group + schema + "</xs:schema>").AsMemory(), "file:///s.xsd")
            .BuildAsync(cancellationToken);
        return await validator.ValidateAsync(instance.AsMemory(), "file:///i.xml", cancellationToken);
    }

    [Test]
    [Arguments(Sequence, "<r><h/></r>", true)]
    [Arguments(Sequence, "<r><m/><z/></r>", true)]
    [Arguments(Sequence, "<r><z/></r>", false)]
    [Arguments(Sequence, "<r><m/><m/></r>", false)]
    [Arguments(Choice, "<r><h/><m/><z/><m/></r>", true)]
    [Arguments(Choice, "<r><x/></r>", false)]
    [Arguments(AbstractHead, "<r><am/></r>", true)]
    [Arguments(AbstractHead, "<r><ah/></r>", false)]
    [Arguments(Nested, "<r><i><m/></i></r>", true)]
    [Arguments(Nested, "<r><i><h/></i></r>", true)]
    [Arguments(Nested, "<r><i><x/></i></r>", false)]
    public async Task SubstitutionMembers_InAutomatonContent_AreMatched(
        string schema,
        string instance,
        bool valid,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await Validate(schema, instance, cancellationToken);

        await Assert.That(result.IsValid).IsEqualTo(valid);
    }
}
