namespace NetEvolve.Schemas.Xsd.Tests.Unit.Compilation;

using System;
using System.Linq;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Notations;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using NetEvolve.Schemas.Xsd.Tests.Unit.Elements;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Compilation.CompilationFixture;

/// <summary>Notation declarations become components in the notation symbol space (Structures 3.12, #751).</summary>
public class NotationTests
{
    [Test]
    [Arguments("<xs:notation name=\"n\" public=\"p\"/>")]
    [Arguments("<xs:notation name=\"n\" system=\"s\"/>")]
    [Arguments("<xs:notation name=\"n\" public=\"p\" system=\"s\"><xs:annotation/></xs:notation>")]
    public async Task Notation_WithPublicOrSystem_IsRegisteredInTheTargetNamespace(string body)
    {
        var compilation = Compile(Schema(body));

        await Assert.That(Describe(compilation)).IsEmpty();
        await Assert.That(compilation.Set!.TryGet(XsdComponentKind.Notation, new(Tns, "n"), out _)).IsTrue();
    }

    [Test]
    public async Task Notation_WithoutPublicAndSystem_ViolatesSrcNotation()
    {
        var compilation = Compile(Schema("<xs:notation name=\"n\"/>"));

        await Assert.That(Codes(compilation)).IsEquivalentTo([XsdRuleCodes.SrcNotation]);
        await Assert.That(compilation.Result.Issues[0].Location.Line).IsGreaterThan(0);
    }

    [Test]
    public async Task Notation_DeclaredTwice_IsReportedOnce()
    {
        var compilation = Compile(
            Schema("<xs:notation name=\"n\" public=\"a\"/><xs:notation name=\"n\" public=\"b\"/>")
        );

        await Assert.That(Codes(compilation)).IsEquivalentTo([XsdRuleCodes.SchPropsCorrect]);
    }

    [Test]
    [Arguments("public=\"p\"", "p", null)]
    [Arguments("system=\"s\"", null, "s")]
    [Arguments("public=\"p\" system=\"s\"", "p", "s")]
    public async Task Notation_Component_HoldsItsIdentifiers(string attributes, string? publicId, string? systemId)
    {
        var compilation = Compile(Schema("<xs:notation name=\"n\" " + attributes + "/>"));

        var notation = Get<NotationDeclaration>(compilation, XsdComponentKind.Notation, "n");

        await Assert.That(notation.Kind).IsEqualTo(XsdComponentKind.Notation);
        await Assert.That(notation.PublicId).IsEqualTo(publicId);
        await Assert.That(notation.SystemId).IsEqualTo(systemId);
    }

    [Test]
    public async Task Builder_ReportsItsKind() =>
        await Assert
            .That(new NotationDeclarationBuilder(new("urn:test", false)).Kind)
            .IsEqualTo(XsdComponentKind.Notation);

    [Test]
    public async Task Builder_RejectsNullArguments()
    {
        var fixture = ElementFixture.Build(string.Empty);
        var builder = new NotationDeclarationBuilder(new("urn:test", false));
        var syntax = new FakeSyntaxElement("notation", 1);

        await Assert
            .That(() => builder.Build(null!, fixture.Registry, fixture.Reporter))
            .Throws<ArgumentNullException>();
        await Assert.That(() => builder.Build(syntax, null!, fixture.Reporter)).Throws<ArgumentNullException>();
        await Assert.That(() => builder.Build(syntax, fixture.Registry, null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Builder_RejectsSyntaxThatIsNotASchemaSyntaxNode()
    {
        var fixture = ElementFixture.Build(string.Empty);
        var syntax = new FakeSyntaxElement("notation", 1);

        await Assert
            .That(() =>
                new NotationDeclarationBuilder(new("urn:test", false)).Build(syntax, fixture.Registry, fixture.Reporter)
            )
            .Throws<NotSupportedException>();
    }

    [Test]
    [Arguments("")]
    [Arguments("name=\"\"")]
    public async Task Builder_WithoutName_RegistersNothingAndReportsNoDuplicate(string nameAttribute)
    {
        var fixture = ElementFixture.Build(string.Empty);
        var text =
            "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">"
            + $"<xs:notation {nameAttribute} public=\"p\"/><xs:notation {nameAttribute} public=\"q\"/></xs:schema>";
        var root = SchemaDocumentParser.Parse(text.AsSpan(), "file:///t.xsd", fixture.Reporter).Root!;
        var builder = new NotationDeclarationBuilder(new("urn:test", false));
        var before = fixture.Registry.Count;

        _ = builder.Build(root.GetChildNode(0), fixture.Registry, fixture.Reporter);
        _ = builder.Build(root.GetChildNode(1), fixture.Registry, fixture.Reporter);

        await Assert.That(fixture.Registry.Count).IsEqualTo(before);
        await Assert.That(fixture.Issues.Select(i => i.Code)).DoesNotContain(XsdRuleCodes.SchPropsCorrect);
    }
}
