namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;

using System;
using System.Linq;
using System.Text;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using ValidationResult = NetEvolve.Schemas.ValidationResult;

/// <summary>The XSD 1.1 all group wired into the type compilation and the validation model, end to end through the public validator.</summary>
public class AllGroup11ValidationTests
{
    private const string Head = "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">";

    private const string Extension =
        "<xs:complexType name=\"base\"><xs:all>"
        + "<xs:element name=\"a\" type=\"xs:string\"/><xs:element name=\"b\" type=\"xs:string\" minOccurs=\"0\"/>"
        + "</xs:all></xs:complexType>"
        + "<xs:complexType name=\"ext\"><xs:complexContent><xs:extension base=\"base\"><xs:all>"
        + "<xs:element name=\"c\" type=\"xs:string\"/></xs:all></xs:extension></xs:complexContent></xs:complexType>"
        + "<xs:element name=\"r\" type=\"ext\"/>";

    private const string Repeating =
        "<xs:element name=\"m\"><xs:complexType><xs:all>"
        + "<xs:element name=\"x\" type=\"xs:string\" maxOccurs=\"2\"/>"
        + "<xs:element name=\"y\" type=\"xs:string\" minOccurs=\"0\"/>"
        + "<xs:any namespace=\"##other\" processContents=\"skip\" minOccurs=\"0\" maxOccurs=\"unbounded\"/>"
        + "</xs:all></xs:complexType></xs:element>";

    private static async Task<ValidationResult> Validate(
        XsdVersion version,
        string schema,
        string instance,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var validator = await SchemaValidator
            .ForXsd()
            .WithVersion(version)
            .AddSchema((Head + schema + "</xs:schema>").AsMemory(), "file:///s.xsd")
            .BuildAsync(cancellationToken);
        return await validator.ValidateAsync(instance.AsMemory(), "file:///i.xml", cancellationToken);
    }

    private static Task<ValidationResult> V11(string schema, string instance, CancellationToken cancellationToken) =>
        Validate(XsdVersion.V11, schema, instance, cancellationToken);

    [Test]
    public async Task Extension_OfAnAllGroupByAnAllGroup_AcceptsTheMembersInterleaved(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await V11(Extension, "<r><c/><a/></r>", cancellationToken);

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Extension_OfAnAllGroupByAnAllGroup_ReportsAMissingMemberOfTheBase(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await V11(Extension, "<r><c/></r>", cancellationToken);

        await Assert.That(result.Issues.Count).IsEqualTo(1);
        await Assert.That(result.Issues[0].Message).Contains("'a'");
    }

    [Test]
    public async Task Extension_OfAnAllGroupByAnAllGroup_InXsd10_IsASchemaError(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // XSD 1.0 keeps the sequence of base and own content; an all group inside a sequence is not allowed there.
        _ = await Assert.ThrowsAsync<SchemaCompilationException>(() =>
            Validate(XsdVersion.V10, Extension, "<r><a/><c/></r>", cancellationToken)
        );
    }

    [Test]
    public async Task RepeatingMember_UpToItsMaximum_IsValid(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await V11(Repeating, "<m><x/><y/><x/></m>", cancellationToken);

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task RepeatingMember_AboveItsMaximum_ReportsTheExpectedNames(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await V11(Repeating, "<m><x/><x/><x/></m>", cancellationToken);

        await Assert.That(result.Issues.Count).IsEqualTo(1);
        await Assert.That(result.Issues[0].Message).Contains("'y'").And.Contains("any element the wildcard allows");
    }

    [Test]
    public async Task RepeatingMember_BelowItsMinimum_ReportsTheMissingName(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await V11(Repeating, "<m/>", cancellationToken);

        await Assert.That(result.Issues.Count).IsEqualTo(1);
        await Assert.That(result.Issues[0].Message).Contains("'x'");
    }

    [Test]
    public async Task Wildcard_MemberTakesAnElementOfAnotherNamespace(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await V11(Repeating, "<m xmlns:f=\"urn:f\"><f:z/><x/><f:z/></m>", cancellationToken);

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Wildcard_MemberRefusesAnElementOfNoNamespace(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await V11(Repeating, "<m><x/><q/></m>", cancellationToken);

        await Assert.That(result.Issues.Count).IsEqualTo(1);
        await Assert.That(result.Issues[0].Message).Contains("'q'");
    }

    [Test]
    public async Task Wildcard_MemberBelowItsMinimum_IsNamedAsMissing(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        const string schema =
            "<xs:element name=\"m\"><xs:complexType><xs:all>"
            + "<xs:any namespace=\"##other\" processContents=\"skip\"/>"
            + "</xs:all></xs:complexType></xs:element>";

        var result = await V11(schema, "<m/>", cancellationToken);

        await Assert.That(result.Issues.Count).IsEqualTo(1);
        await Assert.That(result.Issues[0].Message).Contains("any element the wildcard allows");
    }

    [Test]
    public async Task Runs_AreReusedAcrossSiblingsOfTheSameType(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        const string schema =
            "<xs:element name=\"l\"><xs:complexType><xs:sequence>"
            + "<xs:element name=\"m\" maxOccurs=\"3\"><xs:complexType><xs:all>"
            + "<xs:element name=\"x\" type=\"xs:string\" maxOccurs=\"2\"/>"
            + "</xs:all></xs:complexType></xs:element></xs:sequence></xs:complexType></xs:element>";

        var result = await V11(schema, "<l><m><x/><x/></m><m><x/></m><m><x/><x/></m></l>", cancellationToken);

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task GroupReference_ToAnAllGroup_IsTheGroupItself(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        const string schema =
            "<xs:group name=\"g\"><xs:all><xs:element name=\"a\" type=\"xs:string\" maxOccurs=\"2\"/></xs:all></xs:group>"
            + "<xs:element name=\"r\"><xs:complexType><xs:group ref=\"g\"/></xs:complexType></xs:element>";

        var result = await V11(schema, "<r><a/><a/></r>", cancellationToken);

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task SubstitutionGroupMember_TakesTheDeclaredMember(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        const string schema =
            "<xs:element name=\"h\" type=\"xs:string\"/><xs:element name=\"h2\" type=\"xs:string\" substitutionGroup=\"h\"/>"
            + "<xs:element name=\"r\"><xs:complexType><xs:all><xs:element ref=\"h\"/></xs:all></xs:complexType></xs:element>";

        var result = await V11(schema, "<r><h2/></r>", cancellationToken);

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task NestedAllGroups_DeeperThanTheCeiling_AreReported(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        const int depth = 70;
        var builder = new StringBuilder();
        for (var i = 0; i < depth; i++)
        {
            _ = builder
                .Append("<xs:group name=\"g")
                .Append(i)
                .Append("\"><xs:all><xs:group ref=\"g")
                .Append(i + 1)
                .Append("\"/></xs:all></xs:group>");
        }

        _ = builder
            .Append("<xs:group name=\"g")
            .Append(depth)
            .Append("\"><xs:all><xs:element name=\"a\"/></xs:all></xs:group>")
            .Append("<xs:element name=\"r\"><xs:complexType><xs:group ref=\"g0\"/></xs:complexType></xs:element>");

        var thrown = await Assert.ThrowsAsync<SchemaCompilationException>(() =>
            V11(builder.ToString(), "<r/>", cancellationToken)
        );

        await Assert
            .That(thrown!.Issues.Any(i => i.Message.Contains("nests deeper", StringComparison.Ordinal)))
            .IsTrue();
    }
}
