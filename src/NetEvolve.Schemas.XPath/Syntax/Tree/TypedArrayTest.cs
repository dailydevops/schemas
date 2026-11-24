namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary><c>array(type)</c> (production TypedArrayTest, XPath 3.1).</summary>
internal sealed class TypedArrayTest : ItemType
{
    /// <summary>Initializes a new instance of the <see cref="TypedArrayTest"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="memberType">Gets the member type.</param>
    public TypedArrayTest(SourceSpan location, SequenceType memberType)
        : base(location) => MemberType = memberType;

    /// <summary>Gets the member type.</summary>
    public SequenceType MemberType { get; }

    /// <inheritdoc />
    private protected override IReadOnlyList<SyntaxNode> CreateChildren()
    {
        List<SyntaxNode> children = [];
        children.Add(MemberType);
        return children.AsReadOnly();
    }

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitTypedArrayTest(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitTypedArrayTest(this);
}
