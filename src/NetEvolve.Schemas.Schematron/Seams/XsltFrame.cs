namespace NetEvolve.Schemas.Schematron.Seams;

using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>
/// The execution frame of an XSLT instruction: the run, the focus that the iteration instructions set (design #150 EP-13), the
/// token that every iteration checks (EP-16), the diagnostic channel of <c>xsl:message</c>, and the problem channel of the
/// instructions themselves. The slots for current group, regex group, and mode are added by the evaluation package. Not thread-safe:
/// one frame per run.
/// </summary>
internal sealed class XsltFrame
{
    /// <summary>The default limit of the items that one <c>xsl:for-each</c> holds (design #150 EP-16).</summary>
    internal const int DefaultMaxSequenceLength = 1_000_000;

    private SchematronProblem _problem;
    private bool _hasProblem;

    internal XsltFrame(IQueryRun run, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(run);

        Run = run;
        CancellationToken = cancellationToken;
    }

    internal IQueryRun Run { get; }

    /// <summary>Gets the token that an iteration checks before it starts.</summary>
    internal CancellationToken CancellationToken { get; }

    /// <summary>Gets or sets the focus; the default focus has no item. An instruction that sets the focus restores the one it found.</summary>
    internal QueryFocus Focus { get; set; }

    /// <summary>Gets or sets the diagnostic channel of <c>xsl:message</c>; <see langword="null"/> drops the messages.</summary>
    internal IXsltMessageSink? Messages { get; set; }

    /// <summary>Gets or sets the most items that one <c>xsl:for-each</c> holds; a larger sequence is the coded limit error.</summary>
    internal int MaxSequenceLength { get; set; } = DefaultMaxSequenceLength;

    /// <summary>Gets or sets the most characters of one <c>xsl:message</c>; a longer message is the coded limit error.</summary>
    internal int MaxMessageLength { get; set; } = XmlReaderLimits.Default.MaxTextNodeLength;

    /// <summary>Records a problem that an instruction found itself, as opposed to one the run found while evaluating.</summary>
    /// <param name="problem">The problem.</param>
    /// <returns><see cref="QueryStatus.Problem"/>, for the caller to return.</returns>
    internal QueryStatus Fail(in SchematronProblem problem)
    {
        _problem = problem;
        _hasProblem = true;
        return QueryStatus.Problem;
    }

    /// <summary>
    /// Takes the problem of the last failure: the one the frame recorded with <see cref="Fail"/>, which is then forgotten, or else the
    /// one of the run. Read it once after an instruction returned <see cref="QueryStatus.Problem"/>.
    /// </summary>
    /// <returns>The problem.</returns>
    internal SchematronProblem TakeProblem()
    {
        if (!_hasProblem)
        {
            return Run.GetProblem();
        }

        _hasProblem = false;
        return _problem;
    }
}
