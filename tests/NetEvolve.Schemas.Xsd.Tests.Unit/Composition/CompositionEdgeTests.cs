namespace NetEvolve.Schemas.Xsd.Tests.Unit.Composition;

using System;
using System.Text;
using NetEvolve.Schemas.Resources;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Composition.CompositionFixture;

/// <summary>Edge cases of the loader: resolver answers, location forms, and the redefine content shapes.</summary>
public class CompositionEdgeTests
{
    private const string Include = "<xs:include schemaLocation=\"a.xsd\"/>";
    private const string SimpleT = "<xs:simpleType name=\"T\"><xs:restriction base=\"xs:string\"/></xs:simpleType>";

    private static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);

    [Test]
    public async Task InvalidReference_IsACodedError_WithoutAskingTheResolver()
    {
        var resolver = new ScriptedResolver(_ => ResourceResult.Denied());

        var (set, issues) = Load(Schema("<xs:include schemaLocation=\"a b%zz\"/>"), resolver);

        await Assert.That(set.Units.Count).IsEqualTo(1);
        await Assert.That(resolver.Calls).IsEqualTo(0);
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(CoreDiagnosticCodes.ResourceReferenceInvalid);
    }

    [Test]
    public async Task DeniedWithoutMessage_AndNotFoundWithoutMessage_GetADefaultMessage()
    {
        var denied = new ScriptedResolver(_ => ResourceResult.Denied());
        var missing = new ScriptedResolver(_ => ResourceResult.NotFound());

        var (_, first) = Load(Schema(Include), denied);
        var (_, second) = Load(Schema(Include), missing);

        await Assert.That(first[0].Code).IsEqualTo(CoreDiagnosticCodes.ResourceDenied);
        await Assert.That(first[0].Message.Contains("denied", StringComparison.Ordinal)).IsTrue();
        await Assert.That(second[0].Code).IsEqualTo(CoreDiagnosticCodes.ResourceNotFound);
        await Assert.That(second[0].Message.Contains("not found", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task ContentResult_IsParsed_AndRedirectIsTheBaseUriOfTheDocument()
    {
        var resolver = new ScriptedResolver(_ =>
            ResourceResult.Found(Bytes(Schema("<xs:element name=\"a\"/>")), "file:///moved/a.xsd#frag")
        );

        var (set, issues) = Load(Schema(Include), resolver);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(set.Units[1].Document.BaseUri).IsEqualTo("file:///moved/a.xsd");
    }

    [Test]
    public async Task Redirect_ToAnAlreadyLoadedDocument_IsLoadedOnce()
    {
        var resolver = new ScriptedResolver(_ => ResourceResult.Found(Bytes(Schema(string.Empty)), "file:///same.xsd"));

        var (set, issues) = Load(Schema(Include + "<xs:include schemaLocation=\"b.xsd\"/>"), resolver);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(set.Units.Count).IsEqualTo(2);
        await Assert.That(resolver.Calls).IsEqualTo(2);
    }

    [Test]
    public async Task ContentResult_AboveTheSizeLimit_IsReported()
    {
        var resolver = new ScriptedResolver(_ => ResourceResult.Found(Bytes(Schema(new string(' ', 2000)))));

        var (_, issues) = Load(Schema(Include), resolver, ValidationLimits.Default.WithMaxResourceSize(100));

        await Assert.That(issues[0].Code).IsEqualTo(CoreDiagnosticCodes.ResourceSizeLimitExceeded);
    }

    [Test]
    public async Task NonSeekableStream_AboveTheSizeLimit_IsReported_WhileReading()
    {
        var resolver = new ScriptedResolver(_ =>
            ResourceResult.Found(new NonSeekableStream(Bytes(Schema(new string(' ', 5000)))))
        );

        var (_, issues) = Load(Schema(Include), resolver, ValidationLimits.Default.WithMaxResourceSize(1024));

        await Assert.That(issues[0].Code).IsEqualTo(CoreDiagnosticCodes.ResourceSizeLimitExceeded);
    }

    [Test]
    public async Task NonSeekableStream_WithinTheLimit_IsParsed()
    {
        var resolver = new ScriptedResolver(_ =>
            ResourceResult.Found(new NonSeekableStream(Bytes(Schema(string.Empty))))
        );

        var (set, issues) = Load(Schema(Include), resolver);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(set.Units.Count).IsEqualTo(2);
    }

    [Test]
    public async Task UnreadableRoot_GivesNoUnits()
    {
        var (set, _) = Load("<xs:schema", new MapResolver());

        await Assert.That(set.Units.Count).IsEqualTo(0);
    }

    [Test]
    public async Task DirectivesWithoutLocation_LoadNothing()
    {
        var resolver = new MapResolver();

        var (set, _) = Load(Schema("<xs:include/><xs:redefine/>"), resolver);

        await Assert.That(set.Units.Count).IsEqualTo(1);
        await Assert.That(resolver.Requests.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Components_AreAllTopLevelDeclarationsAndDefinitions_InDocumentOrder()
    {
        var (set, _) = Load(
            Schema(
                "<xs:annotation/><xs:element name=\"e\"/><xs:attribute name=\"a\"/><xs:notation name=\"n\" public=\"p\"/>"
                    + SimpleT.Replace("\"T\"", "\"s\"", StringComparison.Ordinal)
                    + "<xs:complexType name=\"c\"/><xs:group name=\"g\"><xs:sequence/></xs:group><xs:attributeGroup name=\"ag\"/>"
            ),
            new MapResolver()
        );

        await Assert
            .That(Names(set.Units[0]))
            .IsEqualTo("element:e,attribute:a,notation:n,simpleType:s,complexType:c,group:g,attributeGroup:ag");
    }

    [Test]
    public async Task Redefine_OfComplexTypes_AcceptsSimpleContentAndPrefixedSelfReferences()
    {
        var resolver = new MapResolver().Add(
            "file:///a.xsd",
            Schema(
                "<xs:complexType name=\"T\"><xs:simpleContent><xs:extension base=\"xs:string\"/></xs:simpleContent></xs:complexType>"
                    + "<xs:group name=\"G\"><xs:sequence/></xs:group>",
                "urn:r"
            )
        );

        var (set, issues) = Load(
            Schema(
                "<xs:redefine schemaLocation=\"a.xsd\">"
                    + "<xs:complexType name=\"T\"><xs:simpleContent><xs:extension base=\"t:T\"/></xs:simpleContent></xs:complexType>"
                    + "<xs:group name=\"G\"><xs:sequence><xs:group ref=\"t:G\" minOccurs=\"1\" maxOccurs=\"1\"/></xs:sequence></xs:group>"
                    + "<xs:annotation/></xs:redefine>",
                "urn:r"
            ),
            resolver
        );

        await Assert.That(issues.Count).IsEqualTo(0);
        var extension = set.Units[0].Components[0].GetChildNode(0).GetChildNode(0);
        await Assert
            .That(extension.FindAttribute("base"))
            .IsEqualTo("t:" + set.Units[1].Components[0].FindAttribute("name"));
    }

    [Test]
    public async Task Redefine_SelfReferenceInAnotherNamespace_IsNotASelfReference()
    {
        var resolver = new MapResolver().Add("file:///a.xsd", Schema(SimpleT, "urn:r"));

        var (_, issues) = Load(
            Schema(
                "<xs:redefine schemaLocation=\"a.xsd\" xmlns:o=\"urn:other\"><xs:simpleType name=\"T\"><xs:restriction base=\"o:T\"/></xs:simpleType></xs:redefine>",
                "urn:r"
            ),
            resolver
        );

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(CodeOf("src-redefine"));
    }

    [Test]
    public async Task Redefine_SelfReferenceWithUndeclaredPrefix_IsInvalid()
    {
        var resolver = new MapResolver().Add("file:///a.xsd", Schema(SimpleT));

        var (_, issues) = Load(
            Schema(
                "<xs:redefine schemaLocation=\"a.xsd\"><xs:simpleType name=\"T\"><xs:restriction base=\"zz:T\"/></xs:simpleType></xs:redefine>"
            ),
            resolver
        );

        await Assert.That(issues.Count).IsEqualTo(1);
    }
}
