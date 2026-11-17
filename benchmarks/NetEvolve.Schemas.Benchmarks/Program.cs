using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using NetEvolve.Schemas.Benchmarks.Gate;

// Usage: `gate <report.json> [targetsDirectory]` gates a report; anything else runs BenchmarkDotNet.
if (args is ["gate", var report, ..])
{
    return BenchmarkGate.Run(report, args.Length > 2 ? args[2] : Path.Combine("benchmarks", "targets"), Console.Out);
}

// The generated benchmark project inherits the repository's warnings-as-errors; its code is not ours to document.
var job = Job.Default.WithArguments([
    new MsBuildArgument("/p:TreatWarningsAsErrors=false"),
    new MsBuildArgument("/p:GenerateDocumentationFile=false"),
]);

// BenchmarkDotNet 0.15.8 does not recognize net11.0 and rejects the out-of-process toolchain there; run in-process on newer runtimes.
if (Environment.Version.Major > 10)
{
    job = job.WithToolchain(InProcessNoEmitToolchain.Instance);
}

var config = DefaultConfig.Instance.AddJob(job).AddDiagnoser(MemoryDiagnoser.Default).AddExporter(JsonExporter.Full);
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
return 0;
