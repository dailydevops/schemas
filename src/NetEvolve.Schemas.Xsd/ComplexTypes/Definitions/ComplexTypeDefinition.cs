namespace NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;

using System.Collections.Generic;
using System.Runtime.InteropServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.ContentModels.AllGroups11;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.OpenContent;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;
using NetEvolve.Schemas.Xsd.Substitution;
using NetEvolve.Schemas.Xsd.Wildcards.Core;

/// <summary>
/// A complex type definition (Structures section 3.4) with the content type and the attribute uses that the mapping rules of
/// section 3.4.2.2 give it. Immutable; the factories take resolved components (the base type, the attribute uses, the
/// attribute wildcard) and report a violation of the representation constraints they can decide as a located issue.
/// </summary>
/// <remarks>
/// <para>
/// The factories correspond to the three shapes of the XML representation: <see cref="Implicit"/> (no
/// <c>simpleContent</c> or <c>complexContent</c>, a restriction of <see cref="AnyType"/>), <see cref="FromComplexContent"/>, and
/// <see cref="FromSimpleContent"/>. The caller passes the particle of the explicit content, the uses of the <c>attribute</c>
/// children and of the attribute groups (flattened), and the complete wildcard of the own <c>anyAttribute</c> and the attribute
/// groups; <see cref="EffectiveMixed"/> gives the effective mixed value.
/// </para>
/// <para>
/// Limitations: whether a derivation is valid (final base types, the content of an extension or restriction against the base:
/// <c>cos-ct-extends</c>, <c>derivation-ok-restriction</c>, <c>cos-ct-derived-ok</c>) is XSD-10 and XSD-62; compiling the content
/// model is XSD-09; assertions are XSD-23, so that mapping rule is not applied (an extension combines base and own content as a
/// sequence, except two <c>all</c> groups in XSD 1.1, which <see cref="AllGroup11Compiler.Extend"/> merges); open content (XSD-20) follows the
/// mapping rules of XSD 1.1 and is given to the factories as the <c>openContent</c> child and the <c>defaultOpenContent</c> of the
/// schema, see <see cref="OpenContentRules"/> for the limitation of the extension case; at most one attribute of type ID
/// (<c>ct-props-correct.5</c>) is not checked because the simple type definition cannot tell derivation from <c>ID</c> yet;
/// circular derivation cannot arise because the base is resolved before the type exists, so the pipeline that orders the
/// construction reports it (<c>ct-props-correct.3</c>); <see cref="AnyType"/> is the ur-type the registry registers;
/// the emptiness of a restricted mixed base for a <c>simpleContent</c> restriction is not checked
/// (<c>derivation-ok-restriction.5</c>).
/// </para>
/// </remarks>
internal sealed class ComplexTypeDefinition : IReferencingComponent, IBlockedType
{
    private const XsdDerivationMethod DerivationMethods =
        XsdDerivationMethod.Extension | XsdDerivationMethod.Restriction;

    private static readonly AttributeUse[] NoUses = [];
    private static readonly RefSite[] NoSites = [];

    private readonly RefSite[] _sites;
    private readonly AttributeUse[] _uses;

    private ComplexTypeDefinition(
        ComplexTypeHeader header,
        XsdDerivationMethod derivationMethod,
        IXsdComponent? baseType,
        Content content,
        AttributeUse[] uses,
        IWildcard? attributeWildcard,
        RefSite[] sites
    )
    {
        Name = header.Name;
        Location = header.Location;
        IsAbstract = header.IsAbstract;
        Block = header.Block & DerivationMethods;
        Final = header.Final & DerivationMethods;
        DerivationMethod = derivationMethod;
        BaseType = baseType;
        ContentKind = content.Kind;
        ContentParticle = content.Particle;
        SimpleContentType = content.SimpleType;
        OpenContent = content.OpenContent;
        _uses = uses;
        AttributeWildcard = attributeWildcard;
        _sites = sites;
    }

    /// <summary>Gets the ur-type <c>xs:anyType</c>: mixed content of any lax elements and any lax attributes.</summary>
    public static ComplexTypeDefinition AnyType { get; } = CreateAnyType();

    /// <inheritdoc />
    public XsdComponentKind Kind => XsdComponentKind.ComplexType;

