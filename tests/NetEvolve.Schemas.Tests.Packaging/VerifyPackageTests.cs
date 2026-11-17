namespace NetEvolve.Schemas.Tests.Packaging;

using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Text;

public sealed class VerifyPackageTests
{
    private const string DependencyInjectionAbstractions = "Microsoft.Extensions.DependencyInjection.Abstractions";

    private static readonly string[] Folders = ["netstandard2.0", "netstandard2.1", "net10.0", "net11.0"];

    [Test]
    public async Task Verify_ValidPackage_Passes(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (exitCode, output) = await Run(Package(), null, cancellationToken);

        await Assert.That(exitCode).IsEqualTo(0);
        foreach (var folder in Folders)
        {
            await Assert.That(output).Contains($"lib/{folder}");
        }
    }

    [Test]
    public async Task Verify_ArgumentsDependency_Passes(CancellationToken cancellationToken) =>
        await Assert
            .That(await ExitCode(Package(dependencies: ["NetEvolve.Arguments"]), cancellationToken))
            .IsEqualTo(0);

    [Test]
    public async Task Verify_SiblingDependency_Passes(CancellationToken cancellationToken) =>
        await Assert.That(await ExitCode(Package(dependencies: ["NetEvolve.Schemas"]), cancellationToken)).IsEqualTo(0);

    [Test]
    public async Task Verify_DisallowedDependency_Fails(CancellationToken cancellationToken) =>
        await Assert.That(await ExitCode(Package(dependencies: ["Newtonsoft.Json"]), cancellationToken)).IsEqualTo(1);

    [Test]
    public async Task Verify_UngroupedDisallowedDependency_Fails(CancellationToken cancellationToken) =>
        await Assert
            .That(await ExitCode(Package(dependencies: ["Newtonsoft.Json"], grouped: false), cancellationToken))
            .IsEqualTo(1);

    [Test]
    public async Task Verify_UngroupedSiblingDependency_Passes(CancellationToken cancellationToken) =>
        await Assert
            .That(await ExitCode(Package(dependencies: ["NetEvolve.Schemas"], grouped: false), cancellationToken))
            .IsEqualTo(0);

    [Test]
    public async Task Verify_DependencyInjectionAbstractionsOnOtherPackage_Fails(CancellationToken cancellationToken) =>
        await Assert
            .That(await ExitCode(Package(dependencies: [DependencyInjectionAbstractions]), cancellationToken))
            .IsEqualTo(1);

    [Test]
    public async Task Verify_DependencyInjectionAbstractionsOnDependencyInjectionPackage_Passes(
        CancellationToken cancellationToken
    ) =>
        await Assert
            .That(
                await ExitCode(
                    Package(
                        id: "NetEvolve.Schemas.DependencyInjection",
                        dependencies: [DependencyInjectionAbstractions]
                    ),
                    cancellationToken
                )
            )
            .IsEqualTo(0);

    [Test]
    public async Task Verify_PolyfillOnNetStandard20_Passes(CancellationToken cancellationToken) =>
        await Assert
            .That(
                await ExitCode(
                    Package(dependencies: ["System.Memory"], dependencyGroup: ".NETStandard2.0"),
                    cancellationToken
                )
            )
            .IsEqualTo(0);

    [Test]
    public async Task Verify_PolyfillOnOtherFramework_Fails(CancellationToken cancellationToken) =>
        await Assert
            .That(
                await ExitCode(Package(dependencies: ["System.Memory"], dependencyGroup: "net10.0"), cancellationToken)
            )
            .IsEqualTo(1);

    [Test]
    public async Task Verify_MissingFramework_Fails(CancellationToken cancellationToken) =>
        await Assert
            .That(await ExitCode(Package(folders: ["netstandard2.0", "net10.0"]), cancellationToken))
            .IsEqualTo(1);

    [Test]
    public async Task Verify_MissingReadme_Fails(CancellationToken cancellationToken) =>
        await Assert.That(await ExitCode(Package(readme: false), cancellationToken)).IsEqualTo(1);

    [Test]
    public async Task Verify_MissingReadmeAllowed_ReportsKnownGap(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (exitCode, output) = await Run(Package(readme: false), "-AllowMissingReadme", cancellationToken);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(output).Contains("KNOWN GAP");
    }

