namespace NetEvolve.Schemas.Schematron.Seams;

/// <summary>A compiled XSLT instruction.</summary>
internal abstract class XsltExecutable
{
    /// <summary>Executes the instruction; the problem is read with <see cref="XsltFrame.TakeProblem"/>, which falls back to <see cref="IQueryRun.GetProblem"/>.</summary>
    internal abstract QueryStatus TryExecute(XsltFrame frame, ISequenceSink sink);
}
