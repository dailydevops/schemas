namespace NetEvolve.Schemas.Xsd.Elements;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Declarations11;
using NetEvolve.Schemas.Xsd.IdentityConstraints.Definitions;
using NetEvolve.Schemas.Xsd.Substitution;

/// <summary>
/// An element declaration (Structures section 3.3). Its type is a by-name reference that the resolver binds; an anonymous type
/// stays as syntax until the type builders construct it.
/// </summary>
/// <remarks>
/// Limitations: the declaration lists several heads of the <c>substitutionGroup</c> of a global declaration only when the builder is given them
/// (XSD 1.1); the type of a declaration without a type is the one of its first head. Type alternatives are
/// XSD 1.1 (#319); checking the value constraint against the type (cos-valid-default) is XSD-54 (#307).
/// </remarks>
internal sealed class ElementDeclaration : IReferencingComponent, IMultiHeadElement
{
    private readonly List<IdentityConstraintDefinition> _constraints = [];
    private readonly XsdReference[] _references;
    private readonly int[] _headIndexes;
    private readonly ElementDeclaration?[] _heads;
    private readonly bool _inheritsType;
    private IXsdComponent? _type;

    private ElementDeclaration(
        XPathQName name,
        DocumentLocation location,
        bool isGlobal,
        XsdReference[] references,
        int[] headIndexes,
        bool inheritsType,
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
        _headIndexes = headIndexes;
        _heads = new ElementDeclaration?[headIndexes.Length];
        _inheritsType = inheritsType;
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

    /// <summary>
    /// Gets the type definition once it is bound, <see langword="null"/> while it is unresolved or anonymous and not yet constructed. A
    /// declaration that states no type and names a head has the type of the head (Structures 3.3.2, clause 1.2 of the type definition).
    /// </summary>
    public IXsdComponent? TypeDefinition => _type ?? InheritedType();

    /// <summary>Gets the head of the substitution group, <see langword="null"/> for none or while it is unresolved.</summary>
    internal ElementDeclaration? Head => _heads.Length > 0 ? _heads[0] : null;

    /// <inheritdoc />
    IReadOnlyList<IMultiHeadElement> IMultiHeadElement.Heads
    {
        get
        {
            if (_heads.Length == 0)
            {
                return [];
            }

            // A head without a type is an error that the resolver reported; it takes no part in the groups, as in XSD 1.0.
            var heads = new List<IMultiHeadElement>(_heads.Length);
            for (var i = 0; i < _heads.Length; i++)
            {
                if (_heads[i] is { } head && head is not { TypeDefinition: null, InheritsType: false })
                {
                    heads.Add(head);
                }
            }

            return heads;
        }
    }

    /// <inheritdoc />
    ISubstitutionElement? ISubstitutionElement.SubstitutionGroup =>
        Head is { TypeDefinition: null, InheritsType: false } ? null : Head;

    /// <inheritdoc />
    IXsdComponent ISubstitutionElement.Type => TypeDefinition ?? ComplexTypeDefinition.AnyType;

    /// <summary>Gets a value indicating whether the declaration states no type and takes the type of its head.</summary>
    internal bool InheritsType => _inheritsType;

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

    /// <summary>Gets the identity constraints the declaration owns in document order (Structures 3.3, {identity-constraint definitions}).</summary>
    internal IReadOnlyList<IdentityConstraintDefinition> IdentityConstraints => _constraints;

    /// <summary>Adds an identity constraint that the declaration owns.</summary>
    /// <param name="constraint">The constraint.</param>
    internal void AddIdentityConstraint(IdentityConstraintDefinition constraint)
    {
        ArgumentNullException.ThrowIfNull(constraint);

        _constraints.Add(constraint);
    }

    /// <summary>Gets the identity of the type used to compare two declarations, <see langword="null"/> while it is unknown.</summary>
    internal object? TypeIdentity => (object?)TypeDefinition ?? InlineTypeSyntax;

    /// <inheritdoc />
    public XsdReference GetReference(int index) => _references[index];

    /// <inheritdoc />
    public void Bind(int index, IXsdComponent target)
    {
        ArgumentNullException.ThrowIfNull(target);

        var head = Array.IndexOf(_headIndexes, index);
        if (head >= 0)
        {
            _heads[head] = target as ElementDeclaration;
        }
        else
        {
            _type = target;
        }
    }

    /// <summary>Receives the type that the type builders constructed from <see cref="InlineTypeSyntax"/>.</summary>
    /// <param name="type">The anonymous type.</param>
    internal void BindInlineType(IXsdComponent type)
    {
        ArgumentNullException.ThrowIfNull(type);

        _type = type;
    }

    // ponytail: a chain of affiliations that is longer than the limit, or circular, has no inherited type; the cycle itself is reported by the substitution group check.
    private IXsdComponent? InheritedType()
    {
        var current = this;
        for (var steps = 0; steps < 4096 && current is { _inheritsType: true, _type: null }; steps++)
        {
            current = current.Head;
        }

        return current?._type;
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
    /// <param name="substitutionGroup">The expanded name of the first substitution group head, <see langword="default"/> for none.</param>
    /// <param name="otherHeads">The expanded names of the further heads of XSD 1.1, <see langword="null"/> for none.</param>
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
        XsdDerivationMethod final,
        XPathQName substitutionGroup = default,
        IReadOnlyList<XPathQName>? otherHeads = null
    )
    {
        var references = new List<XsdReference>(2);
        if (typeName != default)
        {
            references.Add(new XsdReference(XsdSymbolSpace.Types, typeName, location, false));
        }

        var headIndexes = new List<int>(1 + (otherHeads?.Count ?? 0));
        var inheritsType = typeName == default && inlineTypeSyntax is null && substitutionGroup != default;
        if (substitutionGroup != default)
        {
            headIndexes.Add(references.Count);
            references.Add(new XsdReference(XsdSymbolSpace.Elements, substitutionGroup, location, false));
            for (var i = 0; otherHeads is not null && i < otherHeads.Count; i++)
            {
                headIndexes.Add(references.Count);
                references.Add(new XsdReference(XsdSymbolSpace.Elements, otherHeads[i], location, false));
            }
        }

        return new(
            name,
            location,
            isGlobal,
            [.. references],
            [.. headIndexes],
            inheritsType,
            inlineTypeSyntax,
            isNillable,
            valueConstraint,
            isAbstract,
            block,
            final
        );
    }
}
