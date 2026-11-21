namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Evaluated;

using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;
using static NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner.Qt3Fixtures;

public class Qt3AssertionReaderTests
{
    private static Qt3Case ReadCase(string result)
    {
        using var temp = new TempDirectory();
        var root = WriteSuite(temp, ("set", TestCase("c1", result: result)));
        return Qt3CatalogReader.Read(root)[0].Cases[0];
    }

    [Test]
    [Arguments("<assert>$result = 1</assert>", "assert", "$result = 1")]
    [Arguments("<assert-eq>1.5e0</assert-eq>", "assert-eq", "1.5e0")]
    [Arguments("<assert-count>3</assert-count>", "assert-count", "3")]
    [Arguments("<assert-string-value>a b</assert-string-value>", "assert-string-value", "a b")]
    public async Task Read_EvaluatedAssertion_KeepsTheText(string result, string kind, string value)
    {
        var assertion = ReadCase(result).Assertion;

        await Assert.That(assertion.Kind).IsEqualTo(kind);
        await Assert.That(assertion.Value).IsEqualTo(value);
        await Assert.That(assertion.IsEvaluated).IsTrue();
        await Assert.That(assertion.IsActive).IsFalse();
        await Assert.That(assertion.NormalizeSpace).IsFalse();
    }

    [Test]
    public async Task Read_AssertStringValueWithNormalizeSpace_KeepsTheFlag()
    {
        var assertion = ReadCase("<assert-string-value normalize-space=\"true\">a b</assert-string-value>").Assertion;

        await Assert.That(assertion.NormalizeSpace).IsTrue();
    }

    [Test]
    public async Task Read_ResultWithIndentation_FindsTheAssertionElement()
    {
        var assertion = ReadCase("\n   <assert-count>2</assert-count>\n").Assertion;

        await Assert.That(assertion.Kind).IsEqualTo("assert-count");
        await Assert.That(assertion.Value).IsEqualTo("2");
    }

    [Test]
    [Arguments(" ", " ")]
    [Arguments("&#13;", "\r")]
    [Arguments("&#9;&#10;", "\t\n")]
    public async Task Read_AssertStringValueOfOnlyWhitespace_KeepsTheWhitespace(string text, string expected)
    {
        var assertion = ReadCase($"<assert-string-value>{text}</assert-string-value>").Assertion;

        await Assert.That(assertion.Value).IsEqualTo(expected);
    }

    [Test]
    [Arguments("<assert-true/>")]
    [Arguments("<error code=\"XPST0003\"/>")]
    [Arguments("<assert-xml>x</assert-xml>")]
    [Arguments("<any-of><assert-eq>1</assert-eq></any-of>")]
    public async Task Read_OtherAssertion_IsNotEvaluated(string result) =>
        await Assert.That(ReadCase(result).Assertion.IsEvaluated).IsFalse();
}
