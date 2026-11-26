namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.SchemaLocation;

using System.Linq;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xsd.Tests.Unit.Composition;

public class HintLimitTests
{
    private const string Hint = "xsi:schemaLocation=\"urn:h h.xsd\"";

    private const string Including =
        "<xs:schema xmlns:xs=\""
        + HintFixture.Xs
        + "\" targetNamespace=\"urn:h\" elementFormDefault=\"qualified\"><xs:include schemaLocation=\"inc.xsd\"/>"
        + "<xs:element name=\"root\"/></xs:schema>";

    private const string Included =
        "<xs:schema xmlns:xs=\""
        + HintFixture.Xs
        + "\" targetNamespace=\"urn:h\"><xs:element name=\"extra\"/></xs:schema>";

    [Test]
    public async Task TheDocumentCountLimitCoversTheHintsOfARun()
    {
        var resolver = HintFixture.Resolver().Add("file:///p.xsd", "<xs:schema xmlns:xs=\"" + HintFixture.Xs + "\"/>");
        var validator = HintFixture.Build(resolver, limits: ValidationLimits.Default.WithMaxExternalResources(1));
        var instance = HintFixture.BaseRoot("xsi:schemaLocation=\"urn:h h.xsd urn:p p.xsd\"", string.Empty);

        var result = HintFixture.Run(validator, instance);

        await Assert.That(resolver.Requests.Count).IsEqualTo(1);
        var issue = await Assert.That(HintFixture.Errors(result)).HasSingleItem();
        await Assert.That(issue!.Code).IsEqualTo(CoreDiagnosticCodes.ExternalResourceLimitExceeded);
    }

    [Test]
    public async Task TheDocumentsOfTheHintedSchemaCountAgainstTheSameLimit()
    {
        var resolver = new MapResolver().Add(HintFixture.HintedId, Including).Add("file:///inc.xsd", Included);
        var validator = HintFixture.Build(resolver, limits: ValidationLimits.Default.WithMaxExternalResources(1));

        var result = HintFixture.Run(validator, HintFixture.HintedRoot(Hint, string.Empty));

        await Assert.That(resolver.Requests.Count).IsEqualTo(1);
        await Assert
            .That(HintFixture.Errors(result).Any(i => i.Code == CoreDiagnosticCodes.ExternalResourceLimitExceeded))
            .IsTrue();
    }

    [Test]
    public async Task WithRoomInTheLimit_TheIncludedDocumentOfTheHintedSchemaIsLoaded()
    {
        var resolver = new MapResolver().Add(HintFixture.HintedId, Including).Add("file:///inc.xsd", Included);
        var validator = HintFixture.Build(resolver, limits: ValidationLimits.Default.WithMaxExternalResources(2));

        var result = HintFixture.Run(validator, HintFixture.HintedRoot(Hint, string.Empty));

        await Assert.That(resolver.Requests.Count).IsEqualTo(2);
        await Assert.That(result.IsValid).IsTrue();
    }

    private const string SmallBaseSchema =
        "<xs:schema xmlns:xs=\""
        + HintFixture.Xs
        + "\" targetNamespace=\""
        + HintFixture.Base
        + "\"><xs:element name=\"r\"/></xs:schema>";

    [Test]
    public async Task ADocumentLargerThanTheSizeLimit_IsReportedWithTheSizeCode()
    {
        // The configured schema is a resource too, so it must fit the limit that the larger hinted document exceeds.
        var validator = HintFixture.Build(
            SmallBaseSchema,
            HintFixture.Resolver(),
            limits: ValidationLimits.Default.WithMaxResourceSize(200)
        );

        var result = HintFixture.Run(validator, HintFixture.BaseRoot(Hint, string.Empty));

        var issue = await Assert.That(HintFixture.Errors(result)).HasSingleItem();
        await Assert.That(issue!.Code).IsEqualTo(CoreDiagnosticCodes.ResourceSizeLimitExceeded);
    }
}
