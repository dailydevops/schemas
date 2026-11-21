namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Library.Formatting;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Schematron.Xslt.Library.Formatting;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.XPath.Api;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.FunctionTree;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Library.Formatting.FormatFixture;

/// <summary>The compile of <c>xsl:decimal-format</c> (XSLT 2.0 section 16.4.1): the name, the attributes, and the errors of a value.</summary>
public class DecimalFormatInstructionTests
{
    [Test]
    public async Task Compile_WithoutName_IsTheUnnamedFormat()
    {
        var fixture = new FormatFixture();

        var declaration = fixture.CompileOne(Format(null, 10));

        await Assert.That(declaration).IsNotNull();
        await Assert.That(declaration!.Name).IsEqualTo(default(XPathQName));
        await Assert.That(declaration.DisplayName).IsEqualTo(string.Empty);
        await Assert.That(declaration.Location.Line).IsEqualTo(10);
        await Assert.That(fixture.Problems.Problems).IsEmpty();
    }

    [Test]
    public async Task Compile_UnprefixedName_IsInNoNamespace()
    {
        var fixture = new FormatFixture();

        var declaration = fixture.CompileOne(Format("ch", 10));

        await Assert.That(declaration!.Name).IsEqualTo(new XPathQName(string.Empty, "ch"));
        await Assert.That(declaration.DisplayName).IsEqualTo("ch");
    }

    [Test]
    public async Task Compile_PrefixedName_ExpandsTheNamespace()
    {
        var fixture = new FormatFixture();

        var declaration = fixture.CompileOne(Format("f:ch", 10));

        await Assert.That(declaration!.Name).IsEqualTo(new XPathQName(Functions, "ch"));
        await Assert.That(declaration.DisplayName).IsEqualTo("f:ch");
    }

    [Test]
    public async Task Compile_UnboundPrefix_IsXtse0280()
    {
        var fixture = new FormatFixture();

        var declaration = fixture.CompileOne(Format("zz:ch", 10));

        await Assert.That(declaration).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo(SchematronDiagnosticCodes.AttributeInvalid + "/XTSE0280");
        await Assert.That(fixture.Problems.Problems[0].Location.Line).IsEqualTo(10);
    }

    [Test]
    [Arguments("1ch")]
    [Arguments("")]
    [Arguments("a b")]
    public async Task Compile_NameThatIsNotAQName_IsXtse0020(string name)
    {
        var fixture = new FormatFixture();

        var declaration = fixture.CompileOne(Format(name, 10));

        await Assert.That(declaration).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo(SchematronDiagnosticCodes.AttributeInvalid + "/XTSE0020");
    }

    // Every attribute of the declaration is kept as written, so that the table passes it on to the decimal format of the XPath package.
    [Test]
    public async Task Compile_AllAttributes_AreKept()
    {
        var fixture = new FormatFixture();

        var declaration = fixture.CompileOne(
            Format(
                "all",
                10,
                "decimal-separator",
                ",",
                "grouping-separator",
                ".",
                "percent",
                "p",
                "per-mille",
                "m",
                "zero-digit",
                "٠",
                "digit",
                "_",
                "pattern-separator",
                "|",
                "minus-sign",
                "~",
                "infinity",
                "inf",
                "NaN",
                "none"
            )
        );

        await Assert.That(XsltDecimalFormatDeclaration.Attributes.Count).IsEqualTo(10);
        string[] expected = [",", ".", "p", "m", "٠", "_", "|", "~", "inf", "none"];
        for (var i = 0; i < expected.Length; i++)
        {
            await Assert.That(declaration!.Values[i]).IsEqualTo(expected[i]);
        }
    }

    [Test]
    public async Task Compile_AttributesThatAreLeftOut_AreNotSet()
    {
        var fixture = new FormatFixture();

        var declaration = fixture.CompileOne(Format(null, 10, "percent", "p"));

        await Assert.That(declaration!.Values.Count).IsEqualTo(10);
        await Assert.That(declaration.Values[2]).IsEqualTo("p");
        await Assert.That(declaration.Values[0]).IsNull();
        await Assert.That(declaration.Values[9]).IsNull();
    }

