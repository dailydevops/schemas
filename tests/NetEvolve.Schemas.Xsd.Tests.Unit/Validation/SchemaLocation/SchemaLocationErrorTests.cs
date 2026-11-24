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
    public async Task Hints_Overlay_DuplicateDefinitionOfBaseComponent_IsReported()
    {
        // The hinted schema of urn:g imports a document that declares the element r of the configured schema again.
        var resolver = new MapResolver()
            .Add(
                "file:///g.xsd",
                "<xs:schema xmlns:xs=\""
                    + HintFixture.Xs
                    + "\" targetNamespace=\"urn:g\"><xs:import namespace=\"urn:t\" schemaLocation=\"t.xsd\"/></xs:schema>"
            )
            .Add(
                "file:///t.xsd",
                "<xs:schema xmlns:xs=\""
                    + HintFixture.Xs
                    + "\" targetNamespace=\"urn:t\"><xs:element name=\"r\" type=\"xs:int\"/></xs:schema>"
            );
        var validator = HintFixture.Build(resolver);
        var instance = HintFixture.BaseRoot("xsi:schemaLocation=\"urn:g g.xsd\"", string.Empty);

        var result = HintFixture.Run(validator, instance);

        var issue = await Assert.That(HintFixture.Errors(result)).HasSingleItem();
        await Assert.That(issue!.Constraint).IsEqualTo("sch-props-correct.2");
        await Assert.That(issue.Message).Contains("'{urn:t}r'");
        await Assert.That(resolver.Requests.Count).IsEqualTo(2);
    }

    [Test]
    public async Task AHintForANamespaceThatTheSchemaAlreadyDefines_IsIgnoredWithoutARequest()
    {
        var resolver = HintFixture.Resolver();
        var validator = HintFixture.Build(resolver);
        var instance = "<r xmlns=\"urn:t\" xmlns:xsi=\"" + HintFixture.Xsi + "\" xsi:schemaLocation=\"urn:t s.xsd\"/>";

        var result = HintFixture.Run(validator, instance);

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(resolver.Requests).IsEmpty();
    }

    [Test]
    public async Task AnInstanceThatRepeatsTheConfiguredSchemaAndNamesAnotherOne_LoadsOnlyTheOther()
    {
        var resolver = HintFixture.Resolver();
        var validator = HintFixture.Build(resolver);
        var instance = HintFixture.BaseRoot(
            "xsi:schemaLocation=\"urn:t s.xsd urn:h h.xsd\"",
            "<h:root><h:a>1</h:a></h:root>"
        );

        var result = HintFixture.Run(validator, instance);

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(resolver.Requests).HasSingleItem();
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

    [Test]
    public async Task AWhitespaceOnlySchemaLocation_HasNoPairsAndNothingToReport()
    {
        var resolver = HintFixture.Resolver();
        var validator = HintFixture.Build(resolver);

        var result = HintFixture.Run(validator, HintFixture.BaseRoot("xsi:schemaLocation=\" \t\r\n \"", string.Empty));

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(resolver.Requests).IsEmpty();
    }

    [Test]
    public async Task AnEmptyNoNamespaceSchemaLocation_IsALocatedIssue()
    {
        var resolver = HintFixture.Resolver();
        var validator = HintFixture.Build(resolver);

        var result = HintFixture.Run(
            validator,
            HintFixture.BaseRoot("xsi:noNamespaceSchemaLocation=\"\"", string.Empty)
        );

        var issue = await Assert.That(HintFixture.Errors(result)).HasSingleItem();
        await Assert.That(issue!.Constraint).IsEqualTo("schema_reference");
        await Assert.That(resolver.Requests).IsEmpty();
    }

    [Test]
    public async Task EveryPairOfOneAttributeIsLoaded_ThePairsExtendTheModelOneAfterTheOther()
    {
        var resolver = HintFixture
            .Resolver()
            .Add(
                "file:///g.xsd",
                "<xs:schema xmlns:xs=\""
                    + HintFixture.Xs
                    + "\" targetNamespace=\"urn:g\" elementFormDefault=\"qualified\">"
                    + "<xs:element name=\"leaf\" type=\"xs:int\"/></xs:schema>"
            );
        var validator = HintFixture.Build(resolver);
        var instance = HintFixture.BaseRoot(
            "xsi:schemaLocation=\"urn:h h.xsd urn:g g.xsd\" xmlns:g=\"urn:g\"",
            "<h:root><h:a>1</h:a></h:root><g:leaf>x</g:leaf>"
        );

        var result = HintFixture.Run(validator, instance);

        var issue = await Assert.That(HintFixture.Errors(result)).HasSingleItem();
        await Assert.That(issue!.Constraint).Contains("cvc-datatype");
        await Assert.That(resolver.Requests.Count).IsEqualTo(2);
    }

    [Test]
    public async Task AStrictWildcardFindsTheElementsOfAHintOnTheElementAbove()
    {
        var schema =
            "<xs:schema xmlns:xs=\""
            + HintFixture.Xs
            + "\" targetNamespace=\"urn:t\" elementFormDefault=\"qualified\"><xs:element name=\"r\"><xs:complexType>"
            + "<xs:sequence><xs:any namespace=\"##other\" processContents=\"strict\"/></xs:sequence></xs:complexType></xs:element></xs:schema>";
        var validator = HintFixture.Build(schema, HintFixture.Resolver());

        var valid = HintFixture.Run(validator, HintFixture.BaseRoot(Hint, "<h:root><h:a>1</h:a></h:root>"));
        var undeclared = HintFixture.Run(
            validator,
            HintFixture.BaseRoot(string.Empty, "<h:root><h:a>1</h:a></h:root>")
        );

        await Assert.That(valid.IsValid).IsTrue();
        await Assert.That(undeclared.IsValid).IsFalse();
    }

    [Test]
    [Arguments("ht!tp://x/h.xsd")]
    [Arguments("1http://x/h.xsd")]
    public async Task ALocationThatIsNoUriReference_IsReportedWithTheReferenceCodeAndNoRequest(string location)
    {
        var resolver = new MapResolver();
        var validator = HintFixture.Build(resolver);

        var result = HintFixture.Run(
            validator,
            HintFixture.BaseRoot("xsi:schemaLocation=\"urn:h " + location + "\"", "<h:root/>")
        );

        var issue = await Assert.That(HintFixture.Errors(result)).HasSingleItem();
        await Assert.That(issue!.Code).IsEqualTo(CoreDiagnosticCodes.ResourceReferenceInvalid);
        await Assert.That(resolver.Requests.Count).IsEqualTo(0);
    }
}
