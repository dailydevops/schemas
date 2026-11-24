namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>Square array constructor, <c>[ a, b ]</c> (production SquareArrayConstructor, XPath 3.1).</summary>
internal sealed class SquareArrayConstructor : Expression
{
    /// <summary>Initializes a new instance of the <see cref="SquareArrayConstructor"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="members">Gets the members.</param>
    public SquareArrayConstructor(SourceSpan location, IReadOnlyList<Expression> members)
        : base(location) => Members = NodeList.Copy(members);

    /// <summary>Gets the members.</summary>
    public IReadOnlyList<Expression> Members { get; }

    /// <inheritdoc />
    private protected override IReadOnlyList<SyntaxNode> CreateChildren()
    {
        List<SyntaxNode> children = [];
        children.AddRange(Members);
        return children.AsReadOnly();
    }

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitSquareArrayConstructor(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitSquareArrayConstructor(this);
}
