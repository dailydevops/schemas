namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.General;

using System.Threading;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Loader;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.General.GeneralHarness;

/// <summary>
/// What stays refused or forbidden (XML-62): external access is deny by default, an external entity reference in an
/// attribute value is a well-formedness error, and so is a reference to an unparsed entity.
/// </summary>
public class ExternalGateTests
{
    private const string UnparsedU = "<!ENTITY u SYSTEM \"u.png\" NDATA png>";

    [Test]
    public async Task Reference_WithoutExternalAccessOptions_IsXml2002(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var run = Read(Doc(ExternalE, "&e;"), Options(access: null));

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code).IsEqualTo("XML2002");
    }

    [Test]
    public async Task Reference_WithDefaultOptions_IsXml2002_AndTheResolverReceivesNoCall(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = Serving("data");

        var run = Read(Doc(ExternalE, "&e;"), Options(DtdValidationOptions.Default.WithResolver(resolver)));

        await Assert.That(run.Code).IsEqualTo("XML2002");
        await Assert.That(resolver.Requests).IsEmpty();
    }

    [Test]
    public async Task Reference_WithTheFlagOfGeneralEntitiesOff_IsXml2002_AndTheResolverReceivesNoCall(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = Serving("data");
        var access = LoaderHarness.Open(resolver).WithAllowExternalGeneralEntities(false);

        var run = Read(Doc(ExternalE, "&e;"), Options(access));

        await Assert.That(run.Code).IsEqualTo("XML2002");
        await Assert.That(resolver.Requests).IsEmpty();
    }

    [Test]
    public async Task Reference_WithTheFlagOnAndTheDefaultResolver_IsDeniedByTheResolver(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var access = DtdValidationOptions.Default.WithAllowExternalGeneralEntities(true);

        var run = Read(Doc(ExternalE, "&e;"), Options(access));

        await Assert.That(run.Code).IsEqualTo("XML2002");
    }

    [Test]
    public async Task Reference_ThatTheResolverCannotFind_IsXml2002(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = new RecordingResolver();

        var run = Read(Doc(ExternalE, "&e;"), resolver);

        await Assert.That(run.Code).IsEqualTo("XML2002");
        await Assert.That(resolver.Requests.Count).IsEqualTo(1);
    }

    [Test]
    [Arguments("<doc a=\"&e;\"/>")]
    [Arguments("<doc a='x&e;y'/>")]
    public async Task Reference_InAnAttributeValue_RaisesNoExternalEntityReferences_WithoutAResolverCall(
        string root,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = Serving("data");

        var run = Read($"<!DOCTYPE doc [{ExternalE}]>\n{root}", resolver);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code).IsEqualTo("XML0022");
        await Assert.That(resolver.Requests).IsEmpty();
    }

    [Test]
    public async Task Reference_InAnAttributeValueThroughAnInternalEntity_RaisesNoExternalEntityReferences(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = Serving("data");
        var declarations = "<!ENTITY i \"[&e;]\">" + ExternalE;

        var run = Read($"<!DOCTYPE doc [{declarations}]>\n<doc a=\"&i;\"/>", resolver);

        await Assert.That(run.Code).IsEqualTo("XML0022");
        await Assert.That(resolver.Requests).IsEmpty();
    }

    [Test]
    public async Task Reference_InAnAttributeValueWithoutExternalAccess_RaisesNoExternalEntityReferences(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var run = Read($"<!DOCTYPE doc [{ExternalE}]>\n<doc a=\"&e;\"/>", Options(access: null));

        await Assert.That(run.Code).IsEqualTo("XML0022");
    }

    [Test]
    public async Task Reference_ToAnUnparsedEntityInContent_RaisesParsedEntity_WithoutAResolverCall(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = Serving("data");

        var run = Read(Doc(UnparsedU, "&u;"), resolver);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code).IsEqualTo("XML0021");
        await Assert.That(resolver.Requests).IsEmpty();
    }

    [Test]
    public async Task Reference_ToAnUnparsedEntityInAnAttributeValue_RaisesParsedEntity(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var run = Read($"<!DOCTYPE doc [{UnparsedU}]>\n<doc a=\"&u;\"/>", Serving("data"));

        await Assert.That(run.Code).IsEqualTo("XML0021");
    }

    [Test]
    public async Task Reference_ToAnUnparsedEntityThroughAnExternalEntity_RaisesParsedEntity(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var run = Read(Doc(UnparsedU + ExternalE, "&e;"), Serving("<b>&u;</b>"));

        await Assert.That(run.Code).IsEqualTo("XML0021");
    }

    [Test]
    public async Task Reference_OutsideTheRootElement_IsStillRefused(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = Serving("data");

        var run = Read($"<!DOCTYPE doc [{ExternalE}]>\n&e;<doc/>", resolver);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(resolver.Requests).IsEmpty();
    }
}
