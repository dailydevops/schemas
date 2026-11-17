namespace NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;

using System.Collections.Generic;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;

/// <summary>A compiler that returns a configured model and reports the position ceiling when the model exceeds it.</summary>
internal sealed class FakeContentModelCompiler : IContentModelCompiler
{
    private readonly FakeContentModel _model;
    private readonly int _positionCeiling;
    private readonly List<string> _log;

    public FakeContentModelCompiler(FakeContentModel model, int positionCeiling, List<string> log)
    {
        _model = model;
        _positionCeiling = positionCeiling;
        _log = log;
    }

    public ICompiledContentModel Compile(IXsdComponent group, XsdIssueReporter reporter)
    {
        _log.Add($"Compile:{group.Name.LocalName}");
        if (_model.PositionCount > _positionCeiling)
        {
            reporter.Report(
                FakeRules.Find(XsdRuleCodes.ContentModelPositionCeilingExceeded),
                group.Location,
                "The content model has too many positions."
            );
        }

        return _model;
    }
}
