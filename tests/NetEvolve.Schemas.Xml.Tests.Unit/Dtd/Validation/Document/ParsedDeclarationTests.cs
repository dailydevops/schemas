namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Document;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Declarations;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Loader;
using NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Subset;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Document.DocumentHarness;

/// <summary>What the declaration parser hands the document step: where each element and attribute declaration stands, and mixed content as written.</summary>
public class ParsedDeclarationTests
{
    private static DtdDeclarations ParseInternal(string internalSubset)
    {
        var info = new XmlDoctypeInfo("doc".AsSpan(), default, default, internalSubset.AsSpan());
        var error = DtdSubsetParser.Parse(in info, XmlReaderLimits.Default, out var declarations);

        return error is null ? declarations! : throw new InvalidOperationException(error.Code.ToString());
    }

    [Test]
    public async Task InternalSubset_ElementsAndAttributes_AreNotExternal()
    {
        var model = ParseInternal("<!ELEMENT doc (a)><!ATTLIST doc x CDATA 'd'>");

        await Assert.That(model.Elements["doc"].InExternalText).IsFalse();
        await Assert.That(model.AttributeLists["doc"][0].InExternalText).IsFalse();
    }

    [Test]
    public async Task ExternalSubsetAndExternalParameterEntity_MarkElementsAndAttributesAsExternal()
    {
        var resolver = new RecordingResolver()
            .Add(
                SubsetHarness.SubsetUri,
                LoaderHarness.Utf8("<!ELEMENT fromSubset (a)><!ATTLIST fromSubset x CDATA 'd'> %ext;")
            )
            .Add(
                "http://example.org/doc/pe.ent",
                LoaderHarness.Utf8("<!ELEMENT fromEntity ANY><!ATTLIST fromEntity y ID #IMPLIED>")
            );

        var (model, error) = SubsetHarness.ParseModel(
            "<!ELEMENT fromDocument (a)><!ATTLIST fromDocument z CDATA 'd'><!ENTITY % ext SYSTEM \"pe.ent\">",
            resolver
        );

        await Assert.That(error).IsNull();
        await Assert.That(model!.Elements["fromDocument"].InExternalText).IsFalse();
        await Assert.That(model.AttributeLists["fromDocument"][0].InExternalText).IsFalse();
        await Assert.That(model.Elements["fromSubset"].InExternalText).IsTrue();
        await Assert.That(model.AttributeLists["fromSubset"][0].InExternalText).IsTrue();
        await Assert.That(model.Elements["fromEntity"].InExternalText).IsTrue();
        await Assert.That(model.AttributeLists["fromEntity"][0].InExternalText).IsTrue();
    }

    [Test]
    public async Task MixedContentWithARepeatedName_KeepsEveryNameForTheStep()
    {
        var model = ParseInternal("<!ELEMENT doc (#PCDATA|a|a)*><!ELEMENT a EMPTY>");
        var session = new Session(model);

        await Assert.That(session.Result.Count).IsEqualTo(1);
        await Assert.That(session.Result[0].Code).IsEqualTo(DtdValidityCodes.XML5008.Code);
    }

    [Test]
    public async Task MixedContentWithDistinctNames_ReportsNothing()
    {
        var model = ParseInternal("<!ELEMENT doc (#PCDATA|a|b)*><!ELEMENT a EMPTY><!ELEMENT b EMPTY>");

        await Assert.That(new Session(model).Result.Count).IsEqualTo(0);
    }
}
