namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>Integer literal (production IntegerLiteral).</summary>
internal sealed class IntegerLiteral : Expression
{
    /// <summary>Initializes a new instance of the <see cref="IntegerLiteral"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="lexeme">Gets the literal text as written.</param>
    public IntegerLiteral(SourceSpan location, string lexeme)
        : base(location) => Lexeme = lexeme;

    /// <summary>Gets the literal text as written.</summary>
    public string Lexeme { get; }

    /// <inheritdoc />
    private protected override IReadOnlyList<SyntaxNode> CreateChildren() => [];

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitIntegerLiteral(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitIntegerLiteral(this);
}
