namespace NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;

using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;

internal sealed class FakeModelExpander : IModelExpander
{
    internal int Calls { get; private set; }

    public bool TryExpand(
        ResolvedModel model,
        BuildSettings settings,
        IProblemSink problems,
        out SchematronSchema effective
    )
    {
        Calls++;
        effective = FakeModel.Schema();
        return true;
    }
}
