namespace NetEvolve.Schemas.Schematron.Seams;

using NetEvolve.Arguments;

/// <summary>The execution frame of an XSLT instruction. The slots for current group, regex group, and mode are added by the evaluation package.</summary>
internal sealed class XsltFrame
{
    internal XsltFrame(IQueryRun run)
    {
        Argument.ThrowIfNull(run);

        Run = run;
    }

    internal IQueryRun Run { get; }
}
