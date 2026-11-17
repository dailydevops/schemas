namespace NetEvolve.Schemas.Conformance.Tests.Unit;

using NetEvolve.Schemas.Conformance.Manifest;

public class ConformanceLegTests
{
    [Test]
    public async Task Parse_OnEveryTestLeg_ReadsManifest()
    {
        var manifest = SuiteManifestLoader.Parse(
            $$"""{ "key": "leg", "url": "https://example.invalid/leg.zip", "sha256": "{{new string('A', 64)}}", "archive": "zip" }""",
            "leg.json"
        );

        await Assert.That(manifest.Key).IsEqualTo("leg");
        await Assert.That(manifest.Sha256).IsEqualTo(new string('a', 64));
    }
}
