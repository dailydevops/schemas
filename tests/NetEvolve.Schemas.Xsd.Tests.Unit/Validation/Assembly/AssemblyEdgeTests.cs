namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Assembly;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>Edges of the assembly that the main tests pass by: events without a hook, constructs that look like a 1.1 construct, and the size check of a seekable stream.</summary>
public class AssemblyEdgeTests
{
    [Test]
    public async Task Validate_CommentsProcessingInstructionsAndCdata_AreForwardedAndIgnored()
    {
        var instance =
            "<?xml version=\"1.0\"?><?before x?><!-- c --><root xmlns=\"urn:t\" id=\"a\"><!-- inner --><?inner y?>"
            + "<item>1</item><item><![CDATA[2]]></item></root><!-- after --><?after z?>";

        var result = AssemblyFixture.Run(AssemblyFixture.Build(AssemblyFixture.Schema), instance);

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Build_SeekableStreamLargerThanMaxResourceSize_ThrowsWithTheResourceLimitCode(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(AssemblyFixture.Schema));
        var builder = SchemaValidator
            .ForXsd()
            .WithLimits(ValidationLimits.Default.WithMaxResourceSize(10))
            .AddSchema(stream);

        var exception = await Assert
            .That(async () => await builder.BuildAsync(cancellationToken))
            .Throws<SchemaCompilationException>();

        await Assert.That(exception!.Issues[0].Code).IsEqualTo(CoreDiagnosticCodes.ResourceSizeLimitExceeded);
        await Assert
            .That(stream.Position)
            .IsEqualTo(0L)
            .Because("a stream that is too long is rejected without a read");
    }

    [Test]
    public async Task Build_Version11_ConstructsThatLookLikeAnXsd11Construct_AreNotReported()
    {
        // One substitution group head is XSD 1.0; a type of another name, a type of another namespace, and the local name alone.
        var schema = AssemblyFixture.WrapSchema(
            "<xs:element name=\"head\" type=\"xs:string\"/>"
                + "<xs:element name=\"member\" type=\"xs:string\" substitutionGroup=\"t:head\"/>"
                + "<xs:simpleType name=\"u\"><xs:union memberTypes=\"xs:string xs:int\"/></xs:simpleType>"
                + "<xs:simpleType name=\"precisionDecimal\"><xs:restriction base=\"xs:decimal\"/></xs:simpleType>"
                + "<xs:element name=\"e\" type=\"t:precisionDecimal\"/>"
        );

        var failure = TryBuild(schema);

        await Assert.That(failure.Any(i => i.Code.Number is >= 4060 and <= 4077)).IsFalse();
    }

    [Test]
    public async Task Build_Version11_PrecisionDecimalInAUnionOfMemberTypes_IsReported()
    {
        var schema = AssemblyFixture.WrapSchema(
            "<xs:simpleType name=\"u\"><xs:union memberTypes=\"xs:string xs:precisionDecimal\"/></xs:simpleType>"
        );

        var failure = TryBuild(schema);

        await Assert.That(failure.Any(i => i.Code == XsdRuleCodes.NotYetSupportedPrecisionDecimal)).IsTrue();
    }

    [Test]
    public async Task Build_Version11_AGlobalDeclarationWithATargetNamespace_IsNotTheLocalConstruct()
    {
        var schema = AssemblyFixture.WrapSchema(
            "<xs:attribute name=\"a\" type=\"xs:string\" targetNamespace=\"urn:t\"/>"
        );

        var failure = TryBuild(schema);

        await Assert.That(failure.Any(i => i.Code == XsdRuleCodes.NotYetSupportedLocalTargetNamespace)).IsFalse();
    }

    private static IReadOnlyList<ValidationIssue> TryBuild(string schema)
    {
        try
        {
            _ = AssemblyFixture.Build(schema);
            return [];
        }
        catch (SchemaCompilationException exception)
        {
            return exception.Issues;
        }
    }
}
