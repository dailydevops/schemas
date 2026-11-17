namespace NetEvolve.Schemas.Xml.Tests.Unit.Reader.Declaration;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Reader.Declaration;

public class XmlDeclarationParserTests
{
    // A ref struct result cannot cross an await, so each test reads it through this snapshot.
    private readonly record struct Snapshot(
        bool IsPresent,
        bool IsValid,
        XmlErrorCode? Error,
        int ErrorOffset,
        int Length,
        string Version,
        string Encoding,
        bool? Standalone,
        long? VersionOffset,
        long? EncodingOffset,
        long? StandaloneOffset
    );

    private static Snapshot Run(string input, XmlDeclarationKind kind)
    {
        var r = XmlDeclarationParser.Parse(input, kind);
        return new Snapshot(
            r.IsPresent,
            r.IsValid,
            r.Error,
            r.ErrorOffset,
            r.Length,
            r.Version.ToString(),
            r.Encoding.ToString(),
            r.Standalone,
            r.VersionPosition?.CharOffset,
            r.EncodingPosition?.CharOffset,
            r.StandalonePosition?.CharOffset
        );
    }

    private static XmlErrorCode? ErrorOf(string input, XmlDeclarationKind kind) => Run(input, kind).Error;

    private static long IndexOf(string input, string value) => input.IndexOf(value, StringComparison.Ordinal);

    // Production 23 XMLDecl, 24 VersionInfo, 25 Eq, 26 VersionNum.
    [Test]
    [Arguments("<?xml version=\"1.0\"?>", 21)]
    [Arguments("<?xml version='1.0'?>", 21)]
    [Arguments("<?xml  version = '1.0'  ?>", 26)]
    [Arguments("<?xml\r\n\tversion\n=\r'1.0'\n?>", 26)]
    public async Task Parse_XmlDeclWithVersion_IsValid(string input, int length)
    {
        var result = Run(input, XmlDeclarationKind.Xml);

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(result.Version).IsEqualTo("1.0");
        await Assert.That(result.Encoding).IsEmpty();
        await Assert.That(result.Standalone).IsNull();
        await Assert.That(result.Length).IsEqualTo(length);
    }

    [Test]
    public async Task Parse_XmlDeclWithAllPseudoAttributes_ReportsValuesAndPositions()
    {
        const string Input = "<?xml version=\"1.0\" encoding='UTF-8' standalone=\"yes\" ?>";

        var result = Run(Input, XmlDeclarationKind.Xml);

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(result.Version).IsEqualTo("1.0");
        await Assert.That(result.Encoding).IsEqualTo("UTF-8");
        await Assert.That(result.Standalone).IsTrue();
        await Assert.That(result.VersionOffset).IsEqualTo(IndexOf(Input, "1.0"));
        await Assert.That(result.EncodingOffset).IsEqualTo(IndexOf(Input, "UTF-8"));
        await Assert.That(result.StandaloneOffset).IsEqualTo(IndexOf(Input, "yes"));
    }

    [Test]
    public async Task Parse_XmlDeclStandaloneNo_IsFalse()
    {
        var result = Run("<?xml version='1.0' standalone='no'?>", XmlDeclarationKind.Xml);

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(result.Standalone).IsFalse();
    }

    [Test]
    public async Task Parse_InputAfterDeclaration_LengthStopsAtClosingMarker()
    {
        const string Decl = "<?xml version='1.0'?>";

        var result = Run(Decl + "<root/>", XmlDeclarationKind.Xml);

        await Assert.That(result.Length).IsEqualTo(Decl.Length);
    }

    // Production 80 EncodingDecl and 81 EncName.
    [Test]
    [Arguments("<?xml version='1.0' encoding=\"UTF-8\"?>", "UTF-8")]
    [Arguments("<?xml version='1.0' encoding = 'iso-8859-1'?>", "iso-8859-1")]
    [Arguments("<?xml version='1.0' encoding='A'?>", "A")]
    [Arguments("<?xml version='1.0' encoding='x_y.z-9'?>", "x_y.z-9")]
    public async Task Parse_EncodingDecl_ReportsEncodingName(string input, string expected)
    {
        var result = Run(input, XmlDeclarationKind.Xml);

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(result.Encoding).IsEqualTo(expected);
    }

