namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.SchemaLocation;

using System.Threading.Tasks;
using NetEvolve.Schemas.Xsd.Tests.Unit.Composition;

public class SchemaLocationErrorTests
{
    private const string Hint = "xsi:schemaLocation=\"urn:h h.xsd\"";

    [Test]
    public async Task AHintForANamespaceThatIsAlreadyInUse_IsAnErrorAndNothingIsLoaded()
    {
        var resolver = HintFixture.Resolver();
        var validator = HintFixture.Build(resolver);
        var instance = HintFixture.BaseRoot(string.Empty, "<h:root xsi:schemaLocation=\"urn:t t2.xsd\"/>");

        var result = HintFixture.Run(validator, instance);

        var issue = await Assert.That(HintFixture.Errors(result)).HasSingleItem();
        await Assert.That(issue!.Constraint).IsEqualTo("schema_reference");
        await Assert.That(issue.Location.Line).IsEqualTo(1);
        await Assert.That(resolver.Requests).IsEmpty();
    }

    [Test]
    public async Task AHintForTheNamespaceOfItsOwnElement_IsNotALateHint()
    {
        var validator = HintFixture.Build(HintFixture.Resolver());

        var result = HintFixture.Run(validator, HintFixture.HintedRoot(Hint, "<h:a>1</h:a>"));

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task AHintForANamespaceAfterAnElementOfThatNamespace_IsAnError()
    {
        var resolver = HintFixture.Resolver();
        var validator = HintFixture.Build(resolver);
        var instance = HintFixture.BaseRoot(string.Empty, "<h:other/><h:root " + Hint + "/>");

        var result = HintFixture.Run(validator, instance);

        var issue = await Assert.That(HintFixture.Errors(result)).HasSingleItem();
        await Assert.That(issue!.Constraint).IsEqualTo("schema_reference");
        await Assert.That(resolver.Requests).IsEmpty();
    }

    [Test]
    public async Task ALoadedDocumentWithAnotherTargetNamespace_IsAnError()
    {
        var resolver = new MapResolver().Add(HintFixture.HintedId, HintFixture.BaseSchema);
        var validator = HintFixture.Build(resolver);

        var result = HintFixture.Run(validator, HintFixture.BaseRoot(Hint, "<h:root><h:a>1</h:a></h:root>"));

        var issue = await Assert.That(HintFixture.Errors(result)).HasSingleItem();
        await Assert.That(issue!.Constraint).IsEqualTo("schema_reference");
        await Assert.That(issue.Message).Contains("urn:h");
        await Assert.That(issue.Message).Contains("urn:t");
    }

    [Test]
    public async Task AnOddNumberOfTokensInSchemaLocation_IsALocatedIssueAndNothingIsLoaded()
    {
        var resolver = HintFixture.Resolver();
        var validator = HintFixture.Build(resolver);
        var instance = HintFixture.BaseRoot("xsi:schemaLocation=\"urn:h h.xsd urn:x\"", string.Empty);

        var result = HintFixture.Run(validator, instance);

        var issue = await Assert.That(HintFixture.Errors(result)).HasSingleItem();
        await Assert.That(issue!.Constraint).IsEqualTo("schema_reference");
        await Assert.That(issue.Location.Source).IsEqualTo(HintFixture.InstanceId);
        await Assert.That(issue.Location.Line).IsEqualTo(1);
        await Assert.That(resolver.Requests).IsEmpty();
    }

    [Test]
    public async Task SchemaLocationWithAnyWhitespaceBetweenTokens_LoadsEveryPair()
    {
        var resolver = HintFixture.Resolver().Add("file:///p.xsd", "<xs:schema xmlns:xs=\"" + HintFixture.Xs + "\"/>");
        var validator = HintFixture.Build(resolver);
        var instance = HintFixture.BaseRoot("xsi:schemaLocation=\"  urn:h\th.xsd\r\n urn:p   p.xsd \"", string.Empty);

        _ = HintFixture.Run(validator, instance);

        await Assert.That(resolver.Requests.Count).IsEqualTo(2);
    }

    [Test]
    public async Task AHintForTheNamespaceOfTheConfiguredSchema_ReportsTheDuplicateComponentAndLoadsNothing()
    {
        var resolver = new MapResolver().Add(
            HintFixture.HintedId,
            "<xs:schema xmlns:xs=\""
                + HintFixture.Xs
                + "\" targetNamespace=\"urn:t\"><xs:element name=\"r\" type=\"xs:int\"/></xs:schema>"
        );
        var validator = HintFixture.Build(resolver);
        var instance = "<r xmlns=\"urn:t\" xmlns:xsi=\"" + HintFixture.Xsi + "\" xsi:schemaLocation=\"urn:t h.xsd\"/>";

        var result = HintFixture.Run(validator, instance);

        var issue = await Assert.That(HintFixture.Errors(result)).HasSingleItem();
        await Assert.That(issue!.Message).Contains("'{urn:t}r'");
    }

    [Test]
    public async Task ASchemaThatDoesNotCompile_ReportsItsIssuesAndTheElementsStayUndeclared()
    {
        var resolver = new MapResolver().Add(
            HintFixture.HintedId,
            "<xs:schema xmlns:xs=\"" + HintFixture.Xs + "\" targetNamespace=\"urn:h\"><xs:bad/></xs:schema>"
        );
        var validator = HintFixture.Build(resolver);

        var result = HintFixture.Run(validator, HintFixture.HintedRoot(Hint, "<h:a>1</h:a>"));

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(HintFixture.Errors(result).Length).IsGreaterThanOrEqualTo(2);
    }
}
