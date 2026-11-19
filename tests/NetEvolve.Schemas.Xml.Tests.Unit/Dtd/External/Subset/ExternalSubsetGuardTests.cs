namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Subset;

using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Declarations;
using NetEvolve.Schemas.Xml.Dtd.External.Subset;
using NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Loader;

/// <summary>The internal boundaries of the external subset reading reject null arguments, negative counts, and kinds of other resources.</summary>
public class ExternalSubsetGuardTests
{
    private static ExternalSubsetReader Reader() =>
        new(LoaderHarness.Open(SubsetHarness.Serving("<!ELEMENT a EMPTY>")), XmlReaderLimits.Default);

    [Test]
    public async Task Reader_NullArguments_Throw()
    {
        await Assert
            .That(() => new ExternalSubsetReader(null!, XmlReaderLimits.Default))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new ExternalSubsetReader(DtdValidationOptions.Default, null!))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => Reader().TryOpen(ResourceKind.ExternalSubset, null!, null, "a.dtd", null, out _, out _))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => Reader().TryOpen(ResourceKind.ExternalSubset, "a", null, null!, null, out _, out _))
            .Throws<ArgumentNullException>();
    }

    [Test]
    [Arguments(ResourceKind.SchemaDocument)]
    [Arguments(ResourceKind.XPathText)]
    public async Task Reader_TryOpen_KindOfAnotherResource_Throws(ResourceKind kind) =>
        await Assert
            .That(() => Reader().TryOpen(kind, "a", null, "a.dtd", SubsetHarness.DocumentUri, out _, out _))
            .Throws<ArgumentOutOfRangeException>();

    [Test]
    [Arguments(-1)]
    [Arguments(int.MinValue)]
    public async Task Reader_TryChargeExpansion_NegativeCount_Throws(int count) =>
        await Assert.That(() => Reader().TryChargeExpansion(count)).Throws<ArgumentOutOfRangeException>();

    [Test]
    public async Task Text_NullFrame_Throws() =>
        await Assert.That(() => new ExternalSubsetText(null!)).Throws<ArgumentNullException>();

    [Test]
    public async Task Handler_NullArguments_Throw()
    {
        await Assert.That(() => new DtdDoctypeHandler(null!)).Throws<ArgumentNullException>();
        await Assert
            .That(() => new DtdDoctypeHandler(null!, DtdValidationOptions.Default, SubsetHarness.DocumentUri))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Parser_NullLimits_Throw() =>
        await Assert
            .That(() =>
            {
                var info = new XmlDoctypeInfo("doc".AsSpan(), default, default, default);
                return DtdSubsetParser.Parse(in info, null!, null, null, out _);
            })
            .Throws<ArgumentNullException>();
}
