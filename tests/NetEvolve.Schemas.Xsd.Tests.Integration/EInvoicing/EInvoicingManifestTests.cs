namespace NetEvolve.Schemas.Xsd.Tests.Integration.EInvoicing;

/// <summary>The record is well formed; no archive is fetched.</summary>
public class EInvoicingManifestTests
{
    [Test]
    public async Task Parse_TheCommittedRecord_HasUniqueNamedSetsWithRootsAndKnownExpectations()
    {
        var manifest = EInvoicingRun.LoadManifest();

        await Assert.That(manifest.Sets).IsNotEmpty();
        await Assert
            .That(manifest.Sets.Select(s => s.Name).Distinct(StringComparer.Ordinal).Count())
            .IsEqualTo(manifest.Sets.Count);
        foreach (var set in manifest.Sets)
        {
            await Assert.That(set.Schemas).IsNotEmpty();
            foreach (var group in set.Documents)
            {
                await Assert
                    .That(new[] { EInvoicingRunner.Valid, EInvoicingRunner.Differential })
                    .Contains(group.Expectation);
            }
        }
    }

    [Test]
    public async Task Parse_TheCommittedRecord_NamesEveryKnownFailureAndExclusionOnce()
    {
        var manifest = EInvoicingRun.LoadManifest();

        await Assert
            .That(manifest.KnownFailures.Select(f => f.Id).Distinct(StringComparer.Ordinal).Count())
            .IsEqualTo(manifest.KnownFailures.Count);
        await Assert
            .That(manifest.Excluded.Select(e => e.Id).Distinct(StringComparer.Ordinal).Count())
            .IsEqualTo(manifest.Excluded.Count);
    }

    [Test]
    public async Task Parse_NullMalformedOrIncompleteJson_Throws()
    {
        await Assert.That(() => EInvoicingManifest.Parse(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => EInvoicingManifest.Parse("{")).Throws<InvalidDataException>();
        await Assert.That(() => EInvoicingManifest.Parse("{\"sets\":[]}")).Throws<InvalidDataException>();
    }
}
