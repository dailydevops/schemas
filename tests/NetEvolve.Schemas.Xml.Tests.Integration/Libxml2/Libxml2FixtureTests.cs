namespace NetEvolve.Schemas.Xml.Tests.Integration.Libxml2;

using NetEvolve.Schemas.Conformance.Results;

/// <summary>The readers and runners of the libxml2 sub-trees, checked with inline data so no download is needed.</summary>
public class Libxml2FixtureTests
{
    [Test]
    public async Task RegexCatalog_ReadsPatternsAndInputsInDriverOrder(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var cases = Libxml2RegexCatalog.Parse(
            "f",
            "# comment\n=>a|b\na\r\nab  \n\n=>c\nc\n",
            "Regexp: a|b\na: Ok\nab: Fail\nRegexp: c\nc: Ok\n"
        );

        await Assert
            .That(cases.Select(c => (c.Id, c.Pattern, c.Input, c.Expected)))
            .IsEquivalentTo([
                ("f/1", "a|b", null, true),
                ("f/1/1", "a|b", "a", true),
                ("f/1/2", "a|b", "ab", false),
                ("f/2", "c", null, true),
                ("f/2/1", "c", "c", true),
            ]);
    }

    [Test]
    public async Task RegexCatalog_PatternWithoutArrow_IsTheFirstPatternOfTheFile(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var cases = Libxml2RegexCatalog.Parse("f", "a+\naa\n", "Regexp: a+\naa: Ok\n");

        await Assert.That(cases.Select(c => c.Id)).IsEquivalentTo(["f/1", "f/1/1"]);
    }

    [Test]
    public async Task RegexCatalog_PatternThatDoesNotCompile_EndsTheFile(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var cases = Libxml2RegexCatalog.Parse("f", "=>[\nx\n=>a\n", "Regexp: [\n   failed to compile\n");

        await Assert.That(cases.Count).IsEqualTo(1);
        await Assert.That(cases[0].Expected).IsFalse();
    }

    [Test]
    [Arguments("=>a\nb\n", "Regexp: a\n")]
    [Arguments("=>a\nb\n", "Regexp: a\nb: Ok\nRegexp: c\n")]
    [Arguments("=>a\n=>b\n", "Regexp: a\n")]
    public async Task RegexCatalog_ResultThatDoesNotMatchTheTestFile_Throws(
        string test,
        string result,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(() => Libxml2RegexCatalog.Parse("f", test, result)).Throws<InvalidDataException>();
    }

    [Test]
    public async Task RegexRunner_JudgesCompileAndMatchCases_AndSkipsInputsOfABrokenPattern(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var cases = new List<Libxml2RegexCase>
        {
            new("f/1", "f", "a|b", null, true),
            new("f/1/1", "f", "a|b", "a", true),
            new("f/1/2", "f", "a|b", "ab", false),
            new("f/1/3", "f", "a|b", "ab", true),
            new("f/2", "f", "[", null, false),
            new("f/3", "f", "(", null, true),
            new("f/3/1", "f", "(", "x", true),
        };

        var results = Libxml2RegexRunner.Run(cases, CancellationToken.None);

        await Assert
            .That(results.Select(r => r.Outcome))
            .IsEquivalentTo([
                CaseOutcome.Pass,
                CaseOutcome.Pass,
                CaseOutcome.Pass,
                CaseOutcome.Fail,
                CaseOutcome.Pass,
                CaseOutcome.Fail,
                CaseOutcome.Skipped,
            ]);
    }

    [Test]
    [Arguments("./test/errors/a.xml:2: parser error : x\n", true)]
    [Arguments("./test/errors/a.xml:2: namespace error : x\n", true)]
    [Arguments("./test/errors/a.xml:2: I/O error : x\n", true)]
    [Arguments("./test/errors/a.xml:2: parser warning : x\n./test/errors/a.xml:2: I/O warning : y\n", false)]
    [Arguments("", false)]
    public async Task ErrorCatalog_MustReject_FollowsTheMessageKind(
        string messages,
        bool expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Libxml2ErrorCatalog.MustReject(messages)).IsEqualTo(expected);
    }

    [Test]
    public async Task ErrorRunner_RejectsMalformedDocuments_AndAcceptsWellFormedOnes(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Libxml2ErrorRunner.Rejects("<a><b></a>"u8.ToArray(), "file:///x.xml")).IsTrue();
        await Assert.That(Libxml2ErrorRunner.Rejects("<a><b/></a>"u8.ToArray(), "file:///x.xml")).IsFalse();
    }

    [Test]
    public async Task ErrorCatalog_AndRunner_ReadAFolderLikeTheArchive(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = Path.Combine(Path.GetTempPath(), "libxml2-fixture-" + Guid.NewGuid().ToString("N"));
        try
        {
            _ = Directory.CreateDirectory(Path.Combine(root, "test", "errors"));
            _ = Directory.CreateDirectory(Path.Combine(root, "result", "errors"));
            await File.WriteAllTextAsync(Path.Combine(root, "test", "errors", "bad.xml"), "<a>", cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(root, "test", "errors", "good.xml"), "<a/>", cancellationToken);
            await File.WriteAllTextAsync(
                Path.Combine(root, "result", "errors", "bad.xml.err"),
                "bad.xml:1: parser error : x\n",
                cancellationToken
            );

            var cases = Libxml2ErrorCatalog.Read(root);
            var results = await Libxml2ErrorRunner.RunAsync(cases, cancellationToken);

            await Assert
                .That(cases.Select(c => (c.Id, c.MustReject)))
                .IsEquivalentTo([("bad.xml", true), ("good.xml", false)]);
            await Assert.That(results.All(r => r.Outcome == CaseOutcome.Pass)).IsTrue();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task RegexCatalog_ReadsAFolderLikeTheArchive(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = Path.Combine(Path.GetTempPath(), "libxml2-fixture-" + Guid.NewGuid().ToString("N"));
        try
        {
            _ = Directory.CreateDirectory(Path.Combine(root, "test", "regexp"));
            _ = Directory.CreateDirectory(Path.Combine(root, "result", "regexp"));
            await File.WriteAllTextAsync(Path.Combine(root, "test", "regexp", "b"), "=>b\nb\n", cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(root, "test", "regexp", "a"), "=>a\na\n", cancellationToken);
            await File.WriteAllTextAsync(
                Path.Combine(root, "result", "regexp", "b"),
                "Regexp: b\nb: Ok\n",
                cancellationToken
            );
            await File.WriteAllTextAsync(
                Path.Combine(root, "result", "regexp", "a"),
                "Regexp: a\na: Ok\n",
                cancellationToken
            );

            var cases = Libxml2RegexCatalog.Read(root);

            await Assert.That(cases.Select(c => c.Id)).IsEquivalentTo(["a/1", "a/1/1", "b/1", "b/1/1"]);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
