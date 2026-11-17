namespace NetEvolve.Schemas.Schematron.Seams;

using System.Threading;

/// <summary>Loads included modules through the core resolver. One instance per build, not thread-safe: it counts resources.</summary>
internal interface IModuleLoader
{
    bool TryLoad(
        in ModuleReference reference,
        CancellationToken cancellationToken,
        out LoadedModule module,
        out SchematronProblem problem
    );
}
