namespace NetEvolve.Schemas.XPath.Tests.Integration.Compatibility;

/// <summary>An expression and the key of the document it runs over.</summary>
/// <param name="Document">The document key.</param>
/// <param name="Expression">The expression.</param>
internal sealed record CorpusCase(string Document, string Expression)
{
    /// <summary>Gets the key of the case: the document and the expression, which is unique in the corpus.</summary>
    public string Key => Document + "|" + Expression;
}