    [Test]
    public async Task Verify_NonMitLicense_Fails(CancellationToken cancellationToken) =>
        await Assert.That(await ExitCode(Package(license: "Apache-2.0"), cancellationToken)).IsEqualTo(1);

    [Test]
    public async Task Verify_LicenseAcceptanceRequired_Fails(CancellationToken cancellationToken) =>
        await Assert.That(await ExitCode(Package(requireAcceptance: true), cancellationToken)).IsEqualTo(1);

    [Test]
    [Arguments("content/suites/case.xml")]
    [Arguments("content/conformance/case.xml")]
    [Arguments("content/rules.sch")]
    public async Task Verify_SuiteData_Fails(string extraFile, CancellationToken cancellationToken) =>
        await Assert.That(await ExitCode(Package(extraFiles: [extraFile]), cancellationToken)).IsEqualTo(1);

    private static string Package(
        string id = "NetEvolve.Schemas.Fixture",
        string[]? dependencies = null,
        string dependencyGroup = "net10.0",
        string[]? folders = null,
        bool readme = true,
        string license = "MIT",
        bool requireAcceptance = false,
        string[]? extraFiles = null,
        bool grouped = true
    )
    {
        var directory = Directory.CreateTempSubdirectory("verify-package-").FullName;
        var nuspec = new StringBuilder()
            .Append("<package xmlns=\"http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd\"><metadata>")
            .Append(CultureInfo.InvariantCulture, $"<id>{id}</id><version>1.0.0</version>")
            .Append(CultureInfo.InvariantCulture, $"<license type=\"expression\">{license}</license>")
            .Append(
                CultureInfo.InvariantCulture,
                $"<requireLicenseAcceptance>{(requireAcceptance ? "true" : "false")}</requireLicenseAcceptance>"
            );
        if (readme)
        {
            _ = nuspec.Append("<readme>README.md</readme>");
        }

        _ = nuspec.Append("<dependencies>");
        if (grouped)
        {
            _ = nuspec.Append(CultureInfo.InvariantCulture, $"<group targetFramework=\"{dependencyGroup}\">");
        }
        foreach (var dependency in dependencies ?? [])
        {
            _ = nuspec.Append(CultureInfo.InvariantCulture, $"<dependency id=\"{dependency}\" version=\"1.0.0\" />");
        }

        if (grouped)
        {
            _ = nuspec.Append("</group>");
        }

        _ = nuspec.Append("</dependencies></metadata></package>");

        using (var stream = File.Create(Path.Combine(directory, $"{id}.1.0.0.nupkg")))
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
        {
            Add(zip, $"{id}.nuspec", nuspec.ToString());
            if (readme)
            {
                Add(zip, "README.md", "readme");
            }

            foreach (var folder in folders ?? Folders)
            {
                Add(zip, $"lib/{folder}/{id}.dll", "dll");
            }

            foreach (var file in extraFiles ?? [])
            {
                Add(zip, file, "data");
            }
        }

        return directory;
    }

    private static void Add(ZipArchive zip, string path, string content)
    {
        using var writer = new StreamWriter(zip.CreateEntry(path).Open());
        writer.Write(content);
    }

    private static async Task<int> ExitCode(string directory, CancellationToken cancellationToken) =>
        (await Run(directory, null, cancellationToken)).ExitCode;

    private static async Task<(int ExitCode, string Output)> Run(
        string directory,
        string? extraArgument,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var info = new ProcessStartInfo("pwsh")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var argument in new[] { "-NoProfile", "-File", FindScript(), "-PackageDirectory", directory })
        {
            info.ArgumentList.Add(argument);
        }

        if (extraArgument is not null)
        {
            info.ArgumentList.Add(extraArgument);
        }

        using var process = Process.Start(info)!;
        var output = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        return (process.ExitCode, await output + await error);
    }

    private static string FindScript()
    {
        for (
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            directory is not null;
            directory = directory.Parent
        )
        {
            var candidate = Path.Combine(directory.FullName, "scripts", "verify-package.ps1");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException("scripts/verify-package.ps1 not found above the test output directory.");
    }
}
