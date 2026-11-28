namespace NetEvolve.Schemas.Schematron.Seams;

using NetEvolve.Schemas.XPath.Abstractions;

/// <summary>
/// What a run offers to <c>xsl:analyze-string</c> besides evaluating: the groups of the current matching substring, which the XPath
/// host slot <c>RegexGroups</c> carries to <c>regex-group()</c> (XSLT 2.0 section 15.2). The instruction writes the slot for every
/// substring, and puts back what it found when it ends, also when it fails. <see langword="null"/> means absent: outside every
/// matching substring, and inside a non-matching substring.
/// </summary>
/// <remarks>
/// The run of the XPath binding implements this interface too (an <see cref="IQueryRun"/> has no member for the slots): it copies the
/// value into the XPath host slot when it starts an evaluation, and a function body clears it (#1087), as for <see cref="IXsltGroupRun"/>.
/// </remarks>
internal interface IXsltRegexRun : IQueryRun
{
    /// <summary>Gets or sets the groups of the current matching substring, or <see langword="null"/> when there is none.</summary>
    XPathRegexGroups? RegexGroups { get; set; }
}
