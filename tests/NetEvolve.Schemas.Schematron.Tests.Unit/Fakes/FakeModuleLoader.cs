namespace NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;

using System.Collections.Generic;
using System.Threading;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>A loader that records its references and reports a problem for a reference to <c>missing.sch</c>.</summary>
internal sealed class FakeModuleLoader : IModuleLoader
{
    internal List<string> Loaded { get; } = [];

    public bool TryLoad(
        in ModuleReference reference,
        CancellationToken cancellationToken,
        out LoadedModule module,
        out SchematronProblem problem
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        Loaded.Add(reference.Href);
        module = default;
        problem = default;
        if (string.Equals(reference.Href, "missing.sch", StringComparison.Ordinal))
        {
            problem = new SchematronProblem(CoreDiagnosticCodes.ResourceNotFound, "not found", reference.Location);
            return false;
        }

        module = new LoadedModule(default(XPathNode), reference.Href);
        return true;
    }
}