    /// <inheritdoc />
    public XPathQName Name { get; }

    /// <inheritdoc />
    public DocumentLocation Location { get; }

    /// <summary>Gets a value indicating whether the type has no name.</summary>
    public bool IsAnonymous => Name == default;

    /// <summary>Gets a value indicating whether the type is abstract.</summary>
    public bool IsAbstract { get; }

    /// <summary>Gets the substitutions of derived types that are blocked: any of extension and restriction.</summary>
    public XsdDerivationMethod Block { get; }

    /// <summary>Gets the derivations that are not allowed from this type: any of extension and restriction.</summary>
    public XsdDerivationMethod Final { get; }

    /// <summary>Gets the derivation method: extension or restriction.</summary>
    public XsdDerivationMethod DerivationMethod { get; }

    /// <summary>Gets the base type: a complex or a simple type definition, <see langword="null"/> for <see cref="AnyType"/>.</summary>
    public IXsdComponent? BaseType { get; }

    /// <summary>Gets the variety of the content type.</summary>
    public ComplexContentKind ContentKind { get; }

    /// <summary>Gets the particle of an element-only or mixed content type, <see langword="null"/> otherwise.</summary>
    public Particle? ContentParticle { get; }

    /// <summary>
    /// Gets the explicit content particle that may not occur (<c>maxOccurs</c> 0), <see langword="null"/> otherwise. The content type
    /// is empty then and holds no particle, so this keeps the particle for the checks that apply to the schema document.
    /// </summary>
    internal Particle? NeverOccurringContent { get; private set; }

    /// <summary>Gets the simple type of a simple content type, <see langword="null"/> otherwise.</summary>
    public SimpleTypeDefinition? SimpleContentType { get; }

    /// <summary>Gets the attribute uses, none of them prohibited: the inherited ones first, then the own ones in document order.</summary>
    public IReadOnlyList<AttributeUse> AttributeUses => _uses;

    /// <summary>Gets the attribute wildcard, <see langword="null"/> for none.</summary>
    public IWildcard? AttributeWildcard { get; }

    /// <summary>Gets the open content of an element-only or mixed content type, <see langword="null"/> for none.</summary>
    public OpenContentProperty? OpenContent { get; }

    /// <inheritdoc />
    public int ReferenceCount => _sites.Length;

    /// <inheritdoc />
    public XsdReference GetReference(int index) => _sites[index].Owner.GetReference(_sites[index].Index);

    /// <inheritdoc />
    public void Bind(int index, IXsdComponent target)
    {
        ArgumentNullException.ThrowIfNull(target);

        _sites[index].Owner.Bind(_sites[index].Index, target);
    }

    /// <summary>Gets the effective mixed value: the <c>mixed</c> of <c>complexContent</c>, else that of <c>complexType</c>.</summary>
    /// <param name="typeMixed">The <c>mixed</c> attribute of <c>complexType</c>, <see langword="false"/> when absent.</param>
    /// <param name="contentMixed">The <c>mixed</c> attribute of <c>complexContent</c>, <see langword="null"/> when absent.</param>
    /// <returns>The effective mixed value.</returns>
    internal static bool EffectiveMixed(bool typeMixed, bool? contentMixed) => contentMixed ?? typeMixed;

