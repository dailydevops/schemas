namespace NetEvolve.Schemas.Tests.Unit.Resources.Default;

using System;
using System.IO;
using System.Threading.Tasks;
using NetEvolve.Schemas.Resources;

internal sealed class ResourceContractTests
{
    [Test]
    public async Task Dispose_FoundStream_DisposesTheStream()
    {
        using var stream = new MemoryStream([1]);
        var result = ResourceResult.Found(stream, "sys");

        result.Dispose();

        await Assert.That(stream.CanRead).IsFalse();
        await Assert.That(result.Stream).IsSameReferenceAs(stream);
        await Assert.That(result.Status).IsEqualTo(ResourceStatus.Found);
        await Assert.That(result.SystemId).IsEqualTo("sys");
    }

    [Test]
    public async Task Dispose_CalledTwice_IsHarmless()
    {
        using var stream = new MemoryStream([1]);
        var result = ResourceResult.Found(stream);

        result.Dispose();

        await Assert.That(result.Dispose).ThrowsNothing();
    }

    [Test]
    public async Task Dispose_Copy_DisposesTheSharedStream()
    {
        using var stream = new MemoryStream([1]);
        var result = ResourceResult.Found(stream);
        var copy = result;

        copy.Dispose();

        await Assert.That(stream.CanRead).IsFalse();
    }

    [Test]
    public async Task Dispose_FoundMemory_IsNoOp()
    {
        var result = ResourceResult.Found(new byte[] { 1, 2 });

        result.Dispose();

        await Assert.That(result.Content.Length).IsEqualTo(2);
    }

    [Test]
    public async Task Dispose_DeniedAndDefault_AreNoOps()
    {
        var denied = ResourceResult.Denied();
        var blank = default(ResourceResult);

        denied.Dispose();
        blank.Dispose();

        await Assert.That(blank.Status).IsEqualTo(ResourceStatus.Denied);
    }

    [Test]
    public async Task Found_NullStream_Throws() =>
        await Assert.That(() => ResourceResult.Found((Stream)null!)).Throws<ArgumentNullException>();

    [Test]
    public async Task Request_Default_HasEmptyReference() =>
        await Assert.That(default(ResourceRequest).Reference).IsEqualTo(string.Empty);

    [Test]
    public async Task Request_NullReference_Throws() =>
        await Assert
            .That(() => new ResourceRequest(ResourceKind.SchemaDocument, null!, null, null))
            .Throws<ArgumentNullException>();

    [Test]
    public async Task Request_EmptyReference_IsAccepted()
    {
        var request = new ResourceRequest(ResourceKind.SchemaDocument, string.Empty, null, null);

        await Assert.That(request.Reference).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Request_UndefinedKind_Throws() =>
        await Assert
            .That(() => new ResourceRequest((ResourceKind)7, "a.xsd", null, null))
            .Throws<ArgumentOutOfRangeException>();
}
