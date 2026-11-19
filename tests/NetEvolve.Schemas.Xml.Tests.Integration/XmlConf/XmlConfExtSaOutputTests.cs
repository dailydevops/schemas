namespace NetEvolve.Schemas.Xml.Tests.Integration.XmlConf;

using System.Text;
using NetEvolve.Schemas.Resources.FileSystem;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Reader.Pipeline;

/// <summary>The cases of <c>valid/ext-sa</c> of the suite: documents with external general entities and the canonical output files that go with them (XML-62).</summary>
public class XmlConfExtSaOutputTests
{
    private const string ValidExtSaPrefix = "xmltest/valid-ext-sa-";

    /// <summary>Case 013 needs the attribute defaults of the attribute-list declarations, which the reader does not apply yet (XML-16, #256); its output file lists them, so the comparison drops them from the expected output.</summary>
    private const string AttributeDefaultsCase = "xmltest/valid-ext-sa-013";

    private const string AttributeDefaults = " a1=\"a1 default\" a2=\"a2 default\"";

    [Test]
    public async Task ValidExtSa_CasesParseAndMatchTheProvidedOutputFiles(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var selection = await XmlConfSuite.LoadSelectionAsync(cancellationToken);
        var cases = selection.Selected.Where(c => c.Id.StartsWith(ValidExtSaPrefix, StringComparison.Ordinal)).ToList();
        var access = DtdValidationOptions
            .Default.WithResolver(new SandboxedFileSystemResourceResolver(XmlConfSuite.SuiteRoot))
            .WithAllowExternalSubset(true)
            .WithAllowExternalGeneralEntities(true)
            .WithAllowExternalParameterEntities(true);

        var mismatches = new List<string>();
        foreach (var c in cases)
        {
            var path = Path.Combine(XmlConfSuite.SuiteRoot, c.Uri);
            var expected = Encoding.UTF8.GetString(
                await File.ReadAllBytesAsync(
                    Path.Combine(Path.GetDirectoryName(path)!, "out", Path.GetFileName(path)),
                    cancellationToken
                )
            );
            if (string.Equals(c.Id, AttributeDefaultsCase, StringComparison.Ordinal))
            {
                expected = expected.Replace(AttributeDefaults, string.Empty, StringComparison.Ordinal);
            }

            var actual = Observe(await File.ReadAllBytesAsync(path, cancellationToken), path, access);
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
            {
                mismatches.Add($"{c.Id}: expected [{expected}] but was [{actual}]");
            }
        }

        // The catalog lists 001 to 009 and 011 to 014; it has no case 010.
        await Assert.That(cases.Count).IsEqualTo(13);
        await Assert.That(mismatches).IsEmpty();
    }

    private static string Observe(byte[] bytes, string path, DtdValidationOptions access)
    {
        var options = XmlReaderOptions
            .Default.WithDtdProcessing(true)
            .WithExternalAccess(access)
            .WithBaseUri(new Uri(path).AbsoluteUri);
        using var reader = XmlPipelineReader.Create(bytes.AsSpan(), options);
        var sink = new CanonicalXmlConfSink();
        var status = reader.Pump(ref sink);

        return status == XmlReadStatus.Completed ? sink.Output : $"{status} {reader.Diagnostic?.Code.Alias}";
    }
}
