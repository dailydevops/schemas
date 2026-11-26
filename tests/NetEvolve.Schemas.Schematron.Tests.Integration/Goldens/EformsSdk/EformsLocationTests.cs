namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.EformsSdk;

public class EformsLocationTests
{
    private static readonly Dictionary<string, string> Prefixes = new(StringComparer.Ordinal)
    {
        ["urn:cn"] = "cn",
        ["urn:cbc"] = "cbc",
    };

    [Test]
    [Arguments(
        "/*[local-name()='ContractNotice' and namespace-uri()='urn:cn'][1]/*[local-name()='ID' and namespace-uri()='urn:cbc'][1]",
        "/ContractNotice/cbc:ID"
    )]
    [Arguments(
        "/*[local-name()='ContractNotice' and namespace-uri()='urn:cn'][1]/*[local-name()='ID' and namespace-uri()='urn:cbc'][2]",
        "/ContractNotice/cbc:ID[2]"
    )]
    [Arguments(
        "/*[local-name()='ContractNotice' and namespace-uri()='urn:cn'][1]/*[local-name()='ID' and namespace-uri()='urn:other'][1]",
        "/ContractNotice/ID"
    )]
    [Arguments("/ContractNotice/cbc:ID[1]/cbc:Name[1]", "/ContractNotice/cbc:ID/cbc:Name")]
    [Arguments("/ContractNotice/cbc:ID[12]", "/ContractNotice/cbc:ID[12]")]
    [Arguments("", "")]
    public async Task Normalize_ReturnsTheFormOfTheReference(string location, string expected) =>
        await Assert.That(EformsLocation.Normalize(location, Prefixes)).IsEqualTo(expected);

    [Test]
    public async Task Normalize_PathEndingInAnAttribute_IsEmpty() =>
        await Assert
            .That(
                EformsLocation.Normalize(
                    "/*[local-name()='ContractNotice' and namespace-uri()='urn:cn'][1]/*[local-name()='ID' and namespace-uri()='urn:cbc'][1]/@*[local-name()='listName' and namespace-uri()='']",
                    Prefixes
                )
            )
            .IsEmpty();

    [Test]
    public async Task Normalize_NullArguments_Throw()
    {
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Task.FromResult(EformsLocation.Normalize(null!, Prefixes))
        );
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Task.FromResult(EformsLocation.Normalize("/a", null!))
        );
    }
}
