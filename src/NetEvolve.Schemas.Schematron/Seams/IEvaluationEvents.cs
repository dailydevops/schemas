namespace NetEvolve.Schemas.Schematron.Seams;

using NetEvolve.Schemas.Formats;

/// <summary>The events the evaluator emits; the issue mapper and the optional caller sink are composed behind it.</summary>
internal interface IEvaluationEvents : ISchematronReportSink
{
    /// <summary>Gets a value indicating whether a location path is built; <see langword="false"/> when no caller sink is attached.</summary>
    bool WantsLocationPath { get; }

    /// <summary>Gets a value indicating whether an error was dropped by the issue limit; evaluation stops there.</summary>
    bool HasDroppedError { get; }

    /// <summary>Maps one fired assertion to an issue and adds it to the collector.</summary>
    /// <param name="isReport"><see langword="true"/> for a successful report, <see langword="false"/> for a failed assert.</param>
    /// <param name="constraint">The assertion identifier.</param>
    /// <param name="message">The rendered message.</param>
    /// <param name="location">The instance location of the reported node.</param>
    /// <returns>The issue, or <see langword="null"/> when the collector dropped it.</returns>
    ValidationIssue? Map(bool isReport, string constraint, string message, DocumentLocation location);
}
