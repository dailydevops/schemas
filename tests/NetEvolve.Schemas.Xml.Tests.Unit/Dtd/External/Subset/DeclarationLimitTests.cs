namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Subset;

using System.Globalization;
using System.Text;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Declarations;
using NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Loader;

/// <summary>Design #122, section 9: the declaration parser stops at <c>MaxDeclarations</c> with <c>XML1119</c>, internal and external declarations together.</summary>
public class DeclarationLimitTests
{
    private static DtdValidationOptions Max(RecordingResolver resolver, int count) =>
        LoaderHarness.Open(resolver).WithDtdLimits(DtdValidationLimits.Default.WithMaxDeclarations(count));

    private static (string Model, XmlErrorCode? Error) Parse(string internalSubset, string externalSubset, int max)
    {
        var resolver = SubsetHarness.Serving(externalSubset);

        return SubsetHarness.Parse(internalSubset, resolver, options: Max(resolver, max));
    }

    [Test]
    [Arguments("<!ELEMENT a EMPTY><!ELEMENT b EMPTY><!ELEMENT c EMPTY>", 3, false)]
    [Arguments("<!ELEMENT a EMPTY><!ELEMENT b EMPTY><!ELEMENT c EMPTY><!ELEMENT d EMPTY>", 3, true)]
    [Arguments("<!ATTLIST e a CDATA #IMPLIED b CDATA #IMPLIED c CDATA #IMPLIED>", 3, false)]
    [Arguments("<!ATTLIST e a CDATA #IMPLIED b CDATA #IMPLIED c CDATA #IMPLIED d CDATA #IMPLIED>", 3, true)]
    [Arguments("<!ENTITY a 'x'><!ENTITY % b 'x'><!ENTITY c 'x'><!ENTITY d 'x'>", 3, true)]
    [Arguments(
        "<!NOTATION a SYSTEM 'x'><!NOTATION b SYSTEM 'x'><!NOTATION c SYSTEM 'x'><!NOTATION d SYSTEM 'x'>",
        3,
        true
    )]
    [Arguments("<!ELEMENT a EMPTY><!ATTLIST a x CDATA #IMPLIED><!ENTITY e 'x'><!NOTATION n SYSTEM 'x'>", 4, false)]
    [Arguments("<!ELEMENT a EMPTY><!ATTLIST a x CDATA #IMPLIED><!ENTITY e 'x'><!NOTATION n SYSTEM 'x'>", 3, true)]
    public async Task Declarations_BeyondTheLimit_RaiseXml1119(string subset, int max, bool exceeds)
    {
        ArgumentNullException.ThrowIfNull(subset);
        var (_, error) = Parse(subset, string.Empty, max);

        await Assert.That(error).IsEqualTo(exceeds ? DtdLimitCodes.XML1119 : null);
    }

    [Test]
    public async Task InternalAndExternalDeclarations_AreCountedTogether()
    {
        var (_, within) = Parse("<!ELEMENT a EMPTY><!ELEMENT b EMPTY>", "<!ELEMENT c EMPTY>", 3);
        var (_, beyond) = Parse("<!ELEMENT a EMPTY><!ELEMENT b EMPTY>", "<!ELEMENT c EMPTY><!ELEMENT d EMPTY>", 3);

        await Assert.That(within).IsNull();
        await Assert.That(beyond).IsEqualTo(DtdLimitCodes.XML1119);
    }

    [Test]
    public async Task DeclarationsOfAnExternalParameterEntity_AreCounted()
    {
        var resolver = new RecordingResolver().Add(
            "http://example.org/doc/pe.ent",
            LoaderHarness.Utf8("<!ELEMENT a EMPTY><!ELEMENT b EMPTY><!ELEMENT c EMPTY>")
        );

        var (_, error) = SubsetHarness.Parse(
            "<!ENTITY % ext SYSTEM \"pe.ent\"> %ext;",
            resolver,
            systemId: string.Empty,
            options: Max(resolver, 3)
        );

        await Assert.That(error).IsEqualTo(DtdLimitCodes.XML1119);
    }

    [Test]
    public async Task IgnoredSection_DeclaresNothing()
    {
        var (_, error) = Parse(
            string.Empty,
            "<![ IGNORE [ <!ELEMENT a EMPTY><!ELEMENT b EMPTY><!ELEMENT c EMPTY><!ELEMENT d EMPTY> ]]><!ELEMENT e EMPTY>",
            1
        );

        await Assert.That(error).IsNull();
    }

    [Test]
    public async Task WithoutAReader_TheDefaultLimitApplies()
    {
        var limit = DtdValidationLimits.Default.MaxDeclarations;
        var subset = new StringBuilder();
        for (var i = 0; i <= limit; i++)
        {
            _ = subset.Append("<!ENTITY a 'x'>");
        }

        var (_, atTheLimit) = DtdHarness.Parse(subset.ToString(0, 15 * limit));
        var (_, beyond) = DtdHarness.Parse(subset.ToString());

        await Assert.That(atTheLimit).IsNull();
        await Assert.That(beyond).IsEqualTo(DtdLimitCodes.XML1119);
        await Assert.That(limit.ToString(CultureInfo.InvariantCulture)).IsEqualTo("100000");
    }
}