    [Test]
    [Arguments("<?xml version='1.0' encoding=''?>")]
    [Arguments("<?xml version='1.0' encoding='1abc'?>")]
    [Arguments("<?xml version='1.0' encoding='-abc'?>")]
    [Arguments("<?xml version='1.0' encoding='a b'?>")]
    [Arguments("<?xml version='1.0' encoding='a/b'?>")]
    [Arguments("<?xml version='1.0' encoding='ä'?>")]
    public async Task Parse_InvalidEncodingName_IsXML0017(string input) =>
        await Assert.That(ErrorOf(input, XmlDeclarationKind.Xml)).IsSameReferenceAs(XmlErrorCodes.XML0017);

    // Production 32 SDDecl.
    [Test]
    [Arguments("<?xml version='1.0' standalone='YES'?>")]
    [Arguments("<?xml version='1.0' standalone='true'?>")]
    [Arguments("<?xml version='1.0' standalone=''?>")]
    [Arguments("<?xml version='1.0' standalone='yes '?>")]
    public async Task Parse_StandaloneOtherThanYesOrNo_IsXML0017(string input) =>
        await Assert.That(ErrorOf(input, XmlDeclarationKind.Xml)).IsSameReferenceAs(XmlErrorCodes.XML0017);

    // Production 26 VersionNum.
    [Test]
    [Arguments("<?xml version='1.1'?>", "1.1")]
    [Arguments("<?xml version='1.10'?>", "1.10")]
    [Arguments("<?xml version='1.0'?>", "1.0")]
    public async Task Parse_Version1x_IsAcceptedAndReported(string input, string expected)
    {
        var result = Run(input, XmlDeclarationKind.Xml);

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(result.Version).IsEqualTo(expected);
    }

    [Test]
    [Arguments("<?xml version='2.0'?>")]
    [Arguments("<?xml version='1.'?>")]
    [Arguments("<?xml version='1.x'?>")]
    [Arguments("<?xml version=''?>")]
    [Arguments("<?xml version='0.9'?>")]
    public async Task Parse_VersionOutsideProduction26_IsXML0010(string input) =>
        await Assert.That(ErrorOf(input, XmlDeclarationKind.Xml)).IsSameReferenceAs(XmlErrorCodes.XML0010);

    // Wrong order, duplicates, missing parts, bad syntax: XML0003.
    [Test]
    [Arguments("<?xml encoding='UTF-8' version='1.0'?>")]
    [Arguments("<?xml version='1.0' standalone='yes' encoding='UTF-8'?>")]
    [Arguments("<?xml standalone='yes' version='1.0'?>")]
    [Arguments("<?xml version='1.0' version='1.0'?>")]
    [Arguments("<?xml version='1.0' encoding='UTF-8' encoding='UTF-8'?>")]
    [Arguments("<?xml version='1.0' standalone='yes' standalone='no'?>")]
    [Arguments("<?xml?>")]
    [Arguments("<?xml ?>")]
    [Arguments("<?xml encoding='UTF-8'?>")]
    [Arguments("<?xml standalone='yes'?>")]
    [Arguments("<?xml version='1.0'encoding='UTF-8'?>")]
    [Arguments("<?xml version '1.0'?>")]
    [Arguments("<?xml version=1.0?>")]
    [Arguments("<?xml version='1.0' foo='bar'?>")]
    [Arguments("<?xml version='1.0' ??>")]
    public async Task Parse_XmlDeclStructureError_IsXML0003(string input) =>
        await Assert.That(ErrorOf(input, XmlDeclarationKind.Xml)).IsSameReferenceAs(XmlErrorCodes.XML0003);