    /// <summary>Defines a type without <c>simpleContent</c> or <c>complexContent</c>: a restriction of <see cref="AnyType"/>.</summary>
    /// <param name="header">The name, location, abstract, block, and final.</param>
    /// <param name="mixed">The effective mixed value.</param>
    /// <param name="explicitContent">The particle of the <see langword="group"/>, <c>all</c>, <c>choice</c>, or <c>sequence</c> child, <see langword="null"/> for none.</param>
    /// <param name="attributeUses">The uses of the attributes and attribute groups in document order.</param>
    /// <param name="attributeWildcard">The complete attribute wildcard of the own children, <see langword="null"/> for none.</param>
    /// <param name="reporter">The reporter.</param>
    /// <param name="openContent">The <c>openContent</c> child, <see langword="null"/> for none.</param>
    /// <param name="defaultOpenContent">The <c>defaultOpenContent</c> child of the schema, <see langword="null"/> for none.</param>
    /// <returns>The definition.</returns>
    internal static ComplexTypeDefinition Implicit(
        ComplexTypeHeader header,
        bool mixed,
        Particle? explicitContent,
        IReadOnlyList<AttributeUse> attributeUses,
        IWildcard? attributeWildcard,
        XsdIssueReporter reporter,
        OpenContentElement? openContent = null,
        OpenContentElement? defaultOpenContent = null
    )
    {
        ArgumentNullException.ThrowIfNull(attributeUses);
        ArgumentNullException.ThrowIfNull(reporter);

        // Without a base that has open content the resolution cannot fail, so its result is not needed.
        _ = TryOpen(
            header.Location,
            Restricted(mixed, EffectiveContent(header.Location, mixed, explicitContent)),
            openContent,
            defaultOpenContent,
            reporter,
            out var content
        );

        return Create(
            header,
            XsdDerivationMethod.Restriction,
            AnyType,
            content,
            explicitContent,
            attributeUses,
            attributeWildcard,
            reporter
        );
    }

    /// <summary>Defines a type with <c>complexContent</c>.</summary>
    /// <param name="header">The name, location, abstract, block, and final.</param>
    /// <param name="method">The derivation method: <see cref="XsdDerivationMethod.Extension"/> or <see cref="XsdDerivationMethod.Restriction"/>.</param>
    /// <param name="baseType">The resolved base type, which must be a complex type.</param>
    /// <param name="mixed">The effective mixed value, see <see cref="EffectiveMixed"/>.</param>
    /// <param name="explicitContent">The particle of the explicit content, <see langword="null"/> for none.</param>
    /// <param name="attributeUses">The uses of the attributes and attribute groups in document order.</param>
    /// <param name="attributeWildcard">The complete attribute wildcard of the own children, <see langword="null"/> for none.</param>
    /// <param name="reporter">The reporter.</param>
    /// <param name="openContent">The <c>openContent</c> child, <see langword="null"/> for none.</param>
    /// <param name="defaultOpenContent">The <c>defaultOpenContent</c> child of the schema, <see langword="null"/> for none.</param>
    /// <param name="version">The XSD version; in <see cref="XsdVersion.V11"/> an extension of an <c>all</c> group by an <c>all</c> group is one merged <c>all</c> group.</param>
    /// <returns>The definition, <see langword="null"/> after an issue was reported.</returns>
    internal static ComplexTypeDefinition? FromComplexContent(
        ComplexTypeHeader header,
        XsdDerivationMethod method,
        IXsdComponent baseType,
        bool mixed,
        Particle? explicitContent,
        IReadOnlyList<AttributeUse> attributeUses,
        IWildcard? attributeWildcard,
        XsdIssueReporter reporter,
        OpenContentElement? openContent = null,
        OpenContentElement? defaultOpenContent = null,
        XsdVersion version = XsdVersion.V10
    )
    {
        GuardDerivation(method, baseType, attributeUses, reporter);

        if (baseType is not ComplexTypeDefinition parent)
        {
            ReportRepresentation(
                reporter,
                header.Location,
                "src-ct.1",
                "The base type of a complexContent derivation must be a complex type."
            );
            return null;
        }

        var effective = EffectiveContent(header.Location, mixed, explicitContent);
        Content content;
        if (method == XsdDerivationMethod.Restriction)
        {
            content = Restricted(mixed, effective);
        }
        else if (!TryExtend(header.Location, parent, mixed, effective, version, reporter, out content))
        {
            return null;
        }

        if (!TryOpen(header.Location, content, openContent, defaultOpenContent, reporter, out content))
        {
            return null;
        }

        return Create(
            header,
            method,
            parent,
            content,
            explicitContent,
            attributeUses,
            attributeWildcard,
            reporter,
            parent
        );
    }

