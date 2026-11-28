namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.SchemaLocation;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xsd.Tests.Unit.Composition;

public class SchemaLocationHintTests
{
    private const string Hint = "xsi:schemaLocation=\"urn:h h.xsd\"";

    [Test]
    public async Task WithSchemaLocationHints_ReturnsTheSameBuilder()
    {
        var builder = SchemaValidator.ForXsd();

        await Assert.That(builder.WithSchemaLocationHints(true)).IsSameReferenceAs(builder);
    }

    [Test]
    public async Task Builder_Hints_DefaultOff()
    {
        var resolver = HintFixture.Resolver();
        var validator = HintFixture.BuildDefault(resolver);

        // h:b would be invalid under the hinted schema; without the hint no declaration for h:root is known.
        var result = HintFixture.Run(validator, HintFixture.BaseRoot(Hint, "<h:root><h:b/></h:root>"));

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(resolver.Requests).IsEmpty();
    }

    [Test]
    public async Task ExplicitlyOff_HintsAreIgnored_ResolverReceivesNoCall()
    {
        var resolver = HintFixture.Resolver();
        var validator = HintFixture.Build(resolver, hints: false);

        var result = HintFixture.Run(validator, HintFixture.BaseRoot(Hint, "<h:root><h:b/></h:root>"));

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(resolver.Requests).IsEmpty();
    }

