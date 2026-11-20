namespace NetEvolve.Schemas.XPath.Tests.Unit.Compatibility;

using static CompatibilityFixture;

/// <summary>
/// The function conversion rules of XPath 2.0, 3.1.5 in compatibility mode: an argument that is not of the expected type is replaced
/// by its first item when one item is expected, converted by <c>fn:string</c> when the expected type is <c>xs:string</c> or
/// <c>xs:string?</c>, and by <c>fn:number</c> when it is <c>xs:double</c> or <c>xs:double?</c>. Without the mode the argument must match,
/// or be an untyped value that casts, or XPTY0004 is raised.
/// </summary>
public class FunctionConversionTests
{
    [Test]
    [Arguments("string-length(12345)", "integer:5", "XPTY0004")]
    [Arguments("string-length($yes)", "integer:4", "XPTY0004")]
    [Arguments("contains(123, 2)", "true", "XPTY0004")]
    [Arguments("substring-before(10 div 3, '.')", "string:3", "XPTY0004")]
    [Arguments("normalize-space($yes)", "string:true", "XPTY0004")]
    [Arguments("upper-case($dec)", "string:2.5", "XPTY0004")]
    public async Task StringParameter_IsConvertedByFnString(string expression, string compatibility, string standard) =>
        await AssertModes(expression, compatibility, standard);

    [Test]
    [Arguments("substring('abc', '2')", "string:bc", "XPTY0004")]
    [Arguments("substring('abc', true())", "string:abc", "XPTY0004")]
    [Arguments("substring('abcdef', $text3, '2')", "string:cd", "XPTY0004")]
    [Arguments("substring('abcdef', $none)", "string:", "XPTY0004")]
    public async Task DoubleParameter_IsConvertedByFnNumber(string expression, string compatibility, string standard) =>
        await AssertModes(expression, compatibility, standard);

    [Test]
    [Arguments("string-length($none)")]
    [Arguments("upper-case('a')")]
    [Arguments("substring('abc', 2)")]
    [Arguments("substring('abc', 2.0e0, 1.0e0)")]
    [Arguments("floor($u3)")]
    [Arguments("abs($word)")]
    [Arguments("concat($yes, 1)")]
    public async Task AnArgumentOfTheExpectedTypeOrAnUntypedValue_GivesTheSameResultInBothModes(string expression) =>
        await Assert.That(Run(expression, compatibility: true)).IsEqualTo(Run(expression, compatibility: false));

    [Test]
    [Arguments("floor('3.5')")]
    [Arguments("abs($text3)")]
    public async Task ANumericParameter_IsNotConvertedFromAString(string expression)
    {
        // 3.1.5 names xs:string and xs:double only; the other numeric parameters keep the XPath 2.0 rules, so the string stays XPTY0004.
        await Assert.That(Run(expression, compatibility: true)).IsEqualTo("XPTY0004");
        await Assert.That(Run(expression, compatibility: false)).IsEqualTo("XPTY0004");
    }
}
