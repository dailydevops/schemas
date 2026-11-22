namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Entry;

using System.Threading.Tasks;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Subset;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Entry.DtdEntryFixture;

/// <summary>The default builder is deny by default (design #122, D6 and D7): an external subset is refused with a coded issue.</summary>
public class DtdValidatorSecurityTests
{
    [Test]
    public async Task DefaultBuilder_RefusesAnExternalSubset_WithACodedIssue()
    {
        var result = Run(ExternalSubsetDocument);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Issues.Count).IsEqualTo(1);
        await Assert.That(result.Issues[0].Code).IsEqualTo(XmlErrorCodes.XML2002.Code);
        await Assert.That(result.Issues[0].Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(result.Issues[0].Location.Source).IsEqualTo(SystemId);
        await Assert.That(result.Issues[0].Location.Line).IsEqualTo(1);
        await Assert.That(result.Issues[0].Constraint).IsNotNull();
    }

    [Test]
    public async Task DefaultBuilder_UsesTheDenyAllResolver_AndAllowsNothingExternal()
    {
        var builder = SchemaValidator.ForDtd();

        await Assert.That(builder.Resolver).IsSameReferenceAs(DenyAllResourceResolver.Instance);
        await Assert.That(builder.Options.Resolver).IsSameReferenceAs(DenyAllResourceResolver.Instance);
        await Assert.That(builder.Options.AllowExternalSubset).IsFalse();
        await Assert.That(builder.Options.AllowExternalGeneralEntities).IsFalse();
        await Assert.That(builder.Options.AllowExternalParameterEntities).IsFalse();
    }

    [Test]
    public async Task AResolverWithoutTheFlag_DoesNotEnableTheExternalSubset()
    {
        var resolver = SubsetHarness.Serving("<!ELEMENT r EMPTY>");

        var result = Run(ExternalSubsetDocument, b => b.WithResolver(resolver), SubsetHarness.DocumentUri);

        await Assert.That(result.Issues[0].Code).IsEqualTo(XmlErrorCodes.XML2002.Code);
        await Assert.That(resolver.Requests).IsEmpty();
    }
}
