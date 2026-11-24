namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>Lookup, <c>operand?key</c>, or the unary lookup <c>?key</c> (productions Lookup and UnaryLookup, XPath 3.1).</summary>
internal sealed class LookupExpression : Expression
{
    /// <summary>Initializes a new instance of the <see cref="LookupExpression"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="operand">Gets the operand; <see langword="null"/> for the unary lookup.</param>
    /// <param name="keyKind">Gets the kind of the key specifier.</param>
    /// <param name="keyName">Gets the name of a name key.</param>
    /// <param name="key">Gets the integer literal or the parenthesized expression of the key.</param>
    public LookupExpression(
        SourceSpan location,
        Expression? operand,
        LookupKeyKind keyKind,
        string? keyName,
        Expression? key
    )
        : base(location)
    {
        Operand = operand;
        KeyKind = keyKind;
        KeyName = keyName;
        Key = key;
    }

    /// <summary>Gets the operand; <see langword="null"/> for the unary lookup.</summary>
    public Expression? Operand { get; }

    /// <summary>Gets the kind of the key specifier.</summary>
    public LookupKeyKind KeyKind { get; }

    /// <summary>Gets the name of a name key; <see langword="null"/> for the other kinds.</summary>
    public string? KeyName { get; }

    /// <summary>Gets the integer literal or the parenthesized expression of the key.</summary>
    public Expression? Key { get; }

    /// <inheritdoc />
    private protected override IReadOnlyList<SyntaxNode> CreateChildren()
    {
        List<SyntaxNode> children = [];
        if (Operand is not null)
        {
            children.Add(Operand);
        }

        if (Key is not null)
        {
            children.Add(Key);
        }

        return children.AsReadOnly();
    }

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitLookupExpression(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitLookupExpression(this);
}
