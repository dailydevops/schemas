namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>One <c>key : value</c> entry of a map constructor (production MapConstructorEntry, XPath 3.1).</summary>
internal sealed class MapEntry : SyntaxNode
{
    /// <summary>Initializes a new instance of the <see cref="MapEntry"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="key">Gets the key.</param>
    /// <param name="value">Gets the value.</param>
    public MapEntry(SourceSpan location, Expression key, Expression value)
        : base(location)
    {
        Key = key;
        Value = value;
    }

    /// <summary>Gets the key.</summary>
    public Expression Key { get; }

    /// <summary>Gets the value.</summary>
    public Expression Value { get; }

    /// <inheritdoc />
    private protected override IReadOnlyList<SyntaxNode> CreateChildren()
    {
        List<SyntaxNode> children = [];
        children.Add(Key);
        children.Add(Value);
        return children.AsReadOnly();
    }

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitMapEntry(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitMapEntry(this);
}
