namespace NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>Base of kind tests (production KindTest).</summary>
internal abstract class KindTest : NodeTest
{
    /// <summary>Initializes a new instance of the <see cref="KindTest"/> class.</summary>
    /// <param name="location">The source location.</param>
    private protected KindTest(SourceSpan location)
        : base(location) { }
}
