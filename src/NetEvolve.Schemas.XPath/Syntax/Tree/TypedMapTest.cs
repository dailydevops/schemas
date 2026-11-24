namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary><c>map(key, value)</c> (production TypedMapTest, XPath 3.1).</summary>
internal sealed class TypedMapTest : ItemType
{
    /// <summary>Initializes a new instance of the <see cref="TypedMapTest"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="keyType">Gets the key type.</param>
    /// <param name="valueType">Gets the value type.</param>
    public TypedMapTest(SourceSpan location, AtomicType keyType, SequenceType valueType)
        : base(location)
    {
        KeyType = keyType;
        ValueType = valueType;
    }

    /// <summary>Gets the key type.</summary>
    public AtomicType KeyType { get; }

    /// <summary>Gets the value type.</summary>
    public SequenceType ValueType { get; }

    /// <inheritdoc />
    private protected override IReadOnlyList<SyntaxNode> CreateChildren()
    {
        List<SyntaxNode> children = [];
        children.Add(KeyType);
        children.Add(ValueType);
        return children.AsReadOnly();
    }

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitTypedMapTest(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitTypedMapTest(this);
}
