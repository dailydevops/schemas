namespace NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;

using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Seams;

internal sealed class FakeProblemSink : IProblemSink
{
    internal List<SchematronProblem> Problems { get; } = [];

    public void Add(in SchematronProblem problem) => Problems.Add(problem);
}
