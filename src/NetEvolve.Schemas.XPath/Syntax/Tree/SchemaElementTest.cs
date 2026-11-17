namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary><c>schema-element()</c> (productions SchemaElementTest, ElementDeclaration).</summary>
internal sealed class SchemaElementTest : KindTest
{
    /// <summary>Initializes a new instance of the <see cref="SchemaElementTest"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="name">Gets the name.</param>
    public SchemaElementTest(SourceSpan location, QualifiedName name)
        : base(location) => Name = name;

    /// <summary>Gets the name.</summary>
    public QualifiedName Name { get; }

    /// <inheritdoc />
    public override IReadOnlyList<SyntaxNode> Children => [Name];

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitSchemaElementTest(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitSchemaElementTest(this);
}
