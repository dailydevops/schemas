namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>Path expression (productions PathExpr and RelativePathExpr). A mid-path <c>//</c> is an explicit descendant-or-self::node() step.</summary>
internal sealed class PathExpression : Expression
{
    /// <summary>Initializes a new instance of the <see cref="PathExpression"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="root">Gets the root.</param>
    /// <param name="steps">Gets the steps.</param>
    public PathExpression(SourceSpan location, PathRoot root, IReadOnlyList<Expression> steps)
        : base(location)
    {
        Root = root;
        Steps = NodeList.Copy(steps);
    }

    /// <summary>Gets the root.</summary>
    public PathRoot Root { get; }

    /// <summary>Gets the steps.</summary>
    public IReadOnlyList<Expression> Steps { get; }

    /// <inheritdoc />
    public override IReadOnlyList<SyntaxNode> Children
    {
        get
        {
            List<SyntaxNode> children = [];
            children.AddRange(Steps);
            return children.AsReadOnly();
        }
    }

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitPathExpression(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitPathExpression(this);
}
