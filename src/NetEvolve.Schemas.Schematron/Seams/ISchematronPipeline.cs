namespace NetEvolve.Schemas.Schematron.Seams;

using System.Threading;

/// <summary>The one loader method all schema source overloads reach.</summary>
internal interface ISchematronPipeline
{
    /// <summary>Loads and compiles a schema.</summary>
    /// <exception cref="SchemaCompilationException">The schema has load or compile problems.</exception>
    CompiledSchema Load(SchemaSource source, BuildSettings settings, CancellationToken cancellationToken);
}
