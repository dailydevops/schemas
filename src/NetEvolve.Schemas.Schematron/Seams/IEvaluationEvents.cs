namespace NetEvolve.Schemas.Schematron.Seams;

/// <summary>The events the evaluator emits; the issue mapper and the optional caller sink are composed behind it.</summary>
internal interface IEvaluationEvents : ISchematronReportSink
{
    /// <summary>Gets a value indicating whether a location path is built; <see langword="false"/> when no caller sink is attached.</summary>
    bool WantsLocationPath { get; }
}
