namespace NetEvolve.Schemas.Tests.Unit.Contract;

using System.Threading.Tasks;
using NetEvolve.Schemas.Resources;

public class ValueTypeContractTests
{
    [Test]
    public async Task DiagnosticCode_Equality_IsByNumber()
    {
        var a = new DiagnosticCode(42);
        var same = new DiagnosticCode(42);
        var other = new DiagnosticCode(43);

        await Assert.That(a.Equals(same)).IsTrue();
        await Assert.That(a.Equals((object)same)).IsTrue();
        await Assert.That(a.Equals((object)other)).IsFalse();
        await Assert.That(a.Equals("NES0042")).IsFalse();
        await Assert.That(a == same).IsTrue();
        await Assert.That(a != other).IsTrue();
        await Assert.That(a.GetHashCode()).IsEqualTo(same.GetHashCode());
    }

    [Test]
    public async Task DiagnosticCodeRange_Equality_IsByBounds()
    {
        var a = new DiagnosticCodeRange(10, 20);
        var same = new DiagnosticCodeRange(10, 20);
        var otherLast = new DiagnosticCodeRange(10, 21);
        var otherFirst = new DiagnosticCodeRange(11, 20);

        await Assert.That(a.Equals(same)).IsTrue();
        await Assert.That(a.Equals((object)same)).IsTrue();
        await Assert.That(a.Equals((object)otherLast)).IsFalse();
        await Assert.That(a.Equals("NES0010-NES0020")).IsFalse();
        await Assert.That(a == same).IsTrue();
        await Assert.That(a != otherFirst).IsTrue();
        await Assert.That(a.GetHashCode()).IsEqualTo(same.GetHashCode());
        await Assert.That(a.GetHashCode()).IsNotEqualTo(otherLast.GetHashCode());
    }

    [Test]
    public async Task ResourceRequest_ExposesAllConstructorArguments()
    {
        var request = new ResourceRequest(
            ResourceKind.SchemaDocument,
            "a.xsd",
            "file:///base/",
            "file:///base/a.xsd",
            "pub",
            "urn:ns"
        );

        await Assert.That(request.Kind).IsEqualTo(ResourceKind.SchemaDocument);
        await Assert.That(request.Reference).IsEqualTo("a.xsd");
        await Assert.That(request.BaseUri).IsEqualTo("file:///base/");
        await Assert.That(request.ResolvedUri).IsEqualTo("file:///base/a.xsd");
        await Assert.That(request.PublicId).IsEqualTo("pub");
        await Assert.That(request.NamespaceUri).IsEqualTo("urn:ns");
    }

    [Test]
    public async Task ResourceRequest_OptionalArguments_DefaultToNull()
    {
        var request = new ResourceRequest(ResourceKind.SchemaDocument, "a.xsd", null, null);

        await Assert.That(request.BaseUri).IsNull();
        await Assert.That(request.ResolvedUri).IsNull();
        await Assert.That(request.PublicId).IsNull();
        await Assert.That(request.NamespaceUri).IsNull();
    }

    [Test]
    public async Task ResourceResult_FoundBytes_CarriesContentAndSystemId()
    {
        byte[] bytes = [1, 2, 3];
        using var result = ResourceResult.Found(bytes, "sys");

        await Assert.That(result.Status).IsEqualTo(ResourceStatus.Found);
        await Assert.That(result.Content.ToArray()).IsEquivalentTo(bytes);
        await Assert.That(result.SystemId).IsEqualTo("sys");
        await Assert.That(result.Stream).IsNull();
        await Assert.That(result.Message).IsNull();
    }

    [Test]
    public async Task ResourceResult_DeniedAndNotFound_CarryStatusAndMessage()
    {
        var denied = ResourceResult.Denied("no");
        var notFound = ResourceResult.NotFound();

        await Assert.That(denied.Status).IsEqualTo(ResourceStatus.Denied);
        await Assert.That(denied.Message).IsEqualTo("no");
        await Assert.That(denied.Content.IsEmpty).IsTrue();
        await Assert.That(denied.SystemId).IsNull();
        await Assert.That(notFound.Status).IsEqualTo(ResourceStatus.NotFound);
        await Assert.That(notFound.Message).IsNull();
        await Assert.That(notFound.Stream).IsNull();
    }
}
