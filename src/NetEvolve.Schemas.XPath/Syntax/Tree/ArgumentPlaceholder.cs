namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>The argument placeholder <c>?</c> of a partial function application (production ArgumentPlaceholder, XPath 3.0).</summary>
internal sealed class ArgumentPlaceholder : Expression
{
    /// <summary>Initializes a new instance of the <see cref="ArgumentPlaceholder"/> class.</summary>
    /// <param name="location">The source location.</param>
    public ArgumentPlaceholder(SourceSpan location)
        : base(location) { }

    /// <inheritdoc />
    private protected override IReadOnlyList<SyntaxNode> CreateChildren() => [];

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitArgumentPlaceholder(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitArgumentPlaceholder(this);
}
