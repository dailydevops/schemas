namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary><c>attribute()</c> (productions AttributeTest, AttribNameOrWildcard, AttributeName).</summary>
internal sealed class AttributeTest : KindTest
{
    /// <summary>Initializes a new instance of the <see cref="AttributeTest"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="name">Gets the attribute name, or <see langword="null"/> for <c>*</c> or none.</param>
    /// <param name="typeName">Gets the type name (production TypeName), or <see langword="null"/>.</param>
    public AttributeTest(SourceSpan location, QualifiedName? name, QualifiedName? typeName)
        : base(location)
    {
        Name = name;
        TypeName = typeName;
    }

    /// <summary>Gets the attribute name, or <see langword="null"/> for <c>*</c> or none.</summary>
    public QualifiedName? Name { get; }

    /// <summary>Gets the type name (production TypeName), or <see langword="null"/>.</summary>
    public QualifiedName? TypeName { get; }

    /// <inheritdoc />
    private protected override IReadOnlyList<SyntaxNode> CreateChildren()
    {
        List<SyntaxNode> children = [];
        if (Name is not null)
        {
            children.Add(Name);
        }
        if (TypeName is not null)
        {
            children.Add(TypeName);
        }
        return children.AsReadOnly();
    }

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitAttributeTest(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitAttributeTest(this);
}
