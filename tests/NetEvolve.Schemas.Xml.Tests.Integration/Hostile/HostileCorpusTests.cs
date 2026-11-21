namespace NetEvolve.Schemas.Xml.Tests.Integration.Hostile;

using NetEvolve.Schemas.Xml.Contracts;

/// <summary>
/// Hostile input corpus (XML-21): every file ends in a coded diagnostic within a time bound, on every input kind, and no
/// file reaches the resolver under the default options.
/// </summary>
public class HostileCorpusTests
{
    private static readonly HostileInputKind[] Kinds = Enum.GetValues<HostileInputKind>();

    private static bool UsesDoctype(HostileCase hostile) =>
        hostile.Category
            is HostileCategory.GeneralEntityBomb
                or HostileCategory.ParameterEntityBomb
                or HostileCategory.ExternalAccess;

    [Test]
    public async Task Corpus_CoversEveryCategoryAndNamesEveryFileOnce()
    {
        var files = HostileCorpus.All();

        foreach (var category in Enum.GetValues<HostileCategory>())
        {
            await Assert.That(files.Count(file => file.Category == category)).IsGreaterThanOrEqualTo(3);
        }

        await Assert
            .That(files.Select(file => file.Name).Distinct(StringComparer.Ordinal).Count())
            .IsEqualTo(files.Count);
    }

    [Test]
    [MethodDataSource(typeof(HostileCorpus), nameof(HostileCorpus.Names))]
    public async Task File_EndsInTheExpectedCodeWithinTheTimeBound_OnEveryInputKind(
        string name,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var hostile = HostileCorpus.Get(name);

        var xml = hostile.Document();
        var mismatches = new List<string>();
        foreach (var kind in Kinds)
        {
            var resolver = new HostileResolver(hostile.Resources);
            var outcome = await HostileInputs.LoadAsync(
                kind,
                xml,
                HostileInputs.OptionsOf(hostile, resolver),
                HostileCorpus.TimeBound,
                cancellationToken
            );

            if (outcome.TimedOut)
            {
                mismatches.Add($"{kind}: no end within {HostileCorpus.TimeBound}");
            }
            else if (!ReferenceEquals(outcome.Code, hostile.Expected))
            {
                mismatches.Add($"{kind}: {outcome.Code?.Alias ?? "no diagnostic"} instead of {hostile.Expected.Alias}");
            }
        }

        await Assert.That(mismatches).IsEmpty();
    }

    [Test]
    [MethodDataSource(typeof(HostileCorpus), nameof(HostileCorpus.Names))]
    public async Task File_WithExternalAccessOff_MakesNoResolverRequest_OnEveryInputKind(
        string name,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var hostile = HostileCorpus.Get(name);
        if (hostile.Category != HostileCategory.ExternalAccess)
        {
            return;
        }

        var xml = hostile.Document();
        var requests = new List<string>();
        foreach (var kind in Kinds)
        {
            var resolver = new HostileResolver(hostile.Resources);
            _ = await HostileInputs.LoadAsync(
                kind,
                xml,
                HostileInputs.OptionsOf(hostile, resolver),
                HostileCorpus.TimeBound,
                cancellationToken
            );
            requests.AddRange(resolver.Requests);
        }

        await Assert.That(requests).IsEmpty();
    }

    [Test]
    [MethodDataSource(typeof(HostileCorpus), nameof(HostileCorpus.Names))]
    public async Task File_WithADocumentTypeDeclaration_IsRejectedByTheLibraryDefaultOptions(
        string name,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var hostile = HostileCorpus.Get(name);
        if (!UsesDoctype(hostile))
        {
            return;
        }

        var xml = hostile.Document();
        var mismatches = new List<string>();
        foreach (var kind in Kinds)
        {
            var outcome = await HostileInputs.LoadAsync(
                kind,
                xml,
                XmlReaderOptions.Default,
                HostileCorpus.TimeBound,
                cancellationToken
            );

            if (!ReferenceEquals(outcome.Code, XmlErrorCodes.XML2001))
            {
                mismatches.Add($"{kind}: {outcome.Code?.Alias ?? "no diagnostic"}");
            }
        }

        await Assert.That(mismatches).IsEmpty();
    }

    [Test]
    public async Task Resolver_WithEveryAccessFlagOn_RecordsTheRequestsOfAFile(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var hostile = HostileCorpus.Get("parameter-external-self-reference");
        var resolver = new HostileResolver(hostile.Resources);
        _ = await HostileInputs.LoadAsync(
            HostileInputKind.ByteSpan,
            hostile.Document(),
            HostileInputs.OptionsOf(hostile, resolver),
            HostileCorpus.TimeBound,
            cancellationToken
        );

        await Assert.That(hostile.Access).IsEqualTo(HostileAccess.Allowed);
        await Assert.That(resolver.Requests).IsNotEmpty();
    }
}
