namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Strings;

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xml.Text.Unicode;

/// <summary>
/// The committed case mapping and normalization tables are the output of <see cref="StringTablesGenerator"/> for the pinned Unicode
/// Character Database, which is the version of the tables of the Xml package.
/// </summary>
public class StringTablesTests
{
    [Test]
    public async Task CaseMappingTables_AreReproducibleFromThePinnedFiles(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ucd = await StringsUcd.GetAsync(cancellationToken);
        var generated = StringTablesGenerator.GenerateCaseMapping(
            ucd.Version,
            await ucd.ReadLinesAsync("UnicodeData.txt", cancellationToken),
            await ucd.ReadLinesAsync("SpecialCasing.txt", cancellationToken)
        );

        await AssertCommitted(StringTablesGenerator.CaseMappingPath(), generated, cancellationToken);
    }

    [Test]
    public async Task NormalizationTables_AreReproducibleFromThePinnedFiles(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ucd = await StringsUcd.GetAsync(cancellationToken);
        var generated = StringTablesGenerator.GenerateNormalization(
            ucd.Version,
            await ucd.ReadLinesAsync("UnicodeData.txt", cancellationToken),
            await ucd.ReadLinesAsync("DerivedNormalizationProps.txt", cancellationToken)
        );

        await AssertCommitted(StringTablesGenerator.NormalizationPath(), generated, cancellationToken);
    }

    [Test]
    public async Task PinnedVersion_IsTheVersionOfTheTablesOfTheXmlPackage(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ucd = await StringsUcd.GetAsync(cancellationToken);

        await Assert.That(ucd.Version).IsEqualTo(UnicodeTables.Version);
    }

    private static async Task AssertCommitted(string path, string generated, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Environment.GetEnvironmentVariable(StringTablesGenerator.RegenerateVariable) is { Length: > 0 })
        {
            await File.WriteAllTextAsync(
                path,
                generated,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                cancellationToken
            );
        }

        var committed = (await File.ReadAllTextAsync(path, cancellationToken)).Replace(
            "\r\n",
            "\n",
            StringComparison.Ordinal
        );

        await Assert.That(committed).IsEqualTo(generated);
    }
}
