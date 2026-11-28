namespace NetEvolve.Schemas.Xsd.Attributes.Declarations;

using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;

/// <summary>
/// An attribute group definition (Structures section 3.6). References to nested groups stay by name until
/// <see cref="Resolve"/> runs once; it flattens the uses, reports cycles and duplicate uses, and intersects the wildcards of the
/// nested groups in document order and then the own wildcard through <see cref="IWildcard.Intersect"/>.
/// </summary>
internal sealed class AttributeGroupDefinition : IXsdComponent
{
    private readonly AttributeGroupReference[] _references;
    private readonly IWildcard? _ownWildcard;
    private ResolveState _state;
    private AttributeUse[] _uses;

    private AttributeGroupDefinition(
        XPathQName name,
        DocumentLocation location,
        AttributeUse[] uses,
        AttributeGroupReference[] references,
        IWildcard? wildcard
    )
    {
        Name = name;
        Location = location;
        _uses = uses;
        _references = references;
        _ownWildcard = wildcard;
        AttributeWildcard = wildcard;
    }

    private enum ResolveState
    {
        Unresolved = 0,
        Resolving = 1,
        Resolved = 2,
    }

    /// <inheritdoc />
    public XsdComponentKind Kind => XsdComponentKind.AttributeGroup;

    /// <inheritdoc />
    public XPathQName Name { get; }

    /// <inheritdoc />
    public DocumentLocation Location { get; }

    /// <summary>Gets the uses: the direct ones, and after <see cref="Resolve"/> those of the nested groups, in document order.</summary>
    public IReadOnlyList<AttributeUse> Uses => _uses;

    /// <summary>Gets the attribute wildcard, complete after <see cref="Resolve"/>; <see langword="null"/> for none.</summary>
    public IWildcard? AttributeWildcard { get; private set; }

    /// <summary>Defines a group with unresolved references.</summary>
    /// <param name="name">The expanded name.</param>
    /// <param name="location">The location of the definition.</param>
    /// <param name="uses">The direct uses in document order.</param>
    /// <param name="references">The nested group references in document order.</param>
    /// <param name="wildcard">The own attribute wildcard, <see langword="null"/> for none.</param>
    /// <returns>The definition.</returns>
    internal static AttributeGroupDefinition Create(
        XPathQName name,
        DocumentLocation location,
        IReadOnlyList<AttributeUse> uses,
        IReadOnlyList<AttributeGroupReference> references,
        IWildcard? wildcard
    )
    {
        ArgumentNullException.ThrowIfNull(uses);
        ArgumentNullException.ThrowIfNull(references);

        var directUses = new AttributeUse[uses.Count];
        for (var i = 0; i < directUses.Length; i++)
        {
            directUses[i] = uses[i];
        }

        var nested = new AttributeGroupReference[references.Count];
        for (var i = 0; i < nested.Length; i++)
        {
            nested[i] = references[i];
        }

        return new(name, location, directUses, nested, wildcard);
    }

    /// <summary>Resolves the nested references of this group, and of the groups it reaches, exactly once.</summary>
    /// <param name="registry">The registry that holds the groups.</param>
    /// <param name="reporter">The reporter for unresolved references, cycles, duplicate uses, and wildcard intersections.</param>
    internal void Resolve(IComponentRegistry registry, XsdIssueReporter reporter)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(reporter);

        if (_state != ResolveState.Unresolved)
        {
            return;
        }

        _state = ResolveState.Resolving;

        var all = new List<AttributeUse>(_uses.Length);
        var declarations = new Dictionary<XPathQName, AttributeDeclaration>();
        AddUses(_uses, all, declarations, reporter, direct: true);

        IWildcard? wildcard = null;
        var failed = false;
        for (var i = 0; i < _references.Length; i++)
        {
            var reference = _references[i];
            if (
                !registry.TryGet(XsdComponentKind.AttributeGroup, reference.Name, out var component)
                || component is not AttributeGroupDefinition nested
            )
            {
                reporter.Report(
                    XsdRuleCodes.SrcResolve,
                    reference.Location,
                    "The referenced attribute group is not declared."
                );
                continue;
            }

            if (nested._state == ResolveState.Resolving)
            {
                reporter.Report(
                    XsdRuleCodes.SrcAttributeGroup,
                    reference.Location,
                    "The attribute group reference is circular."
                );
                continue;
            }

            nested.Resolve(registry, reporter);
            AddUses(nested._uses, all, declarations, reporter, direct: false);
            Intersect(ref wildcard, ref failed, nested.AttributeWildcard, reporter);
        }

        Intersect(ref wildcard, ref failed, _ownWildcard, reporter);

        _uses = all.ToArray();
        AttributeWildcard = wildcard;
        _state = ResolveState.Resolved;
    }

    private static void AddUses(
        AttributeUse[] source,
        List<AttributeUse> all,
        Dictionary<XPathQName, AttributeDeclaration> declarations,
        XsdIssueReporter reporter,
        bool direct
    )
    {
        for (var i = 0; i < source.Length; i++)
        {
            var use = source[i];
            if (!declarations.TryGetValue(use.Name, out var existing))
            {
                declarations.Add(use.Name, use.Declaration);
                all.Add(use);
            }
            else if (direct || !ReferenceEquals(existing, use.Declaration))
            {
                reporter.Report(
                    XsdRuleCodes.AgPropsCorrect,
                    use.Location,
                    "Two attribute uses have the same name and target namespace."
                );
            }
        }
    }

    private void Intersect(ref IWildcard? wildcard, ref bool failed, IWildcard? next, XsdIssueReporter reporter)
    {
        if (next is null || failed)
        {
            return;
        }

        if (wildcard is null)
        {
            wildcard = next;
            return;
        }

        wildcard = wildcard.Intersect(next);
        if (wildcard is null)
        {
            reporter.Report(
                XsdRuleCodes.CosAwIntersect,
                Location,
                "The attribute wildcards of the group have no expressible intersection."
            );
            failed = true;
        }
    }
}
