namespace NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

using System.Net;
using System.Net.Sockets;

/// <summary>The baseline adapter, checked with small schemas on the leg that runs the baseline.</summary>
[BaselineLeg]
public class SystemXmlSchemaEngineTests
{
    private const string Schema =
        "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">\n  <xs:element name=\"a\" type=\"xs:int\"/>\n</xs:schema>";

    private static async Task<EngineResult> RunAsync(
        TempDirectory directory,
        string schema,
        string? instance,
        CancellationToken cancellationToken,
        params (string Name, string Content)[] more
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var (name, content) in more)
        {
            directory.Write(name, content);
        }

        var differentialCase = new DifferentialCase(
            "case",
            [directory.Write("schema.xsd", schema)],
            instance is null ? null : directory.Write("instance.xml", instance),
            Verdict.Valid,
            Verdict.Valid
        );
        return await new SystemXmlSchemaEngine().RunAsync(differentialCase, cancellationToken);
    }

    [Test]
    public async Task Run_ValidSchemaAndInstance_IsValidWithoutIssues(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var directory = new TempDirectory();

        var result = await RunAsync(directory, Schema, "<a>1</a>", cancellationToken);

        await Assert.That(result.Schema).IsEqualTo(Verdict.Valid);
        await Assert.That(result.Instance).IsEqualTo(Verdict.Valid);
        await Assert.That(result.Issues).IsEmpty();
    }

    [Test]
    public async Task Run_SchemaThatThrowsAnArgumentException_IsInvalidWithoutPositions(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var directory = new TempDirectory();
        const string emptyId =
            "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"><xs:attributeGroup name=\"abc\" id=\"\"/></xs:schema>";

        var result = await RunAsync(directory, emptyId, "<a/>", cancellationToken);

        await Assert.That(result.Schema).IsEqualTo(Verdict.Invalid);
        await Assert.That(result.Instance).IsEqualTo(Verdict.NotRun);
        await Assert.That(result.Issues).IsEmpty();
    }

    [Test]
    public async Task Run_SchemaOnly_LeavesTheInstanceNotRun(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var directory = new TempDirectory();

        var result = await RunAsync(directory, Schema, null, cancellationToken);

        await Assert.That(result.Schema).IsEqualTo(Verdict.Valid);
        await Assert.That(result.Instance).IsEqualTo(Verdict.NotRun);
    }

    [Test]
    public async Task Run_InvalidSchema_IsInvalidWithItsPositionAndSkipsTheInstance(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var directory = new TempDirectory();
        var schema = Schema.Replace("xs:int", "xs:nope", StringComparison.Ordinal);

        var result = await RunAsync(directory, schema, "<a>1</a>", cancellationToken);

        await Assert.That(result.Schema).IsEqualTo(Verdict.Invalid);
        await Assert.That(result.Instance).IsEqualTo(Verdict.NotRun);
        await Assert.That(result.Issues.Count).IsEqualTo(1);
        await Assert.That(result.Issues[0].Line).IsEqualTo(2);
    }

    [Test]
    public async Task Run_InvalidInstance_IsInvalidWithItsPosition(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var directory = new TempDirectory();

        var result = await RunAsync(directory, Schema, "<a>not a number</a>", cancellationToken);

        await Assert.That(result.Schema).IsEqualTo(Verdict.Valid);
        await Assert.That(result.Instance).IsEqualTo(Verdict.Invalid);
        await Assert.That(result.Issues.Count).IsEqualTo(1);
        await Assert.That(result.Issues[0].Line).IsEqualTo(1);
    }

    [Test]
    public async Task Run_InstanceThatIsNotWellFormed_IsInvalidWithItsPosition(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var directory = new TempDirectory();

        var result = await RunAsync(directory, Schema, "<a>\n1</b>", cancellationToken);

        await Assert.That(result.Instance).IsEqualTo(Verdict.Invalid);
        await Assert.That(result.Issues.Count).IsEqualTo(1);
        await Assert.That(result.Issues[0].Line).IsEqualTo(2);
    }

    [Test]
    public async Task Run_IncludeWithARelativeLocation_IsResolvedFromTheLocalFile(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var directory = new TempDirectory();
        const string Including =
            "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">\n  <xs:include schemaLocation=\"part.xsd\"/>\n</xs:schema>";

        var result = await RunAsync(directory, Including, "<a>1</a>", cancellationToken, ("part.xsd", Schema));

        await Assert.That(result.Schema).IsEqualTo(Verdict.Valid);
        await Assert.That(result.Instance).IsEqualTo(Verdict.Valid);
    }

    [Test]
    public async Task Run_ImportWithAnHttpLocation_ReadsNothingFromTheNetwork(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var directory = new TempDirectory();
        var port = FreeLoopbackPort();
        var prefix = $"http://127.0.0.1:{port}/";
        using var listener = new HttpListener();
        listener.Prefixes.Add(prefix);
        listener.Start();
        var requests = 0;
        // Called directly (not via Task.Run), so the first GetContextAsync is registered before the engine runs:
        // a pool thread that starts late can no longer reach a listener that was already stopped.
        var serving = ServeAsync();
        async Task ServeAsync()
        {
            try
            {
                while (true)
                {
                    var context = await listener.GetContextAsync();
                    _ = Interlocked.Increment(ref requests);
                    context.Response.StatusCode = 404;
                    context.Response.Close();
                }
            }
            catch (Exception ex)
                when (ex is HttpListenerException or ObjectDisposedException or InvalidOperationException)
            {
                // The listener was stopped.
            }
        }
        var importing =
            $"<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">\n  <xs:import namespace=\"http://www.w3.org/XML/1998/namespace\" schemaLocation=\"{prefix}xml.xsd\"/>\n  <xs:element name=\"a\" type=\"xs:int\"/>\n</xs:schema>";

        var result = await RunAsync(directory, importing, "<a>1</a>", cancellationToken);
        listener.Stop();
        await serving;

        await Assert.That(result.Schema).IsEqualTo(Verdict.Valid);
        await Assert.That(result.Instance).IsEqualTo(Verdict.Valid);
        await Assert.That(requests).IsEqualTo(0);
    }

    private static int FreeLoopbackPort()
    {
#if NET9_0_OR_GREATER
        using
#endif
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        return ((IPEndPoint)probe.LocalEndpoint).Port;
    }
}
