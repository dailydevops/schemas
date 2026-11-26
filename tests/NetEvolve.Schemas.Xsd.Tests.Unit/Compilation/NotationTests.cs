namespace NetEvolve.Schemas.Xsd.Tests.Unit.Compilation;

using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Notations;
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
}
