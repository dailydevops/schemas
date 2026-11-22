namespace NetEvolve.Schemas.Xsd.Elements;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>
/// An element declaration (Structures section 3.3). Its type is a by-name reference that the resolver binds; an anonymous type
/// stays as syntax until the type builders construct it.
/// </summary>
/// <remarks>
/// Limitations: substitution group affiliation is XSD-12 (#239), identity constraints are XSD-17 (#241), type alternatives are
/// XSD 1.1 (#319), and a local <c>targetNamespace</c> is XSD-71 (#322); checking the value constraint against the type
/// (cos-valid-default) is XSD-54 (#307). <see langword="abstract"/>, <c>final</c> and <c>substitutionGroup</c> on a local declaration
/// are not rejected.
/// </remarks>
internal sealed class ElementDeclaration : IReferencingComponent
{
    private readonly XsdReference[] _references;

    private ElementDeclaration(
        XPathQName name,
        DocumentLocation location,
        bool isGlobal,
        XsdReference[] references,
        ISchemaSyntaxElement? inlineTypeSyntax,
        bool isNillable,
        ElementValueConstraint valueConstraint,
        bool isAbstract,
        XsdDerivationMethod block,
        XsdDerivationMethod final
    )
    {
        Name = name;
        Location = location;
        IsGlobal = isGlobal;
        _references = references;
        InlineTypeSyntax = inlineTypeSyntax;
        IsNillable = isNillable;
        ValueConstraint = valueConstraint;
        IsAbstract = isAbstract;
        Block = block;
        Final = final;
    }

    /// <inheritdoc />
    public XsdComponentKind Kind => XsdComponentKind.ElementDeclaration;

    /// <inheritdoc />
    public XPathQName Name { get; }

    /// <inheritdoc />
    public DocumentLocation Location { get; }

    /// <inheritdoc />
    public int ReferenceCount => _references.Length;

    /// <summary>Gets a value indicating whether the declaration is a child of the schema element.</summary>
    public bool IsGlobal { get; }

    /// <summary>Gets the type definition once it is bound, <see langword="null"/> while it is unresolved or anonymous and not yet constructed.</summary>
    public IXsdComponent? TypeDefinition { get; private set; }

    /// <summary>Gets the syntax of the anonymous type, <see langword="null"/> when the type is named or defaulted.</summary>
    public ISchemaSyntaxElement? InlineTypeSyntax { get; }

    /// <summary>Gets a value indicating whether the element may be nilled.</summary>
    public bool IsNillable { get; }

    /// <summary>Gets the value constraint.</summary>
    public ElementValueConstraint ValueConstraint { get; }

    /// <summary>Gets a value indicating whether the declaration is abstract.</summary>
    public bool IsAbstract { get; }

    /// <summary>Gets the derivation methods that block substitution and type use (disallowed substitutions).</summary>
    public XsdDerivationMethod Block { get; }

    /// <summary>Gets the derivation methods that may not derive substitution group members (extension and restriction).</summary>
    public XsdDerivationMethod Final { get; }

    /// <summary>Gets the identity of the type used to compare two declarations, <see langword="null"/> while it is unknown.</summary>
    internal object? TypeIdentity => (object?)TypeDefinition ?? InlineTypeSyntax;

    /// <inheritdoc />
    public XsdReference GetReference(int index) => _references[index];

    /// <inheritdoc />
    public void Bind(int index, IXsdComponent target)
    {
        ArgumentNullException.ThrowIfNull(target);

        TypeDefinition = target;
    }

    /// <summary>Receives the type that the type builders constructed from <see cref="InlineTypeSyntax"/>.</summary>
    /// <param name="type">The anonymous type.</param>
    internal void BindInlineType(IXsdComponent type)
    {
        ArgumentNullException.ThrowIfNull(type);

        TypeDefinition = type;
    }

    /// <summary>Creates a declaration whose references are unresolved.</summary>
    /// <param name="name">The expanded name; for a global declaration it is in the target namespace.</param>
    /// <param name="location">The location of the declaration.</param>
    /// <param name="isGlobal">Whether the declaration is global.</param>
    /// <param name="typeName">The expanded name of the type, <see langword="default"/> for an anonymous type.</param>
    /// <param name="inlineTypeSyntax">The anonymous type, <see langword="null"/> when <paramref name="typeName"/> is given.</param>
    /// <param name="isNillable">Whether the element is nillable.</param>
    /// <param name="valueConstraint">The value constraint.</param>
    /// <param name="isAbstract">Whether the declaration is abstract.</param>
    /// <param name="block">The disallowed substitutions.</param>
    /// <param name="final">The substitution group exclusions.</param>
    /// <returns>The declaration.</returns>
    internal static ElementDeclaration Create(
        XPathQName name,
        DocumentLocation location,
        bool isGlobal,
        XPathQName typeName,
        ISchemaSyntaxElement? inlineTypeSyntax,
        bool isNillable,
        ElementValueConstraint valueConstraint,
        bool isAbstract,
        XsdDerivationMethod block,
        XsdDerivationMethod final
    ) =>
        new(
            name,
            location,
            isGlobal,
            typeName == default ? [] : [new XsdReference(XsdSymbolSpace.Types, typeName, location, false)],
            inlineTypeSyntax,
            isNillable,
            valueConstraint,
            isAbstract,
            block,
            final
        );
}
