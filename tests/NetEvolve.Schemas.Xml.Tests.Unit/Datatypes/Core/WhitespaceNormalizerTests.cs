namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Core;

using System;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.Xml.Datatypes.Core;

public class WhitespaceNormalizerTests
{
    private static string Normalize(ReadOnlySpan<char> source, WhitespaceMode mode)
    {
        var destination = new char[source.Length];
        var length = WhitespaceNormalizer.Normalize(source, mode, destination);
        return new string(destination, 0, length);
    }

    [Test]
    [Arguments("a\tb\nc\rd e", "a\tb\nc\rd e")]
    [Arguments("  a  ", "  a  ")]
    public async Task Preserve_KeepsEveryCharacter(string source, string expected) =>
        await Assert.That(Normalize(source.AsSpan(), WhitespaceMode.Preserve)).IsEqualTo(expected);

    [Test]
    [Arguments("a\tb", "a b")]
    [Arguments("a\nb", "a b")]
    [Arguments("a\rb", "a b")]
    [Arguments("\t\n\r ", "    ")]
    [Arguments("a\r\nb", "a  b")]
    public async Task Replace_MapsTabLineFeedAndCarriageReturnToSpace(string source, string expected) =>
        await Assert.That(Normalize(source.AsSpan(), WhitespaceMode.Replace)).IsEqualTo(expected);

    [Test]
    [Arguments("a\t\t b", "a b")]
    [Arguments("  a b  ", "a b")]
    [Arguments("\r\n a \n\r b\t", "a b")]
    [Arguments("   ", "")]
    [Arguments("", "")]
    [Arguments("abc", "abc")]
    public async Task Collapse_TrimsAndMergesRuns(string source, string expected) =>
        await Assert.That(Normalize(source.AsSpan(), WhitespaceMode.Collapse)).IsEqualTo(expected);

    [Test]
    [Arguments("a b", WhitespaceMode.Collapse, true)]
    [Arguments("a  b", WhitespaceMode.Collapse, false)]
    [Arguments(" a", WhitespaceMode.Collapse, false)]
    [Arguments("a ", WhitespaceMode.Collapse, false)]
    [Arguments("a\tb", WhitespaceMode.Collapse, false)]
    [Arguments("a  b", WhitespaceMode.Replace, true)]
    [Arguments("a\nb", WhitespaceMode.Replace, false)]
    [Arguments("a\nb", WhitespaceMode.Preserve, true)]
    public async Task IsNormalized_ReportsWhetherTheModeLeavesTheTextUnchanged(
        string source,
        WhitespaceMode mode,
        bool expected
    ) => await Assert.That(WhitespaceNormalizer.IsNormalized(source.AsSpan(), mode)).IsEqualTo(expected);

    [Test]
    public async Task Normalize_DestinationShorterThanSource_Throws() =>
        await Assert
            .That(() => WhitespaceNormalizer.Normalize("abc".AsSpan(), WhitespaceMode.Collapse, new char[2]))
            .Throws<ArgumentOutOfRangeException>();

    [Test]
    [Arguments(WhitespaceMode.Preserve, "a  b")]
    [Arguments(WhitespaceMode.Replace, "a  b")]
    [Arguments(WhitespaceMode.Collapse, "a b")]
    public async Task Apply_NormalizedInput_ReturnsTheSameTextWithoutRenting(WhitespaceMode mode, string source)
    {
        var result = WhitespaceNormalizer.Apply(source.AsSpan(), mode, out var rented).ToString();
        using var owner = rented;

        await Assert.That(rented).IsNull();
        await Assert.That(result).IsEqualTo(source);
    }

    [Test]
    public async Task Apply_UnnormalizedInput_RentsABufferThatTheCallerDisposes()
    {
        var result = WhitespaceNormalizer
            .Apply(" a \t b ".AsSpan(), WhitespaceMode.Collapse, out var rented)
            .ToString();
        using var owner = rented;

        await Assert.That(rented).IsNotNull();
        await Assert.That(result).IsEqualTo("a b");
    }

    [Test]
    public void Collapse_AlreadyCollapsedSpan_AllocatesZeroBytes()
    {
        const string source = "alpha beta gamma";

        AllocationAssert.NoAllocation(() =>
        {
            _ = WhitespaceNormalizer.Apply(source.AsSpan(), WhitespaceMode.Collapse, out var rented);
            rented?.Dispose();
        });
    }

    [Test]
    [Arguments("  a  b ", 0, 2)]
    [Arguments("  a  b ", 1, 3)]
    [Arguments("  a  b ", 2, 5)]
    [Arguments("a\t\tb", 1, 1)]
    [Arguments("a\t\tb", 2, 3)]
    public async Task ToSourceOffset_Collapse_PointsAtTheCharacterInTheOriginal(
        string source,
        int normalizedOffset,
        int expected
    ) =>
        await Assert
            .That(WhitespaceNormalizer.ToSourceOffset(source.AsSpan(), WhitespaceMode.Collapse, normalizedOffset))
            .IsEqualTo(expected);

    [Test]
    public async Task ToSourceOffset_ReplaceAndPreserve_AreTheIdentity()
    {
        await Assert.That(WhitespaceNormalizer.ToSourceOffset("a\tb".AsSpan(), WhitespaceMode.Replace, 2)).IsEqualTo(2);
        await Assert
            .That(WhitespaceNormalizer.ToSourceOffset("a\tb".AsSpan(), WhitespaceMode.Preserve, 2))
            .IsEqualTo(2);
    }
}
