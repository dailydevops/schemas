namespace NetEvolve.Schemas.Schematron.Seams;

using System.Threading;
using NetEvolve.Schemas.Schematron.Diagnostics;

/// <summary>The default pipeline until the real composition lands: every load fails with <see cref="SchematronDiagnosticCodes.NotImplemented"/>.</summary>
internal sealed class NotImplementedPipeline : ISchematronPipeline
{
    internal static NotImplementedPipeline Instance { get; } = new();

    public CompiledSchema Load(SchemaSource source, BuildSettings settings, CancellationToken cancellationToken) =>
        throw NotImplementedFailure.For("pipeline");
}
