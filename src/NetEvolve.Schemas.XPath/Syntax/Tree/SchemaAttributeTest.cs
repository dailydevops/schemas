namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary><c>schema-attribute()</c> (productions SchemaAttributeTest, AttributeDeclaration).</summary>
internal sealed class SchemaAttributeTest : KindTest
{
    /// <summary>Initializes a new instance of the <see cref="SchemaAttributeTest"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="name">Gets the name.</param>
    public SchemaAttributeTest(SourceSpan location, QualifiedName name)
        : base(location) => Name = name;

    /// <summary>Gets the name.</summary>
    public QualifiedName Name { get; }

    /// <inheritdoc />
    private protected override IReadOnlyList<SyntaxNode> CreateChildren() => [Name];

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitSchemaAttributeTest(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitSchemaAttributeTest(this);
}
