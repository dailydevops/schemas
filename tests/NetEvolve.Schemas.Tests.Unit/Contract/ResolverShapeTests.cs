namespace NetEvolve.Schemas.Tests.Unit.Contract;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Resources;

public class ResolverShapeTests
{
    [Test]
    public async Task IResourceResolver_ResolvesByInRequestAndToken() =>
        await Assert
            .That(Shape.Of(typeof(IResourceResolver)))
            .IsEquivalentTo(Shape.Sorted("ResourceResult Resolve(in ResourceRequest,CancellationToken)"));

    [Test]
    public async Task DenyAllResourceResolver_IsSealedWithInstance()
    {
        await Assert.That(typeof(DenyAllResourceResolver).IsSealed).IsTrue();
        await Assert.That(typeof(DenyAllResourceResolver)).IsAssignableTo<IResourceResolver>();
        await Assert
            .That(Shape.Of(typeof(DenyAllResourceResolver)))
            .IsEquivalentTo(
                Shape.Sorted(
                    "static DenyAllResourceResolver Instance { get }",
                    "ResourceResult Resolve(in ResourceRequest,CancellationToken)"
                )
            );
    }

    [Test]
    public async Task ResourceRequest_IsReadOnlyStructWithApprovedMembers()
    {
        await Assert.That(typeof(ResourceRequest).IsValueType).IsTrue();
        await Assert
            .That(Shape.Of(typeof(ResourceRequest)))
            .IsEquivalentTo(
                Shape.Sorted(
                    ".ctor(ResourceKind,String,String,String,String,String)",
                    "ResourceKind Kind { get }",
                    "String Reference { get }",
                    "String BaseUri { get }",
                    "String ResolvedUri { get }",
                    "String PublicId { get }",
                    "String NamespaceUri { get }"
                )
            );
    }

    [Test]
    public async Task ResourceResult_IsDisposableStructWithFactories()
    {
        await Assert.That(typeof(ResourceResult).IsValueType).IsTrue();
        await Assert.That(typeof(ResourceResult)).IsAssignableTo<IDisposable>();
        await Assert
            .That(Shape.Of(typeof(ResourceResult)))
            .IsEquivalentTo(
                Shape.Sorted(
                    "static ResourceResult Found(Stream,String)",
                    "static ResourceResult Found(ReadOnlyMemory<Byte>,String)",
                    "static ResourceResult Denied(String)",
                    "static ResourceResult NotFound(String)",
                    "ResourceStatus Status { get }",
                    "Stream Stream { get }",
                    "ReadOnlyMemory<Byte> Content { get }",
                    "String SystemId { get }",
                    "String Message { get }",
                    "Void Dispose()"
                )
            );
    }

    [Test]
    public async Task ResourceStatus_DefaultIsDenied() =>
        await Assert
            .That(Shape.Of(typeof(ResourceStatus)))
            .IsEquivalentTo(Shape.Sorted("Denied=0", "Found=1", "NotFound=2"));

    [Test]
    public async Task ResourceKind_HasSevenMembers() =>
        await Assert
            .That(Shape.Of(typeof(ResourceKind)))
            .IsEquivalentTo(
                Shape.Sorted(
                    "ExternalSubset=0",
                    "ExternalEntity=1",
                    "ExternalParameterEntity=2",
                    "SchemaDocument=3",
                    "SchemaLocationHint=4",
                    "XPathDocument=5",
                    "XPathText=6"
                )
            );
}
