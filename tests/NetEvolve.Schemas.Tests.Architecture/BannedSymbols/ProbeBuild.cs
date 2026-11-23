namespace NetEvolve.Schemas.Tests.Architecture.BannedSymbols;

using System.Diagnostics;
using System.Text;

/// <summary>
/// Compiles a single source file inside a scratch copy of the repository build configuration, so the real
/// <c>.editorconfig</c>, <c>BannedSymbols.txt</c> and Directory.Build.props decide the diagnostics.
/// </summary>
internal static class ProbeBuild
{
    private static readonly string[] _configFiles =
    [
        ".editorconfig",
        "Directory.Build.props",
        "Directory.Packages.props",
        "global.json",
        "nuget.config",
        "src/Directory.Build.props",
        "src/BannedSymbols.txt",
    ];

    internal static string RepositoryRoot { get; } = FindRepositoryRoot();

    /// <summary>Builds <paramref name="source"/> as <c>src/Probe/<paramref name="relativeFile"/></c> in Release.</summary>
    internal static async Task<(int ExitCode, string Output)> BuildAsync(
        string relativeFile,
        string source,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = Path.Combine(Path.GetTempPath(), "schemas-probe-" + Guid.NewGuid().ToString("N"));
        try
        {
            var project = await CreateScratchRootAsync(root, relativeFile, source, cancellationToken)
                .ConfigureAwait(false);

            return await RunBuildAsync(root, project, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            try
            {
                Directory.Delete(root, recursive: true);
            }
            catch (IOException)
            {
                // Best effort: a lingering scratch folder in the temp directory is harmless.
            }
        }
    }

    private static async Task<string> CreateScratchRootAsync(
        string root,
        string relativeFile,
        string source,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var file in _configFiles)
        {
            var target = Path.Combine(root, file);
            _ = Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(Path.Combine(RepositoryRoot, file), target);
        }

        var probeDirectory = Path.Combine(root, "src", "Probe");
        var sourceFile = Path.Combine(probeDirectory, relativeFile);
        _ = Directory.CreateDirectory(Path.GetDirectoryName(sourceFile)!);

        var folder = Path.GetDirectoryName(relativeFile)
            ?.Replace(Path.DirectorySeparatorChar, '.')
            .Replace(Path.AltDirectorySeparatorChar, '.');
        var ns = string.IsNullOrEmpty(folder) ? "Probe" : "Probe." + folder;
        await File.WriteAllTextAsync(sourceFile, $"namespace {ns};\n{source}\n", Encoding.UTF8, cancellationToken)
            .ConfigureAwait(false);

        var project = Path.Combine(probeDirectory, "Probe.csproj");
        await File.WriteAllTextAsync(project, "<Project Sdk=\"Microsoft.NET.Sdk\" />", cancellationToken)
            .ConfigureAwait(false);

        return project;
    }

    private static async Task<(int ExitCode, string Output)> RunBuildAsync(
        string root,
        string project,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var process = Process.Start(
            new ProcessStartInfo("dotnet")
            {
                ArgumentList =
                {
                    "build",
                    project,
                    "-c",
                    "Release",
                    "-f",
                    "net10.0",
                    "-nologo",
                    "-nodeReuse:false",
                    "-p:UseSharedCompilation=false",
                    "-p:CSharpier_Bypass=true",
                },
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = root,
                Environment = { ["DOTNET_CLI_UI_LANGUAGE"] = "en", ["VSLANG"] = "1033" },
            }
        )!;
        var stdout = process.StandardOutput.ReadToEndAsync(
#if NET7_0_OR_GREATER
            cancellationToken
#endif
        );
        var stderr = process.StandardError.ReadToEndAsync(
#if NET7_0_OR_GREATER
            cancellationToken
#endif
        );
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        return (process.ExitCode, await stdout.ConfigureAwait(false) + await stderr.ConfigureAwait(false));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Schemas.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
