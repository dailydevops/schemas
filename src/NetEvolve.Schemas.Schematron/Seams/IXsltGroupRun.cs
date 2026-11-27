namespace NetEvolve.Schemas.Schematron.Seams;

using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// What a run offers to <c>xsl:for-each-group</c> besides evaluating: the current group and the current grouping key, which the XPath
/// host slots <c>CurrentGroup</c> and <c>CurrentGroupingKey</c> carry to <c>current-group()</c> and <c>current-grouping-key()</c>
/// (design #150 EP-13). The instruction writes both for every group, and puts back what it found when it ends, also when it fails.
/// <see langword="null"/> means absent: outside every grouping instruction, and for the grouping key of a grouping by pattern
/// (XSLT 2.0 section 14.2, XSLT 3.0 section 14.2.2).
/// </summary>
/// <remarks>
/// Limitation: an <see cref="IQueryRun"/> has no member for the slots, so the run of a binding implements this interface too; the run
/// of the XPath binding and the test run implement it. This is the same deviation from the EP-13
/// table as the focus of <c>xsl:for-each</c> (decision 14 of #255): the instruction does not write the XPath host slots itself, and
/// the run copies the two values into them when it starts an evaluation. A function body clears both (<see cref="IXsltFunctionRun.EnterFunction"/>).
/// </remarks>
internal interface IXsltGroupRun : IQueryRun
{
    /// <summary>Gets or sets the current group, or <see langword="null"/> when there is none.</summary>
    XdmSequence? CurrentGroup { get; set; }

    /// <summary>Gets or sets the current grouping key, or <see langword="null"/> when there is none.</summary>
    XdmSequence? CurrentGroupingKey { get; set; }
}
