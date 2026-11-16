namespace NetEvolve.Schemas.Tests.Architecture.BannedSymbols;

public class SuppressionTests
{
    [Test]
    public async Task EditorConfig_HasNoRS0030Override(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var text = await File.ReadAllTextAsync(
                Path.Combine(ProbeBuild.RepositoryRoot, ".editorconfig"),
                cancellationToken
            )
            .ConfigureAwait(false);

        await Assert.That(text.Contains("RS0030", StringComparison.Ordinal)).IsFalse();
    }

    [Test]
    public async Task Src_ContainsNoRS0030PragmaSuppression(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var offenders = new List<string>();
        foreach (
            var file in Directory.EnumerateFiles(
                Path.Combine(ProbeBuild.RepositoryRoot, "src"),
                "*.cs",
                SearchOption.AllDirectories
            )
        )
        {
            var text = await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false);
            if (text.Contains("RS0030", StringComparison.Ordinal))
            {
                offenders.Add(file);
            }
        }

        await Assert.That(offenders).IsEmpty();
    }
}
