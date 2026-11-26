namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.XRechnung;

public class KositScenarioReaderTests
{
    private const string Sample = """
        <scenarios xmlns="http://www.xoev.de/de/validator/framework/1/scenarios" frameworkVersion="1.0.0">
          <name>Sample</name>
          <scenario>
            <name>UBL Invoice</name>
            <namespace prefix="cbc">urn:cbc</namespace>
            <namespace prefix="invoice">urn:invoice</namespace>
            <match>exists(/invoice:Invoice)</match>
            <validateWithXmlSchema>
              <resource><name>XSD</name><location>resources/ubl/2.1/xsd/maindoc/UBL-Invoice-2.1.xsd</location></resource>
            </validateWithXmlSchema>
            <validateWithSchematron>
              <resource><name>CEN</name><location>resources/ubl/2.1/xsl/EN16931-UBL-validation.xsl</location></resource>
            </validateWithSchematron>
            <validateWithSchematron>
              <resource><name>XR</name><location>resources/xrechnung/3.0.2/xsl/XRechnung-UBL-validation.xsl</location></resource>
            </validateWithSchematron>
            <createReport>
              <resource><name>r</name><location>resources/xrechnung-report.xsl</location></resource>
              <customLevel level="warning">BR-CL-23</customLevel>
            </createReport>
          </scenario>
          <scenario>
            <name>CII</name>
            <namespace prefix="rsm">urn:rsm</namespace>
            <match>exists(/rsm:CrossIndustryInvoice)</match>
            <validateWithXmlSchema>
              <resource><name>XSD</name><location>resources/cii/16b/xsd/CrossIndustryInvoice_100pD16B.xsd</location></resource>
            </validateWithXmlSchema>
            <validateWithSchematron>
              <resource><name>CEN</name><location>resources/cii/16b/xsl/EN16931-CII-validation.xsl</location></resource>
            </validateWithSchematron>
          </scenario>
        </scenarios>
        """;

    [Test]
    public async Task Read_Scenarios_KeepsOrderMatchAndNamespaces()
    {
        var scenarios = KositScenarioReader.Read(Sample);

        await Assert.That(scenarios.Select(s => s.Name)).IsEquivalentTo(["UBL Invoice", "CII"]);
        await Assert.That(scenarios[0].Match).IsEqualTo("exists(/invoice:Invoice)");
        await Assert.That(scenarios[0].Namespaces["invoice"]).IsEqualTo("urn:invoice");
        await Assert.That(scenarios[0].XsdLocation).IsEqualTo("resources/ubl/2.1/xsd/maindoc/UBL-Invoice-2.1.xsd");
    }

    [Test]
    public async Task Read_RuleSets_AreNamedByLayerAndSyntaxInOrder()
    {
        var scenarios = KositScenarioReader.Read(Sample);

        await Assert
            .That(scenarios[0].RuleSets)
            .IsEquivalentTo([new KositRuleSet("cen", "ubl"), new KositRuleSet("xrechnung", "ubl")]);
        await Assert.That(scenarios[1].RuleSets).IsEquivalentTo([new KositRuleSet("cen", "cii")]);
    }

    [Test]
    public async Task Read_CustomLevels_AreKeyedByRuleId()
    {
        var scenarios = KositScenarioReader.Read(Sample);

        await Assert.That(scenarios[0].CustomLevels["BR-CL-23"]).IsEqualTo("warning");
        await Assert.That(scenarios[1].CustomLevels).IsEmpty();
    }

    [Test]
    public async Task Read_UnknownSchematronLocation_Throws() =>
        await Assert
            .That(() =>
                KositScenarioReader.Read(
                    Sample.Replace("EN16931-UBL-validation.xsl", "Other-validation.xsl", StringComparison.Ordinal)
                )
            )
            .Throws<InvalidDataException>();

    [Test]
    public async Task Read_Null_Throws() =>
        await Assert.That(() => KositScenarioReader.Read(null!)).Throws<ArgumentNullException>();

    [Test]
    public async Task Manifest_PinsTheConfigurationAssetByChecksum()
    {
        var manifest = XRechnungRun.LoadManifest(KositScenarioRun.ConfigurationKey);

        await Assert.That(manifest.SourceUrl.Scheme).IsEqualTo(Uri.UriSchemeHttps);
        await Assert.That(manifest.SourceUrl.AbsolutePath).Contains("/releases/download/");
        await Assert.That(manifest.Sha256.Length).IsEqualTo(64);
    }
}
