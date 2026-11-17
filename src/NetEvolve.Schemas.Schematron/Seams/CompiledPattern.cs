namespace NetEvolve.Schemas.Schematron.Seams;

using System;
using NetEvolve.Schemas.XPath.Api;

/// <summary>A compiled match pattern with the data the evaluator needs to skip non-candidates.</summary>
internal sealed class CompiledPattern
{
    private readonly XPathQName[] _candidateNames;

    internal CompiledPattern(QueryNodeKinds nodeKinds, XPathQName[] candidateNames)
    {
        NodeKinds = nodeKinds;
        _candidateNames = candidateNames;
    }

    /// <summary>Gets which node kinds can match.</summary>
    internal QueryNodeKinds NodeKinds { get; }

    /// <summary>Gets the expanded names of the last step name tests; empty means any name.</summary>
    internal ReadOnlySpan<XPathQName> CandidateNames => _candidateNames;
}
