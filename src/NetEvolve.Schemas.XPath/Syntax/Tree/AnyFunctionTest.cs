namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary><c>function(*)</c> (production AnyFunctionTest, XPath 3.0).</summary>
internal sealed class AnyFunctionTest : ItemType
{
    /// <summary>Initializes a new instance of the <see cref="AnyFunctionTest"/> class.</summary>
    /// <param name="location">The source location.</param>
    public AnyFunctionTest(SourceSpan location)
        : base(location) { }

    /// <inheritdoc />
    public override IReadOnlyList<SyntaxNode> Children => [];

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitAnyFunctionTest(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitAnyFunctionTest(this);
}
