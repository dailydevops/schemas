namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Floating;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;

public class ParseNumberFxxTests
{
    [Test]
    [Arguments("3C00 3F800000 3FF0000000000000 1")]
    [Arguments("3D00 3FA00000 3FF4000000000000 1.25")]
    [Arguments("3D9A 3FB33333 3FF6666666666666 1.4")]
    [Arguments("7C00 7F800000 7FF0000000000000 123.456e789")]
    [Arguments("0000 00000000 0000000000000000 .0")]
    [Arguments("8000 80000000 8000000000000000 -0.")]
    [Arguments("0000 00000000 0000000000000000 1e-9223372036854775809")]
    public async Task Check_LineInTheLexicalSpace_PassesWhenTheBitsMatch(string line) =>
        await Assert.That(ParseNumberFxx.Check(line, XsdVersion.V10)).IsEqualTo(ParseNumberFxxOutcome.Passed);

    [Test]
    [Arguments("3C00 3F800001 3FF0000000000000 1")]
    [Arguments("3C00 3F800000 3FF0000000000001 1")]
    public async Task Check_LineWithOtherBits_IsAMismatch(string line) =>
        await Assert.That(ParseNumberFxx.Check(line, XsdVersion.V10)).IsEqualTo(ParseNumberFxxOutcome.Mismatch);

    [Test]
    [Arguments("7C00 7F800000 7FF0000000000000 inf")]
    [Arguments("7E00 7FC00000 7FF8000000000000 nan")]
    [Arguments("3C00 3F800000 3FF0000000000000 0x1p0")]
    [Arguments("3C00 3F800000 3FF0000000000000 1e")]
    [Arguments("3C00 3F800000 3FF0000000000000 e1")]
    [Arguments("3C00 3F800000 3FF0000000000000 .")]
    [Arguments("3C00 3F800000 3FF0000000000000 1x")]
    [Arguments("3C00 3F800000 3FF0000000000000 +")]
    public async Task Check_LineOutsideTheLexicalSpace_IsRejected(string line) =>
        await Assert.That(ParseNumberFxx.Check(line, XsdVersion.V10)).IsEqualTo(ParseNumberFxxOutcome.Rejected);

    [Test]
    [Arguments("")]
    [Arguments("3C00 3F800000 3FF0000000000000 ")]
    [Arguments("3C00_3F800000 3FF0000000000000 1")]
    [Arguments("3C00 3F800000_3FF0000000000000 1")]
    [Arguments("3C00 3F800000 3FF0000000000000_1")]
    [Arguments("3C00 3F80000G 3FF0000000000000 1")]
    [Arguments("3C00 3F800000 3FF000000000000G 1")]
    public async Task Check_LineWithOtherLayout_IsMalformed(string line) =>
        await Assert.That(ParseNumberFxx.Check(line, XsdVersion.V10)).IsEqualTo(ParseNumberFxxOutcome.Malformed);

    [Test]
    public async Task Check_PlusInfinity_IsAcceptedByNeitherLexicalRule()
    {
        // "+INF" is outside the finite lexical space, and the version 1.0 parser rejects it.
        await Assert
            .That(ParseNumberFxx.Check("7C00 7F800000 7FF0000000000000 +INF", XsdVersion.V10))
            .IsEqualTo(ParseNumberFxxOutcome.Rejected);
        await Assert
            .That(ParseNumberFxx.Check("7C00 7F800000 7FF0000000000000 +INF", XsdVersion.V11))
            .IsEqualTo(ParseNumberFxxOutcome.Mismatch);
    }
}
