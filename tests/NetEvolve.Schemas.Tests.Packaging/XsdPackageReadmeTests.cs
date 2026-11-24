namespace NetEvolve.Schemas.Tests.Packaging;

using System.Diagnostics;
using System.Text.Json;

// NetEvolve.Defaults packs the README.md of the project directory and names it as the readme of the package. Without one it
// falls back to the README.md of the solution, so the test checks which file is packed, not only that a readme is.
public sealed class XsdPackageReadmeTests
{
    [Test]
    public async Task XsdPackage_PacksTheReadmeOfThePackageAndNamesItInTheNuspec(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = FindRoot();
        var readme = Path.GetFullPath(Path.Combine(root, "src", "NetEvolve.Schemas.Xsd", "README.md"));

        using var evaluation = JsonDocument.Parse(await Evaluate(root, cancellationToken));
        var packedReadmes = new List<string>();
        foreach (var item in evaluation.RootElement.GetProperty("Items").GetProperty("None").EnumerateArray())
        {
            if (
                string.Equals(item.GetProperty("Pack").GetString(), "true", StringComparison.Ordinal)
                && string.Equals(
                    item.GetProperty("Filename").GetString() + item.GetProperty("Extension").GetString(),
                    "README.md",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                await Assert.That(item.GetProperty("PackagePath").GetString()).IsEqualTo(string.Empty);
                packedReadmes.Add(Path.GetFullPath(item.GetProperty("FullPath").GetString()!));
            }
        }

        await Assert
            .That(evaluation.RootElement.GetProperty("Properties").GetProperty("PackageReadmeFile").GetString())
            .IsEqualTo("README.md");
        await Assert.That(File.Exists(readme)).IsTrue();
        await Assert.That(packedReadmes).IsEquivalentTo([readme]);
    }

    private static async Task<string> Evaluate(string root, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var info = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (
            var argument in new[]
            {
                "msbuild",
                Path.Combine(root, "src", "NetEvolve.Schemas.Xsd", "NetEvolve.Schemas.Xsd.csproj"),
                "-restore",
                "-getProperty:PackageReadmeFile",
                "-getItem:None",
                // The pack workflow passes the solution directory, so the fallback readme is the one it would pack.
                $"-p:SolutionDir={root}{Path.DirectorySeparatorChar}",
            }
        )
        {
            info.ArgumentList.Add(argument);
        }

        using var process = Process.Start(info)!;
        var output = process.StandardOutput.ReadToEndAsync(
#if NET7_0_OR_GREATER
            cancellationToken
#endif
        );
        var error = process.StandardError.ReadToEndAsync(
#if NET7_0_OR_GREATER
            cancellationToken
#endif
        );
        await process.WaitForExitAsync(cancellationToken);
        var text = await output;

        return process.ExitCode == 0
            ? text[text.IndexOf('{', StringComparison.Ordinal)..]
            : throw new InvalidOperationException(
                $"dotnet msbuild failed with {process.ExitCode}: {text}{await error}"
            );
    }

    private static string FindRoot()
    {
        for (
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            directory is not null;
            directory = directory.Parent
        )
        {
            if (File.Exists(Path.Combine(directory.FullName, "Schemas.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new FileNotFoundException("Schemas.slnx not found above the test output directory.");
    }
}
