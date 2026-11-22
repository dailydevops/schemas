namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.SchemaLocation;

using System.Threading.Tasks;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xsd.Tests.Unit.Composition;

public class NoNamespaceHintTests
{
    private const string Hint = "xsi:noNamespaceSchemaLocation=\"n.xsd\"";

    private static MapResolver Resolver() => new MapResolver().Add("file:///n.xsd", HintFixture.NoNamespaceSchema);

    private static string Instance(string content) =>
        "<plain xmlns:xsi=\"" + HintFixture.Xsi + "\" " + Hint + ">" + content + "</plain>";

    [Test]
    public async Task TheRequestHasNoNamespaceAndTheHintKind()
    {
        var resolver = Resolver();
        var validator = HintFixture.Build(resolver);

        _ = HintFixture.Run(validator, Instance("<a>1</a>"));

        var call = await Assert.That(resolver.Calls).HasSingleItem();
        await Assert.That(call!.Kind).IsEqualTo(ResourceKind.SchemaLocationHint);
        await Assert.That(call.NamespaceUri).IsNullOrEmpty();
        await Assert.That(call.ResolvedUri).IsEqualTo("file:///n.xsd");
    }

    [Test]
    public async Task TheLoadedSchemaAssessesTheElementsWithoutNamespace()
    {
        var validator = HintFixture.Build(Resolver());

        var valid = HintFixture.Run(validator, Instance("<a>1</a>"));
        var invalid = HintFixture.Run(validator, Instance("<b/>"));

        await Assert.That(valid.IsValid).IsTrue();
        var issue = await Assert.That(HintFixture.Errors(invalid)).HasSingleItem();
        await Assert.That(issue!.Constraint).IsEqualTo("cvc-complex-type.2.4");
    }

    [Test]
    public async Task ADocumentWithATargetNamespace_IsAnErrorForTheNoNamespaceHint()
    {
        var resolver = new MapResolver().Add("file:///n.xsd", HintFixture.HintedSchema);
        var validator = HintFixture.Build(resolver);

        var result = HintFixture.Run(validator, Instance("<a>1</a>"));

        await Assert.That(HintFixture.Errors(result)).IsNotEmpty();
        await Assert.That(HintFixture.Errors(result)[0].Constraint).IsEqualTo("schema_reference");
    }

    [Test]
    public async Task MoreThanOneLocation_IsALocatedIssue()
    {
        var resolver = Resolver();
        var validator = HintFixture.Build(resolver);
        var instance = "<plain xmlns:xsi=\"" + HintFixture.Xsi + "\" xsi:noNamespaceSchemaLocation=\"n.xsd m.xsd\"/>";

        var result = HintFixture.Run(validator, instance);

        await Assert.That(HintFixture.Errors(result)).IsNotEmpty();
        await Assert.That(HintFixture.Errors(result)[0].Constraint).IsEqualTo("schema_reference");
        await Assert.That(resolver.Requests).IsEmpty();
    }
}
