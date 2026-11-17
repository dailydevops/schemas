namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>String literal (production StringLiteral), with doubled quotes already collapsed.</summary>
internal sealed class StringLiteral : Expression
{
    /// <summary>Initializes a new instance of the <see cref="StringLiteral"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="value">Gets the value.</param>
    public StringLiteral(SourceSpan location, string value)
        : base(location) => Value = value;

    /// <summary>Gets the value.</summary>
    public string Value { get; }

    /// <inheritdoc />
    public override IReadOnlyList<SyntaxNode> Children => [];

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitStringLiteral(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitStringLiteral(this);
}
