namespace NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;

using System.Threading;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>The fake loader method: counts calls and records what it was given.</summary>
internal sealed class FakePipeline : ISchematronPipeline
{
    internal int LoadCount { get; private set; }

    internal SchemaSource? LastSource { get; private set; }

    internal BuildSettings? LastSettings { get; private set; }

    public CompiledSchema Load(SchemaSource source, BuildSettings settings, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LoadCount++;
        LastSource = source;
        LastSettings = settings;

        return FakeModel.Compiled();
    }
}