    [Test]
    public async Task Default_HintsAreIgnored_AlsoWithTheDenyAllResolver()
    {
        var validator = HintFixture.BuildDefault(resolver: null);

        var result = HintFixture.Run(validator, HintFixture.BaseRoot(Hint, "<h:root><h:b/></h:root>"));

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task OptIn_DenyAllResolver_TheHintYieldsACodedIssueAtTheCarryingElement()
    {
        var validator = HintFixture.Build(resolver: null);

        var result = HintFixture.Run(validator, HintFixture.BaseRoot(Hint, "<h:root/>"));

        var issue = await Assert.That(HintFixture.Errors(result)).HasSingleItem();
        await Assert.That(issue!.Code).IsEqualTo(CoreDiagnosticCodes.ResourceDenied);
        await Assert.That(issue.Constraint).IsEqualTo("schema_reference");
        await Assert.That(issue.Location.Source).IsEqualTo(HintFixture.InstanceId);
        await Assert.That(issue.Location.Line).IsEqualTo(1);
        await Assert.That(issue.Location.Column).IsGreaterThan(0);
    }

    [Test]
    public async Task OptIn_ResolverMissesTheDocument_TheHintYieldsTheNotFoundCode()
    {
        var validator = HintFixture.Build(new MapResolver());

        var result = HintFixture.Run(validator, HintFixture.BaseRoot(Hint, "<h:root/>"));

        var issue = await Assert.That(HintFixture.Errors(result)).HasSingleItem();
        await Assert.That(issue!.Code).IsEqualTo(CoreDiagnosticCodes.ResourceNotFound);
    }

    [Test]
    public async Task OptIn_TheRequestNamesTheHintKindTheNamespaceAndTheResolvedLocation()
    {
        var resolver = HintFixture.Resolver();
        var validator = HintFixture.Build(resolver);

        _ = HintFixture.Run(validator, HintFixture.BaseRoot(Hint, "<h:root><h:a>1</h:a></h:root>"));

        var call = await Assert.That(resolver.Calls).HasSingleItem();
        await Assert.That(call!.Kind).IsEqualTo(ResourceKind.SchemaLocationHint);
        await Assert.That(call.NamespaceUri).IsEqualTo(HintFixture.Hinted);
        await Assert.That(call.Reference).IsEqualTo("h.xsd");
        await Assert.That(call.ResolvedUri).IsEqualTo(HintFixture.HintedId);
        await Assert.That(call.BaseUri).IsEqualTo(HintFixture.InstanceId);
    }

    [Test]
    public async Task OptIn_ResolverReturnsTheSchema_ElementsOfTheNamespaceAreValidAgainstIt()
    {
        var validator = HintFixture.Build(HintFixture.Resolver());

        var result = HintFixture.Run(validator, HintFixture.BaseRoot(Hint, "<h:root><h:a>1</h:a></h:root>"));

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task OptIn_ResolverReturnsTheSchema_AWrongChildIsReportedAgainstTheLoadedContentModel()
    {
        var validator = HintFixture.Build(HintFixture.Resolver());

        var result = HintFixture.Run(validator, HintFixture.BaseRoot(Hint, "<h:root><h:b/></h:root>"));

        var issue = await Assert.That(HintFixture.Errors(result)).HasSingleItem();
        await Assert.That(issue!.Constraint).IsEqualTo("cvc-complex-type.2.4");
    }

    [Test]
    public async Task OptIn_ResolverReturnsTheSchema_AWrongValueIsReportedAgainstTheLoadedType()
    {
        var validator = HintFixture.Build(HintFixture.Resolver());

        var result = HintFixture.Run(validator, HintFixture.BaseRoot(Hint, "<h:root><h:a>x</h:a></h:root>"));

        await Assert.That(result.IsValid).IsFalse();
    }

    [Test]
    public async Task OptIn_TheRootIsInTheHintedNamespace_TheHintOnTheRootDeclaresIt()
    {
        var validator = HintFixture.Build(HintFixture.Resolver());

        var valid = HintFixture.Run(validator, HintFixture.HintedRoot(Hint, "<h:a>1</h:a>"));
        var invalid = HintFixture.Run(validator, HintFixture.HintedRoot(Hint, "<h:b/>"));

        await Assert.That(valid.IsValid).IsTrue();
        var issue = await Assert.That(HintFixture.Errors(invalid)).HasSingleItem();
        await Assert.That(issue!.Constraint).IsEqualTo("cvc-complex-type.2.4");
    }

    [Test]
    public async Task OptIn_TheHintIsOnAChildElement_ItIsLoadedWhereTheElementStartsAndAssessesIt()
    {
        var resolver = HintFixture.Resolver();
        var validator = HintFixture.Build(resolver);
        var instance = HintFixture.BaseRoot(string.Empty, "<h:root " + Hint + "><h:b/></h:root>");

        var result = HintFixture.Run(validator, instance);

        await Assert.That(resolver.Requests).HasSingleItem();
        var issue = await Assert.That(HintFixture.Errors(result)).HasSingleItem();
        await Assert.That(issue!.Constraint).IsEqualTo("cvc-complex-type.2.4");
    }

    [Test]
    public async Task OptIn_TheHintIsOnAnElementOfAStrictWildcard_ItIsAssessedAgainstTheNamedSchema()
    {
        var strict = HintFixture.BaseSchema.Replace(
            "processContents=\"lax\"",
            "processContents=\"strict\"",
            StringComparison.Ordinal
        );
        var resolver = HintFixture.Resolver();
        var validator = HintFixture.Build(strict, resolver);

        var valid = HintFixture.Run(
            validator,
            HintFixture.BaseRoot(string.Empty, "<h:root " + Hint + "><h:a>1</h:a></h:root>")
        );
        var invalid = HintFixture.Run(
            validator,
            HintFixture.BaseRoot(string.Empty, "<h:root " + Hint + "><h:b/></h:root>")
        );

        await Assert.That(valid.IsValid).IsTrue();
        var issue = await Assert.That(HintFixture.Errors(invalid)).HasSingleItem();
        await Assert.That(issue!.Constraint).IsEqualTo("cvc-complex-type.2.4");
    }

    [Test]
    public async Task OptIn_TheLoadedSchemaIsPerRun_TheValidatorStaysUnchanged()
    {
        var validator = HintFixture.Build(HintFixture.Resolver());
        _ = HintFixture.Run(validator, HintFixture.HintedRoot(Hint, "<h:a>1</h:a>"));

        var without = HintFixture.Run(validator, HintFixture.HintedRoot(string.Empty, "<h:a>1</h:a>"));

        var issue = await Assert.That(HintFixture.Errors(without)).HasSingleItem();
        await Assert.That(issue!.Constraint).IsEqualTo("cvc-assess-elt.1");
    }

    [Test]
    public async Task OptIn_ASecondHintForTheSameNamespaceBeforeItsUse_IsIgnoredWithoutACall()
    {
        var resolver = HintFixture.Resolver();
        var validator = HintFixture.Build(resolver);
        var instance = HintFixture.BaseRoot(Hint, "<x xmlns=\"urn:x\" " + Hint + "/><h:root><h:a>1</h:a></h:root>");

        var result = HintFixture.Run(validator, instance);

        await Assert.That(resolver.Requests).HasSingleItem();
        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task OptIn_AHintInASkippedSubtree_IsIgnored()
    {
        var resolver = HintFixture.Resolver();
        var schema =
            "<xs:schema xmlns:xs=\""
            + HintFixture.Xs
            + "\" targetNamespace=\"urn:t\" elementFormDefault=\"qualified\"><xs:element name=\"r\"><xs:complexType>"
            + "<xs:sequence><xs:any namespace=\"##other\" processContents=\"skip\"/></xs:sequence></xs:complexType></xs:element></xs:schema>";
        var validator = HintFixture.Build(schema, resolver);

        var result = HintFixture.Run(validator, HintFixture.BaseRoot(string.Empty, "<h:root " + Hint + "/>"));

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(resolver.Requests).IsEmpty();
    }
}