    /// <summary>Defines a type with <c>simpleContent</c>.</summary>
    /// <param name="header">The name, location, abstract, block, and final.</param>
    /// <param name="method">The derivation method: <see cref="XsdDerivationMethod.Extension"/> or <see cref="XsdDerivationMethod.Restriction"/>.</param>
    /// <param name="baseType">
    /// The resolved base type: a complex type with simple content, a complex type with mixed content for a restriction, or a
    /// simple type for an extension.
    /// </param>
    /// <param name="simpleType">
    /// The simple type of a restriction (the anonymous <c>simpleType</c> child, or the base restricted by the facets),
    /// <see langword="null"/> to keep that of the base; ignored for an extension.
    /// </param>
    /// <param name="attributeUses">The uses of the attributes and attribute groups in document order.</param>
    /// <param name="attributeWildcard">The complete attribute wildcard of the own children, <see langword="null"/> for none.</param>
    /// <param name="reporter">The reporter.</param>
    /// <returns>The definition, <see langword="null"/> after an issue was reported.</returns>
    internal static ComplexTypeDefinition? FromSimpleContent(
        ComplexTypeHeader header,
        XsdDerivationMethod method,
        IXsdComponent baseType,
        SimpleTypeDefinition? simpleType,
        IReadOnlyList<AttributeUse> attributeUses,
        IWildcard? attributeWildcard,
        XsdIssueReporter reporter
    )
    {
        GuardDerivation(method, baseType, attributeUses, reporter);

        var parent = baseType as ComplexTypeDefinition;
        var restriction = method == XsdDerivationMethod.Restriction;
        SimpleTypeDefinition? contentType = null;
        if (parent is not null)
        {
            if (parent.ContentKind == ComplexContentKind.Simple)
            {
                contentType = restriction ? simpleType ?? parent.SimpleContentType : parent.SimpleContentType;
            }
            else if (restriction && parent.ContentKind == ComplexContentKind.Mixed)
            {
                contentType = simpleType;
            }
        }
        else if (!restriction)
        {
            contentType = AsSimple(baseType);
        }

        if (contentType is null)
        {
            ReportRepresentation(
                reporter,
                header.Location,
                "src-ct.2",
                "The base type of a simpleContent derivation must be a complex type with simple content, a complex type with mixed content and a simple type for a restriction, or a simple type for an extension."
            );
            return null;
        }

        return Create(
            header,
            method,
            (IXsdComponent?)parent ?? contentType,
            new Content(ComplexContentKind.Simple, null, contentType),
            null,
            attributeUses,
            attributeWildcard,
            reporter,
            parent
        );
    }

