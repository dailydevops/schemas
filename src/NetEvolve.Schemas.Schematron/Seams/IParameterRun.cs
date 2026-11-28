namespace NetEvolve.Schemas.Schematron.Seams;

/// <summary>
/// What a run offers to bind a caller value of a typed global <c>xsl:param</c> (XSLT 2.0 section 9.5, #795): the value is an untyped
/// atomic value and is converted to the <see langword="as"/> type of the parameter. The run of the XPath binding implements this interface too.
/// </summary>
internal interface IParameterRun : IQueryRun
{
    /// <summary>Converts <paramref name="value"/> to <paramref name="type"/> and binds it; a value that does not convert is XTTE0590.</summary>
    /// <param name="slot">The slot of the parameter.</param>
    /// <param name="value">The caller value.</param>
    /// <param name="type">The canonical type: <c>xs:string</c>, <c>xs:boolean</c>, <c>xs:double</c>, or <c>xs:integer</c>.</param>
    /// <param name="location">The location of the parameter, for the problem.</param>
    /// <returns><see cref="QueryStatus.Problem"/> when the value does not convert; the problem is <see cref="IQueryRun.GetProblem"/>.</returns>
    QueryStatus TryBindParameter(int slot, string value, string type, in DocumentLocation location);
}
