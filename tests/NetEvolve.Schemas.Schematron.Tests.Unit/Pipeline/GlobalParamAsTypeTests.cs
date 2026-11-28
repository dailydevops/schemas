namespace NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline;

using System;
using System.Linq;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline.PipelineFixture;

/// <summary>
/// A caller value of a global <c>xsl:param</c> is converted to the <see langword="as"/> type of the parameter (XSLT 2.0 section 9.5,
/// #795); a value that does not convert is XTTE0590 at the first use, and a parameter without <see langword="as"/> keeps the string.
/// </summary>
public class GlobalParamAsTypeTests
{
    private const string Attributes =
        " queryBinding=\"xslt2\" schematronEdition=\"2025\" xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"";

    private const string Ns = "<ns prefix=\"xs\" uri=\"http://www.w3.org/2001/XMLSchema\"/>";

    private static ValidationResult Run(string param, string test, string? value)
    {
        var schema = Wrap(
            Ns
                + param
                + $"<pattern id=\"p\"><rule context=\"doc\"><assert test=\"{test}\">Failed.</assert></rule></pattern>",
            Attributes
        );
        var builder = Builder(schema);
        if (value is not null)
        {
            _ = builder.WithParameter("p", value);
        }

        return Validate(builder.BuildNow(), "<doc/>");
    }

    [Test]
    [Arguments("xs:integer", "$p + 1 = 11", " 10 ")]
    [Arguments("xs:integer", "$p + 1 = 11", "+10")]
    [Arguments("xs:double", "$p * 2 = 3", "1.5")]
    [Arguments("xs:double", "$p = 1000", "1e3")]
    [Arguments("xs:boolean", "$p", "true")]
    [Arguments("xs:boolean", "$p", "1")]
    [Arguments("xs:boolean", "not($p)", "false")]
    [Arguments("xs:string", "$p = '10'", "10")]
    [Arguments("xs:double", "$p = xs:double('INF')", "INF")]
    [Arguments("xs:double", "$p = xs:double('INF')", "+INF")]
    [Arguments("xs:double", "$p = xs:double('-INF')", "-INF")]
    [Arguments("xs:double", "$p != $p", "NaN")]
    [Arguments("xs:integer", "$p = -9223372036854775808", "-9223372036854775808")]
    public async Task Value_ThatConverts_IsBoundWithTheDeclaredType(string type, string test, string value)
    {
        ArgumentNullException.ThrowIfNull(type);
        var result = Run($"<xsl:param name=\"p\" as=\"{type}\" select=\"{DefaultOf(type)}\"/>", test, value);

        await Assert.That(result.IsValid).IsTrue().Because(string.Join("; ", result.Issues.Select(i => i.Message)));
    }

    [Test]
    [Arguments("xs:integer", "abc")]
    [Arguments("xs:integer", "1.5")]
    [Arguments("xs:integer", "")]
    [Arguments("xs:double", "x")]
    [Arguments("xs:boolean", "yes")]
    [Arguments("xs:double", "")]
    [Arguments("xs:double", "Infinity")]
    [Arguments("xs:double", "nan")]
    [Arguments("xs:integer", "9223372036854775808")]
    public async Task Value_ThatDoesNotConvert_IsXtte0590AtFirstUse(string type, string value)
    {
        var result = Run($"<xsl:param name=\"p\" as=\"{type}\" select=\"{DefaultOf(type)}\"/>", "boolean($p)", value);

        await Assert.That(result.IsValid).IsFalse();
        await Assert
            .That(result.Issues.Any(i => (i.Message + i.Constraint).Contains("XTTE0590", StringComparison.Ordinal)))
            .IsTrue()
            .Because(string.Join("; ", result.Issues.Select(i => i.Code + " " + i.Message + " " + i.Constraint)));
    }

    [Test]
    public async Task Value_ThatDoesNotConvert_IsNoErrorWhenTheParameterIsNeverRead()
    {
        var result = Run("<xsl:param name=\"p\" as=\"xs:integer\" select=\"1\"/>", "true()", "abc");

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Parameter_WithoutAs_KeepsTheString()
    {
        var result = Run("<xsl:param name=\"p\" select=\"1\"/>", "$p = '10'", "10");

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Parameter_WithoutCallerValue_UsesTheDefault()
    {
        var result = Run("<xsl:param name=\"p\" as=\"xs:integer\" select=\"7\"/>", "$p = 7", null);

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Parameter_AfterAGlobalVariable_TakesTheCallerValue()
    {
        var result = Run(
            "<xsl:variable name=\"v\" select=\"1\"/><xsl:param name=\"p\" as=\"xs:integer\" select=\"7\"/>",
            "$p + $v = 11",
            "10"
        );

        await Assert.That(result.IsValid).IsTrue().Because(string.Join("; ", result.Issues.Select(i => i.Message)));
    }

    [Test]
    public async Task CallerValue_OfAnUndeclaredName_IsRejectedNextToGlobalDeclarations()
    {
        var schema = Wrap(
            Ns
                + "<xsl:variable name=\"v\" select=\"1\"/><xsl:param name=\"p\" as=\"xs:integer\" select=\"7\"/>"
                + "<pattern id=\"p\"><rule context=\"doc\"><assert test=\"true()\">Failed.</assert></rule></pattern>",
            Attributes
        );

        var failure = Capture(() => _ = Builder(schema).WithParameter("v", "1").WithParameter("zzz", "1").Build());

        await Assert.That(failure.Message).Contains("2 issue(s)").And.Contains("NES3112");
    }

    private static string DefaultOf(string type) =>
        type switch
        {
            "xs:string" => "'d'",
            "xs:boolean" => "false()",
            "xs:double" => "0e0",
            _ => "0",
        };
}
