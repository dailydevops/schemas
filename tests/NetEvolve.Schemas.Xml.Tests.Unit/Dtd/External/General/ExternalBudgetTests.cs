namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.General;

using System.Threading;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Loader;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.General.GeneralHarness;

/// <summary>The budgets of a document are shared by the internal and the external entities (design #122, section 8, rule 4).</summary>
public class ExternalBudgetTests
{
    private const string InternalI = "<!ENTITY i \"123456\">";

    [Test]
    [Arguments("&i;&e;")]
    [Arguments("&e;&i;")]
    public async Task Characters_OfInternalAndExternalEntities_AreChargedTogether_Xml1109(
        string content,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Six characters each fit the budget of ten, twelve do not.
        var limits = XmlReaderLimits.Default.WithMaxEntityExpansionCharacters(10);

        var both = Read(Doc(InternalI + ExternalE, content), Serving("abcdef"), limits);
        var internalOnly = Read(Doc(InternalI + ExternalE, "&i;"), Serving("abcdef"), limits);
        var externalOnly = Read(Doc(InternalI + ExternalE, "&e;"), Serving("abcdef"), limits);

        await Assert.That(internalOnly.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(externalOnly.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(both.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(both.Code).IsEqualTo("XML1109");
    }

    [Test]
    public async Task Characters_OfTwoExternalReferences_AreChargedTogether_Xml1109(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var limits = XmlReaderLimits.Default.WithMaxEntityExpansionCharacters(10);

        var run = Read(Doc(ExternalE, "&e;&e;"), Serving("abcdef"), limits);

        await Assert.That(run.Code).IsEqualTo("XML1109");
    }

    [Test]
    public async Task Characters_OfAnExternalEntity_AreChargedAsTheyAreRead_NotAfterTheyAreBuffered(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var limits = XmlReaderLimits.Default.WithMaxEntityExpansionCharacters(100);

        var run = Read(Doc(ExternalE, "&e;"), Serving(new string('x', 10_000)), limits);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code).IsEqualTo("XML1109");
    }

    [Test]
    public async Task Depth_OfInternalAndExternalFramesTogether_IsBoundedByMaxEntityDepth_Xml1110(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        // i (1) contains e (2) contains j (3): each kind alone nests at most two deep.
        var declarations = "<!ENTITY i \"&e;\">" + ExternalE + "<!ENTITY j \"z\">";
        var allowed = XmlReaderLimits.Default.WithMaxEntityDepth(3);
        var tight = XmlReaderLimits.Default.WithMaxEntityDepth(2);

        var fits = Read(Doc(declarations, "&i;"), Serving("&j;"), allowed);
        var exceeds = Read(Doc(declarations, "&i;"), Serving("&j;"), tight);

        await Assert.That(fits.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(fits.Events).IsEqualTo("<doc|T'z'|/doc");
        await Assert.That(exceeds.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(exceeds.Code).IsEqualTo("XML1110");
    }

    [Test]
    public async Task Depth_IsGivenBack_AfterAnExternalEntity_SoSiblingsDoNotAccumulate(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var limits = XmlReaderLimits.Default.WithMaxEntityDepth(1);

        var run = Read(Doc(ExternalE, "&e;&e;&e;"), Serving("x"), limits);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(run.Events).IsEqualTo("<doc|T'xxx'|/doc");
    }

    [Test]
    public async Task Cycle_ThroughAnInternalAndAnExternalEntity_IsXml0020_BeforeAnyBudgetError(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = Serving("&i;");
        var limits = XmlReaderLimits.Default.WithMaxEntityDepth(2);

        var run = Read(Doc("<!ENTITY i \"&e;\">" + ExternalE, "&i;"), resolver, limits);

        await Assert.That(run.Code).IsEqualTo("XML0020");
        await Assert.That(resolver.Requests.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Requests_AreBoundedByMaxExternalEntityRequests_Xml1117(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = Serving("x");
        var access = LoaderHarness
            .Open(resolver)
            .WithDtdLimits(DtdValidationLimits.Default.WithMaxExternalEntityRequests(2));

        var allowed = Read(Doc(ExternalE, "&e;&e;"), Options(access));
        var exceeds = Read(Doc(ExternalE, "&e;&e;&e;"), Options(access));

        await Assert.That(allowed.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(exceeds.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(exceeds.Code).IsEqualTo("XML1117");
    }

    [Test]
    public async Task Requests_AreSharedWithTheExternalSubset(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = Serving("x").Add("http://example.org/doc/ext.dtd", LoaderHarness.Utf8(string.Empty));
        var access = LoaderHarness
            .Open(resolver)
            .WithDtdLimits(DtdValidationLimits.Default.WithMaxExternalEntityRequests(1));

        var run = Read($"<!DOCTYPE doc SYSTEM \"ext.dtd\" [{ExternalE}]>\n<doc>&e;</doc>", Options(access));

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code).IsEqualTo("XML1117");
        await Assert.That(resolver.Requests.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Characters_AreSharedWithTheExternalSubset(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // The subset is 15 characters, the entity 6: each fits the budget of 20, together they do not.
        var resolver = Serving("abcdef").Add("http://example.org/doc/ext.dtd", LoaderHarness.Utf8("<!-- abcdef -->"));
        var limits = XmlReaderLimits.Default.WithMaxEntityExpansionCharacters(20);

        var run = Read($"<!DOCTYPE doc SYSTEM \"ext.dtd\" [{ExternalE}]>\n<doc>&e;</doc>", resolver, limits);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code).IsEqualTo("XML1109");
    }
}
