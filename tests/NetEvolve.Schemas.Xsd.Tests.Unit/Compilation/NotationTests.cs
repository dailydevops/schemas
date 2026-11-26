namespace NetEvolve.Schemas.Xsd.Tests.Unit.Compilation;

using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
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
    public async Task Notation_DeclaredTwice_IsReportedOnceAndTheFirstStays()
    {
        var compilation = Compile(
            Schema("<xs:notation name=\"n\" public=\"a\"/><xs:notation name=\"n\" public=\"b\"/>")
        );

        await Assert.That(Codes(compilation)).IsEquivalentTo([XsdRuleCodes.SchPropsCorrect]);
        await Assert.That(compilation.Set!.TryGet(XsdComponentKind.Notation, new(Tns, "n"), out var first)).IsTrue();
        await Assert
            .That(first!.Location.Line)
            .IsEqualTo(Get(compilation, XsdComponentKind.Notation, "n").Location.Line);
    }
}
