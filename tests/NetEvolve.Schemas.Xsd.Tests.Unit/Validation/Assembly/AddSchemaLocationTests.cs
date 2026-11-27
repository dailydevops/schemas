#pragma warning disable CA1849, VSTHRD103 // The synchronous Build is the member under test.

namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Assembly;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xsd.Tests.Unit.Composition;

public class AddSchemaLocationTests
{
    private const string Location = "file:///loc/s.xsd";

    [Test]
    public async Task AddSchemaLocation_ReturnsTheSameBuilder()
    {
        var builder = SchemaValidator.ForXsd();

        await Assert.That(builder.AddSchemaLocation(Location)).IsSameReferenceAs(builder);
    }

    [Test]
    public async Task AddSchemaLocation_NullReference_Throws()
    {
        var builder = SchemaValidator.ForXsd();

        await Assert.That(() => builder.AddSchemaLocation(null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task AddSchemaLocation_EmptyReference_Throws()
    {
        var builder = SchemaValidator.ForXsd();

        await Assert.That(() => builder.AddSchemaLocation(string.Empty)).Throws<ArgumentException>();
    }

    [Test]
    public async Task Build_LoadsTheDocumentThroughTheResolverAsSchemaDocumentWithTheNamespace()
    {
        var resolver = new MapResolver().Add(Location, AssemblyFixture.Schema);

        var validator = SchemaValidator
            .ForXsd()
            .WithResolver(resolver)
            .AddSchemaLocation(Location, AssemblyFixture.Tns)
            .Build();

        await Assert.That(resolver.Calls.Count).IsEqualTo(1);
        await Assert.That(resolver.Calls[0].Kind).IsEqualTo(ResourceKind.SchemaDocument);
        await Assert.That(resolver.Calls[0].Reference).IsEqualTo(Location);
        await Assert.That(resolver.Calls[0].NamespaceUri).IsEqualTo(AssemblyFixture.Tns);
        await Assert.That(AssemblyFixture.Run(validator, AssemblyFixture.ValidInstance).IsValid).IsTrue();
        await Assert.That(AssemblyFixture.Run(validator, AssemblyFixture.InvalidInstance).IsValid).IsFalse();
    }

    [Test]
    public async Task Build_WithoutNamespace_SendsNoNamespaceAndSkipsTheNamespaceCheck()
    {
        var resolver = new MapResolver().Add(Location, AssemblyFixture.Schema);

        var validator = SchemaValidator.ForXsd().WithResolver(resolver).AddSchemaLocation(Location).Build();

        await Assert.That(resolver.Calls[0].NamespaceUri).IsNull();
        await Assert.That(AssemblyFixture.Run(validator, AssemblyFixture.ValidInstance).IsValid).IsTrue();
    }

    [Test]
    public async Task BuildAsync_LoadsTheDocumentThroughTheResolver(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = new MapResolver().Add(Location, AssemblyFixture.Schema);

        var validator = await SchemaValidator
            .ForXsd()
            .WithResolver(resolver)
            .AddSchemaLocation(Location, AssemblyFixture.Tns)
            .BuildAsync(cancellationToken);

        await Assert.That(AssemblyFixture.Run(validator, AssemblyFixture.ValidInstance).IsValid).IsTrue();
    }

    [Test]
    public async Task Build_CombinesALocationWithAnAddedDocument()
    {
        var resolver = new MapResolver().Add(Location, AssemblyFixture.Schema);
        var other = AssemblyFixture
            .WrapSchema("<xs:element name=\"other\" type=\"xs:int\"/>")
            .Replace(AssemblyFixture.Tns, "urn:o", StringComparison.Ordinal);

        var validator = SchemaValidator
            .ForXsd()
            .WithResolver(resolver)
            .AddSchema(other.AsMemory(), "file:///o.xsd")
            .AddSchemaLocation(Location, AssemblyFixture.Tns)
            .Build();

        await Assert.That(AssemblyFixture.Run(validator, AssemblyFixture.ValidInstance).IsValid).IsTrue();
        await Assert.That(AssemblyFixture.Run(validator, "<other xmlns=\"urn:o\">1</other>").IsValid).IsTrue();
    }

    [Test]
    public async Task Build_RefusedByTheDefaultResolver_ThrowsWithTheDenial()
    {
        var exception = AssemblyFixture.Failure(() => SchemaValidator.ForXsd().AddSchemaLocation(Location).Build());

        await Assert.That(exception.Issues.Any(i => i.Code == CoreDiagnosticCodes.ResourceDenied)).IsTrue();
    }

    [Test]
    public async Task Build_MissingDocument_ThrowsWithNotFound()
    {
        var exception = AssemblyFixture.Failure(() =>
            SchemaValidator.ForXsd().WithResolver(new MapResolver()).AddSchemaLocation(Location).Build()
        );

        await Assert.That(exception.Issues.Any(i => i.Code == CoreDiagnosticCodes.ResourceNotFound)).IsTrue();
    }

    [Test]
    public async Task Build_DocumentLargerThanTheLimit_ThrowsWithTheSizeLimit()
    {
        var resolver = new MapResolver().Add(Location, AssemblyFixture.Schema);

        var exception = AssemblyFixture.Failure(() =>
            SchemaValidator
                .ForXsd()
                .WithLimits(ValidationLimits.Default.WithMaxResourceSize(10))
                .WithResolver(resolver)
                .AddSchemaLocation(Location)
                .Build()
        );

        await Assert.That(exception.Issues.Any(i => i.Code == CoreDiagnosticCodes.ResourceSizeLimitExceeded)).IsTrue();
    }

    [Test]
    public async Task Build_TargetNamespaceDiffersFromTheNamespace_Throws()
    {
        var resolver = new MapResolver().Add(Location, AssemblyFixture.Schema);

        var exception = AssemblyFixture.Failure(() =>
            SchemaValidator.ForXsd().WithResolver(resolver).AddSchemaLocation(Location, "urn:other").Build()
        );

        await Assert
            .That(exception.Issues.Any(i => i.Message.Contains("urn:other", StringComparison.Ordinal)))
            .IsTrue();
    }

    [Test]
    public async Task Build_UnreadableDocument_ThrowsWithTheReadError()
    {
        var resolver = new MapResolver().Add(Location, "<xs:schema");

        var exception = AssemblyFixture.Failure(() =>
            SchemaValidator.ForXsd().WithResolver(resolver).AddSchemaLocation(Location).Build()
        );

        await Assert.That(exception.Issues.Count).IsGreaterThanOrEqualTo(1);
    }
}
