namespace NetEvolve.Schemas.Conformance.Tests.Unit.Results;

using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Tests.Unit.Fetch;

public class KnownFailureBaselineTests
{
    [Test]
    public async Task Parse_SortedUniqueIds_ReadsThem()
    {
        var baseline = KnownFailureBaseline.Parse("a/1\na/2\nb/1\n");

        await Assert.That(baseline.Count).IsEqualTo(3);
        await Assert.That(baseline.Contains("a/2")).IsTrue();
        await Assert.That(baseline.Contains("a/3")).IsFalse();
    }

    [Test]
    public async Task Parse_EmptyText_IsEmptyBaseline() =>
        await Assert.That(KnownFailureBaseline.Parse(string.Empty).Count).IsEqualTo(0);

    [Test]
    public async Task Parse_CrLfLineEndings_AreAccepted() =>
        await Assert.That(KnownFailureBaseline.Parse("a\r\nb\r\n").Count).IsEqualTo(2);

    [Test]
    [Arguments("b\na\n")]
    [Arguments("a\nB\n")]
    public async Task Parse_UnsortedIds_Fails(string text)
    {
        var ex = await Assert.That(() => KnownFailureBaseline.Parse(text)).Throws<BaselineFormatException>();

        await Assert.That(ex!.Message).Contains("line 2");
    }

    [Test]
    public async Task Parse_DuplicateIds_Fails()
    {
        var ex = await Assert.That(() => KnownFailureBaseline.Parse("a\nb\nb\n")).Throws<BaselineFormatException>();

        await Assert.That(ex!.Message).Contains("line 3");
    }

    [Test]
    public async Task Parse_BlankLine_Fails() =>
        await Assert.That(() => KnownFailureBaseline.Parse("a\n\nb\n")).Throws<BaselineFormatException>();

    [Test]
    public async Task Format_SameSetInAnyOrderWithDuplicates_IsByteIdentical()
    {
        var first = KnownFailureBaseline.Format(["b/2", "a/1", "b/1"]);
        var second = KnownFailureBaseline.Format(["b/1", "b/2", "a/1", "a/1"]);

        await Assert.That(first).IsEqualTo("a/1\nb/1\nb/2\n");
        await Assert.That(second).IsEqualTo(first);
    }

    [Test]
    public async Task Format_Output_RoundTripsThroughParse()
    {
        var text = KnownFailureBaseline.Format(["z", "Z", "a", "10", "9"]);

        var parsed = KnownFailureBaseline.Parse(text);

        await Assert.That(parsed.Count).IsEqualTo(5);
        await Assert.That(KnownFailureBaseline.Format(parsed.Ids)).IsEqualTo(text);
    }

    [Test]
    public async Task Save_WritesUtf8WithoutBomAndEmptyBaselineAsEmptyFile(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var temp = new TempDirectory();

        BaselineFiles.Save(temp.Path, "set", ["b", "a"]);
        BaselineFiles.Save(temp.Path, "empty", []);

        var bytes = await File.ReadAllBytesAsync(BaselineFiles.PathFor(temp.Path, "set"), cancellationToken);
        await Assert.That(bytes).IsEquivalentTo("a\nb\n"u8.ToArray());
        await Assert.That(new FileInfo(BaselineFiles.PathFor(temp.Path, "empty")).Length).IsEqualTo(0L);
    }

    [Test]
    public async Task Save_TwoTestSets_WriteTwoDifferentFiles(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var temp = new TempDirectory();

        BaselineFiles.Save(temp.Path, "set-a", ["a/1"]);
        BaselineFiles.Save(temp.Path, "set-b", ["b/1", "b/2"]);

        await Assert
            .That(BaselineFiles.PathFor(temp.Path, "set-a"))
            .IsNotEqualTo(BaselineFiles.PathFor(temp.Path, "set-b"));
        await Assert.That(BaselineFiles.Load(temp.Path, "set-a").Count).IsEqualTo(1);
        await Assert.That(BaselineFiles.Load(temp.Path, "set-b").Count).IsEqualTo(2);
        await Assert.That(Directory.GetFiles(temp.Path).Length).IsEqualTo(2);
    }

    [Test]
    public async Task Load_MissingFile_IsEmptyBaseline(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var temp = new TempDirectory();

        await Assert.That(BaselineFiles.Load(temp.Path, "none").Count).IsEqualTo(0);
    }

    [Test]
    public async Task PathFor_BlankArguments_Throw()
    {
        await Assert.That(() => BaselineFiles.PathFor(" ", "set")).Throws<ArgumentException>();
        await Assert.That(() => BaselineFiles.PathFor("dir", string.Empty)).Throws<ArgumentException>();
        await Assert.That(() => KnownFailureBaseline.Parse((string)null!)).Throws<ArgumentNullException>();
        await Assert.That(() => KnownFailureBaseline.Format(null!)).Throws<ArgumentNullException>();
    }
}