    [Test]
    [Arguments("decimal-separator", "")]
    [Arguments("decimal-separator", "ab")]
    [Arguments("grouping-separator", "..")]
    [Arguments("percent", "%%")]
    [Arguments("per-mille", "")]
    [Arguments("zero-digit", "00")]
    [Arguments("digit", "##")]
    [Arguments("pattern-separator", ";;")]
    [Arguments("minus-sign", "--")]
    public async Task Compile_CharacterAttributeWithoutExactlyOneCharacter_IsXtse0020(string attribute, string value)
    {
        var fixture = new FormatFixture();

        var declaration = fixture.CompileOne(Format(null, 10, attribute, value));

        await Assert.That(declaration).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo(SchematronDiagnosticCodes.AttributeInvalid + "/XTSE0020");
        await Assert.That(fixture.Problems.Problems[0].Message).Contains(attribute);
    }

    // The infinity and NaN attributes are strings: any text, also the empty one.
    [Test]
    [Arguments("infinity", "")]
    [Arguments("infinity", "unbounded")]
    [Arguments("NaN", "")]
    [Arguments("NaN", "not a number")]
    public async Task Compile_StringAttribute_TakesAnyText(string attribute, string value)
    {
        var fixture = new FormatFixture();

        var declaration = fixture.CompileOne(Format(null, 10, attribute, value));

        await Assert.That(declaration).IsNotNull();
        await Assert.That(fixture.Problems.Problems).IsEmpty();
    }

    // Limitation: the decimal format of the XPath package holds UTF-16 code units, so one character beyond the BMP is refused.
    [Test]
    public async Task Compile_CharacterBeyondTheBasicMultilingualPlane_IsTheUnsupportedError()
    {
        var fixture = new FormatFixture();

        var declaration = fixture.CompileOne(Format(null, 10, "zero-digit", "\U0001D7CE"));

        await Assert.That(declaration).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo(SchematronDiagnosticCodes.XsltUnsupported.ToString());
        await Assert.That(fixture.Problems.Problems[0].Message).Contains("zero-digit");
    }

    // Limitation: exponent-separator is an XSLT 3.0 attribute and the XSLT 2.0 rules of format-number do not read it.
    [Test]
    public async Task Compile_ExponentSeparator_IsTheUnsupportedError()
    {
        var fixture = new FormatFixture();

        var declaration = fixture.CompileOne(Format(null, 10, "exponent-separator", "x"));

        await Assert.That(declaration).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo(SchematronDiagnosticCodes.XsltUnsupported.ToString());
        await Assert.That(fixture.Problems.Problems[0].Message).Contains("exponent-separator");
    }

    [Test]
    public async Task Compile_SeveralProblems_AreAllReported()
    {
        var fixture = new FormatFixture();

        var declaration = fixture.CompileOne(Format("zz:ch", 10, "percent", "", "digit", "##"));

        await Assert.That(declaration).IsNull();
        await Assert.That(fixture.Problems.Problems.Count).IsEqualTo(3);
    }

    [Test]
    public async Task Compile_WithANullContext_Throws() =>
        await Assert
            .That(() => new XsltDecimalFormatInstruction().TryCompile(null!, out _, out _))
            .Throws<ArgumentNullException>();

    [Test]
    public async Task Declaration_Executes_ToNothing()
    {
        var fixture = new FormatFixture();
        var declaration = fixture.CompileOne(Format(null, 10))!;
        using var run = new FakeQueryRun();
        var frame = new XsltFrame(run);
        var sink = new FakeSequenceSink();

        var status = declaration.TryExecute(frame, sink);

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(sink.Texts).IsEmpty();
    }

    [Test]
    public async Task Declaration_ExecuteWithNullArguments_Throws()
    {
        var fixture = new FormatFixture();
        var declaration = fixture.CompileOne(Format(null, 10))!;
        using var run = new FakeQueryRun();
        var frame = new XsltFrame(run);

        await Assert.That(() => declaration.TryExecute(null!, new FakeSequenceSink())).Throws<ArgumentNullException>();
        await Assert.That(() => declaration.TryExecute(frame, null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Registry_ResolvesXslDecimalFormat_AsADeclarationOnly()
    {
        var fixture = new FormatFixture();
        var name = new XPathQName(XsltInstructionRegistry.Namespace, "decimal-format");

        await Assert.That(fixture.Registry.TryGet(in name, out var instruction)).IsTrue();
        await Assert.That(instruction.Position).IsEqualTo(XsltPosition.Declaration);
        await Assert.That(fixture.Registry.IsAvailable(name)).IsFalse();
    }
}
