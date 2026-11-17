namespace NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;

using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;

internal sealed class FakeModelResolver : IModelResolver
{
    internal int Calls { get; private set; }

    public bool TryResolve(ParsedModule root, IModuleLoader loader, IProblemSink problems, out ResolvedModel model)
    {
        Calls++;
        model = new ResolvedModel();
        return true;
    }
}
