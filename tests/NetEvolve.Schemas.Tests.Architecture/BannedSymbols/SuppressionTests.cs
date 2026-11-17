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
    public async Task Src_RS0030Suppression_IsLimitedToTheAnyKeyGuard(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var occurrences = new List<string>();
        foreach (
            var file in Directory.EnumerateFiles(
                Path.Combine(ProbeBuild.RepositoryRoot, "src"),
                "*.cs",
                SearchOption.AllDirectories
            )
        )
        {
            var text = await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false);
            var index = 0;
            while ((index = text.IndexOf("RS0030", index, StringComparison.Ordinal)) >= 0)
            {
                occurrences.Add(Path.GetFileName(file));
                index += 6;
            }
        }

        // One pragma disable and one restore, only around the explicit AnyKey check (decision recorded on #125).
        await Assert
            .That(occurrences)
            .IsEquivalentTo([
                "SchemaValidatorServiceCollectionExtensions.cs",
                "SchemaValidatorServiceCollectionExtensions.cs",
            ]);
    }
}