    private static void GuardDerivation(
        XsdDerivationMethod method,
        IXsdComponent baseType,
        IReadOnlyList<AttributeUse> attributeUses,
        XsdIssueReporter reporter
    )
    {
        ArgumentOutOfRangeException.ThrowIfLessThan((int)method, (int)XsdDerivationMethod.Extension);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)method, (int)XsdDerivationMethod.Restriction);
        ArgumentNullException.ThrowIfNull(baseType);
        ArgumentNullException.ThrowIfNull(attributeUses);
        ArgumentNullException.ThrowIfNull(reporter);
    }

    private static ComplexTypeDefinition Create(
        ComplexTypeHeader header,
        XsdDerivationMethod method,
        IXsdComponent baseType,
        Content content,
        Particle? explicitContent,
        IReadOnlyList<AttributeUse> ownUses,
        IWildcard? ownWildcard,
        XsdIssueReporter reporter,
        ComplexTypeDefinition? inheritFrom = null
    )
    {
        var extension = method == XsdDerivationMethod.Extension;
        var sites = new List<RefSite>();
        explicitContent?.CollectReferences(sites);

        var type = new ComplexTypeDefinition(
            header,
            method,
            baseType,
            content,
            CombineUses(inheritFrom, extension, ownUses, reporter),
            CombineWildcards(inheritFrom, extension, ownWildcard, header.Location, reporter),
            sites.Count == 0 ? NoSites : sites.ToArray()
        )
        {
            NeverOccurringContent = explicitContent is { MaxOccurs: 0 } ? explicitContent : null,
        };
        return type;
    }

    // Anything the specification calls empty content: no particle, a particle that may not occur, or a group without
    // particles (a choice only when it may be skipped). A group reference is never empty here, whatever it refers to.
    private static bool IsEmpty(Particle? particle) =>
        particle is null
        || particle.MaxOccurs == 0
        || (
            particle.Term is ModelGroup { Particles.Count: 0 } group
            && (group.Compositor != ModelGroupCompositor.Choice || particle.MinOccurs == 0)
        );

    private static Particle? EffectiveContent(DocumentLocation location, bool mixed, Particle? explicitContent) =>
        !IsEmpty(explicitContent) ? explicitContent
        : mixed ? Particle.Create(location, 1, 1, ModelGroup.Create(ModelGroupCompositor.Sequence, location, []))
        : null;

    private static Content Restricted(bool mixed, Particle? effective) =>
        effective is null
            ? new Content(ComplexContentKind.Empty, null, null)
            : new Content(mixed ? ComplexContentKind.Mixed : ComplexContentKind.ElementOnly, effective, null);

    private static bool TryExtend(
        DocumentLocation location,
        ComplexTypeDefinition parent,
        bool mixed,
        Particle? effective,
        XsdVersion version,
        XsdIssueReporter reporter,
        out Content content
    )
    {
        if (effective is null)
        {
            content = new Content(
                parent.ContentKind,
                parent.ContentParticle,
                parent.SimpleContentType,
                parent.OpenContent
            );
            return true;
        }

        if (parent.ContentKind == ComplexContentKind.Empty)
        {
            content = Restricted(mixed, effective);
            return true;
        }

        if (parent.ContentKind == ComplexContentKind.Simple)
        {
            content = default;
            reporter.Report(
                XsdRuleLookup.Find(XsdRuleCodes.CosCtExtends),
                location,
                "A type with simple content cannot be extended with element content."
            );
            return false;
        }

        // Structures 1.1, 3.4.2 clause 4.2.3.2: two all groups merge into one; every other combination is a sequence.
        var merged = version == XsdVersion.V11 ? AllGroup11Compiler.Extend(parent.ContentParticle!, effective) : null;
        var sequence = ModelGroup.Create(ModelGroupCompositor.Sequence, location, [parent.ContentParticle!, effective]);
        content = new Content(
            mixed ? ComplexContentKind.Mixed : ComplexContentKind.ElementOnly,
            merged ?? Particle.Create(location, 1, 1, sequence),
            null,
            parent.OpenContent
        );
        return true;
    }

    // Steps 5 and 6 of the mapping rule for the content type (3.4.2.3.3): the open content of the explicit content type, and the
    // wildcard element over it. Open content belongs to element-only and mixed content, and an empty explicit content type
    // that gets open content becomes element-only with an empty sequence.
    private static bool TryOpen(
        DocumentLocation location,
        Content explicitContent,
        OpenContentElement? own,
        OpenContentElement? schemaDefault,
        XsdIssueReporter reporter,
        out Content content
    )
    {
        content = explicitContent;
        if (explicitContent.Kind == ComplexContentKind.Simple || (own is null && schemaDefault is null))
        {
            return true;
        }

        if (
            !OpenContentRules.TryResolve(
                own,
                schemaDefault,
                explicitContent.Kind == ComplexContentKind.Empty,
                explicitContent.OpenContent,
                location,
                reporter,
                out var open
            )
        )
        {
            return false;
        }

        if (ReferenceEquals(open, explicitContent.OpenContent))
        {
            return true;
        }

        content =
            explicitContent.Kind == ComplexContentKind.Empty
                ? new Content(
                    ComplexContentKind.ElementOnly,
                    Particle.Create(location, 1, 1, ModelGroup.Create(ModelGroupCompositor.Sequence, location, [])),
                    null,
                    open
                )
                : new Content(explicitContent.Kind, explicitContent.Particle, null, open);
        return true;
    }

    // Extension keeps every use of the base; restriction keeps those the own uses do not name. A prohibited use only removes.
    private static AttributeUse[] CombineUses(
        ComplexTypeDefinition? parent,
        bool extension,
        IReadOnlyList<AttributeUse> ownUses,
        XsdIssueReporter reporter
    )
    {
        var byName = new Dictionary<XPathQName, AttributeUse>();
        var own = new List<AttributeUse>(ownUses.Count);
        for (var i = 0; i < ownUses.Count; i++)
        {
            var use = ownUses[i];
            if (byName.TryGetValue(use.Name, out var seen))
            {
                if (!ReferenceEquals(seen.Declaration, use.Declaration))
                {
                    ReportDuplicate(reporter, use);
                }

                continue;
            }

            byName.Add(use.Name, use);
            if (!use.Prohibited)
            {
                own.Add(use);
            }
        }

        var inherited = parent?._uses ?? NoUses;
        if (inherited.Length == 0 && own.Count == 0)
        {
            return NoUses;
        }

        var all = new List<AttributeUse>(inherited.Length + own.Count);
        var inheritedByName = new Dictionary<XPathQName, AttributeUse>();
        for (var i = 0; i < inherited.Length; i++)
        {
            if (extension || !byName.ContainsKey(inherited[i].Name))
            {
                all.Add(inherited[i]);
                inheritedByName[inherited[i].Name] = inherited[i];
            }
        }

        for (var i = 0; i < own.Count; i++)
        {
            if (extension && inheritedByName.TryGetValue(own[i].Name, out var clash))
            {
                if (!ReferenceEquals(clash.Declaration, own[i].Declaration))
                {
                    ReportDuplicate(reporter, own[i]);
                }

                continue;
            }

            all.Add(own[i]);
        }

        return all.ToArray();
    }

    // Restriction knows only the own wildcard; extension adds the one of the base, the own process contents win.
    private static IWildcard? CombineWildcards(
        ComplexTypeDefinition? parent,
        bool extension,
        IWildcard? own,
        DocumentLocation location,
        XsdIssueReporter reporter
    )
    {
        var inherited = extension ? parent?.AttributeWildcard : null;
        if (inherited is null)
        {
            return own;
        }

        if (own is null)
        {
            return inherited;
        }

        var union = own.Union(inherited);
        if (union is null)
        {
            reporter.Report(
                XsdRuleLookup.Find(XsdRuleCodes.CosAwUnion),
                location,
                "The attribute wildcards of the type and its base type have no expressible union."
            );
            return own;
        }

        return union;
    }

    private static void ReportDuplicate(XsdIssueReporter reporter, AttributeUse use) =>
        reporter.Report(
            XsdRuleLookup.CtPropsCorrect,
            use.Location,
            "Two attribute uses have the same name and target namespace.",
            "ct-props-correct.4"
        );

    private static void ReportRepresentation(
        XsdIssueReporter reporter,
        DocumentLocation location,
        string clause,
        string message
    ) => reporter.Report(XsdRuleLookup.Find(XsdRuleCodes.SrcCt), location, message, clause);

    // A registered built-in simple type wraps its datatype, which is what a simple type definition is made of.
    private static SimpleTypeDefinition? AsSimple(IXsdComponent type) =>
        type switch
        {
            SimpleTypeDefinition simple => simple,
            XsdBuiltInType { Kind: XsdComponentKind.SimpleType, Descriptor: { } descriptor } builtIn =>
                SimpleTypeDefinition.FromDatatype(builtIn.Name, builtIn.Location, descriptor),
            _ => null,
        };

    private static ComplexTypeDefinition CreateAnyType()
    {
        var any = Wildcard.Create(NamespaceConstraint.Any, XsdProcessContents.Lax, default);
        var elements = Particle.Create(default, 0, Particle.Unbounded, any);
        var content = Particle.Create(
            default,
            1,
            1,
            ModelGroup.Create(ModelGroupCompositor.Sequence, default, [elements])
        );
        return new ComplexTypeDefinition(
            new ComplexTypeHeader(
                new XPathQName(SchemaGrammar.Namespace, "anyType"),
                default,
                false,
                XsdDerivationMethod.None,
                XsdDerivationMethod.None
            ),
            XsdDerivationMethod.Restriction,
            null,
            new Content(ComplexContentKind.Mixed, content, null),
            NoUses,
            any,
            NoSites
        );
    }

    [StructLayout(LayoutKind.Auto)]
    private readonly struct Content
    {
        internal Content(
            ComplexContentKind kind,
            Particle? particle,
            SimpleTypeDefinition? simpleType,
            OpenContentProperty? openContent = null
        )
        {
            Kind = kind;
            Particle = particle;
            SimpleType = simpleType;
            OpenContent = openContent;
        }

        public ComplexContentKind Kind { get; }

        public Particle? Particle { get; }

        public SimpleTypeDefinition? SimpleType { get; }

        public OpenContentProperty? OpenContent { get; }
    }
}
