namespace NetEvolve.Schemas.Xsd.Tests.Integration.SchemaTests;

using System.Text;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Resources.FileSystem;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Tests.Integration.Differential;
using ValidationResult = NetEvolve.Schemas.ValidationResult;

/// <summary>The bridge reaches the internal pipeline: a rename in the package fails here, not in the suite run.</summary>
public class SchemaCompilerBridgeTests
{
    private const string Xs = "http://www.w3.org/2001/XMLSchema";

    private static string Schema(string body) => "<xs:schema xmlns:xs=\"" + Xs + "\">" + body + "</xs:schema>";

    private static IReadOnlyList<(ReadOnlyMemory<byte> Content, string BaseUri)> Documents(string uri, string text) =>
        [(Encoding.UTF8.GetBytes(text), uri)];

    private static SchemaCompilerBridge Bridge(IResourceResolver? resolver = null) =>
        new(XsdVersion.V10, resolver ?? DenyAllResourceResolver.Instance, ValidationLimits.Default);

    [Test]
    public async Task Compile_AValidSchema_IsValid(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = Bridge()
            .Compile(Documents("file:///a.xsd", Schema("<xs:element name=\"e\" type=\"xs:int\"/>")), cancellationToken);

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(result.Issues).IsEmpty();
    }

    [Test]
    public async Task Compile_ASchemaWithAnError_ReportsItWithLocationAndRuleCode(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = Bridge()
            .Compile(
                Documents("file:///a.xsd", Schema("<xs:element name=\"e\" type=\"xs:missing\"/>")),
                cancellationToken
            );

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Issues.Count).IsEqualTo(1);
        await Assert.That(result.Issues[0].Code).IsEqualTo(XsdRuleCodes.SrcResolve);
        await Assert.That(result.Issues[0].Location.Line).IsEqualTo(1);
    }

    [Test]
    public async Task Compile_AnIncludeThroughTheSandboxedResolver_IsFollowed(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var directory = new TempDirectory();
        var root = directory.Write(
            "root.xsd",
            Schema("<xs:include schemaLocation=\"part.xsd\"/><xs:element name=\"e\" type=\"p\"/>")
        );
        _ = directory.Write(
            "part.xsd",
            Schema("<xs:simpleType name=\"p\"><xs:restriction base=\"xs:string\"/></xs:simpleType>")
        );

        var result = Bridge(new SandboxedFileSystemResourceResolver(directory.Path))
            .Compile(
                [(await File.ReadAllBytesAsync(root, cancellationToken), new Uri(root).AbsoluteUri)],
                cancellationToken
            );

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Compile_ACanceledToken_ThrowsTheCancellationNotAWrapper()
    {
        using var source = new CancellationTokenSource();
#if NET8_0_OR_GREATER
        await source.CancelAsync();
#else
        source.Cancel();
#endif

        await Assert
            .That(() => Bridge().Compile(Documents("file:///a.xsd", Schema(string.Empty)), source.Token))
            .Throws<OperationCanceledException>();
    }

    [Test]
    public async Task Compile_AnExceptionOfThePipeline_IsRethrownWithoutTheReflectionWrapper(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var throwing = new ThrowingResolver();

        await Assert
            .That(() =>
                Bridge(throwing)
                    .Compile(
                        Documents("file:///a.xsd", Schema("<xs:include schemaLocation=\"b.xsd\"/>")),
                        cancellationToken
                    )
            )
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Constructor_GuardsItsArguments()
    {
        await Assert
            .That(() => new SchemaCompilerBridge(XsdVersion.V10, null!, ValidationLimits.Default))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new SchemaCompilerBridge(XsdVersion.V10, DenyAllResourceResolver.Instance, null!))
            .Throws<ArgumentNullException>();
        await Assert.That(() => Bridge().Compile(null!, CancellationToken.None)).Throws<ArgumentNullException>();
    }

    private sealed class ThrowingResolver : IResourceResolver
    {
        public ResourceResult Resolve(in ResourceRequest request, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("The resolver failed.");
    }
}
