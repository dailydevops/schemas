namespace NetEvolve.Schemas.Xsd.Tests.Integration.Ubl;

using NetEvolve.Schemas.Conformance.Manifest;

public class UblManifestTests
{
    [Test]
    [Arguments("2.1")]
    [Arguments("2.2")]
    [Arguments("2.3")]
    [Arguments("2.4")]
    public async Task Manifest_PinsTheOasisArchive_OfTheVersion(string version)
    {
        var manifest = UblSuite.LoadManifest(version);

        await Assert
            .That(manifest.SourceUrl.AbsoluteUri)
            .IsEqualTo($"https://docs.oasis-open.org/ubl/os-UBL-{version}/UBL-{version}.zip");
        await Assert.That(manifest.ArchiveType).IsEqualTo(ArchiveType.Zip);
        await Assert.That(manifest.Sha256).Length().IsEqualTo(64);
    }

    [Test]
    public async Task Manifests_HaveOneUniqueChecksumPerVersion()
    {
        var checksums = UblSuite.Versions.Select(v => UblSuite.LoadManifest(v).Sha256).Distinct(StringComparer.Ordinal);

        await Assert.That(checksums.Count()).IsEqualTo(UblSuite.Versions.Length);
    }
}