    [Test]
    [Arguments("<?xml version='1.0")]
    [Arguments("<?xml version='1.0'")]
    [Arguments("<?xml version='1.0' ?")]
    [Arguments("<?xml version=")]
    [Arguments("<?xml")]
    [Arguments("<?xml ")]
    public async Task Parse_TruncatedXmlDecl_IsXML0001(string input) =>
        await Assert.That(ErrorOf(input, XmlDeclarationKind.Xml)).IsSameReferenceAs(XmlErrorCodes.XML0001);

    // White space before the declaration.
    [Test]
    [Arguments(" <?xml version='1.0'?>", 1)]
    [Arguments("\n<?xml version='1.0'?>", 1)]
    [Arguments("\t\r\n <?xml version='1.0'?>", 4)]
    public async Task Parse_WhiteSpaceBeforeDeclaration_IsXML0007(string input, int declarationStart)
    {
        var result = Run(input, XmlDeclarationKind.Xml);

        await Assert.That(result.Error).IsSameReferenceAs(XmlErrorCodes.XML0007);
        await Assert.That((long)result.ErrorOffset).IsEqualTo(declarationStart);
    }

    [Test]
    [Arguments("<root/>")]
    [Arguments("")]
    [Arguments("   ")]
    [Arguments("<?xml-stylesheet href='a'?>")]
    [Arguments("<?xmlfoo bar?>")]
    [Arguments("<!-- c --><?xml version='1.0'?>")]
    public async Task Parse_NoDeclaration_IsAbsent(string input)
    {
        var result = Run(input, XmlDeclarationKind.Xml);

        await Assert.That(result.IsPresent).IsFalse();
        await Assert.That(result.Error).IsNull();
    }

    // Production 77 TextDecl.
    [Test]
    [Arguments("<?xml encoding='UTF-8'?>", "", "UTF-8")]
    [Arguments("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", "1.0", "UTF-8")]
    [Arguments("<?xml version='1.1' encoding = 'ascii' ?>", "1.1", "ascii")]
    [Arguments("<?xml\tencoding\n=\n\"utf-16\"\r?>", "", "utf-16")]
    public async Task Parse_TextDecl_IsValid(string input, string version, string encoding)
    {
        var result = Run(input, XmlDeclarationKind.Text);

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(result.Version).IsEqualTo(version);
        await Assert.That(result.Encoding).IsEqualTo(encoding);
        await Assert.That(result.Standalone).IsNull();
    }

    [Test]
    [Arguments("<?xml version='1.0'?>")]
    [Arguments("<?xml?>")]
    [Arguments("<?xml encoding='UTF-8' version='1.0'?>")]
    [Arguments("<?xml version='1.0' encoding='UTF-8' standalone='yes'?>")]
    [Arguments("<?xml encoding='UTF-8' encoding='UTF-8'?>")]
    public async Task Parse_TextDeclStructureError_IsXML0003(string input) =>
        await Assert.That(ErrorOf(input, XmlDeclarationKind.Text)).IsSameReferenceAs(XmlErrorCodes.XML0003);

    [Test]
    public async Task Parse_TextDeclInvalidEncodingOrVersion_ReportsCodes()
    {
        await Assert
            .That(ErrorOf("<?xml encoding='1x'?>", XmlDeclarationKind.Text))
            .IsSameReferenceAs(XmlErrorCodes.XML0017);
        await Assert
            .That(ErrorOf("<?xml version='2.0' encoding='UTF-8'?>", XmlDeclarationKind.Text))
            .IsSameReferenceAs(XmlErrorCodes.XML0010);
    }

    [Test]
    public async Task Parse_TextDeclWithLeadingWhiteSpace_IsXML0007() =>
        await Assert
            .That(ErrorOf(" <?xml encoding='UTF-8'?>", XmlDeclarationKind.Text))
            .IsSameReferenceAs(XmlErrorCodes.XML0007);
}
