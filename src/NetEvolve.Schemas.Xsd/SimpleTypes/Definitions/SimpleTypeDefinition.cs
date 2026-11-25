namespace NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SimpleTypes.Checks;

/// <summary>
/// A simple type definition by restriction, list, or union (Structures section 3.14; Datatypes section 4.1). Immutable; the
/// factories take resolved components and report a violation of Simple Type Restriction (cos-st-restricts) as a located issue.
/// </summary>
/// <remarks>
/// Limitations: the base type of a list or union (<c>anySimpleType</c>) is not modeled, so <see cref="BaseType"/> is
/// <see langword="null"/> for them; <see cref="FromDatatype"/> cannot read item or member types from the datatype contract; own
/// facets are supplied as datatype facets, and checking that they restrict the base validly is XSD-54.
/// </remarks>
internal sealed class SimpleTypeDefinition : IXsdComponent
{
    private const XsdDerivationMethod SimpleMethods =
        XsdDerivationMethod.Restriction | XsdDerivationMethod.List | XsdDerivationMethod.Union;

    private static readonly IReadOnlyList<SimpleTypeDefinition> NoMembers = [];

    private SimpleTypeDefinition(
        XPathQName name,
        DocumentLocation location,
        XsdDerivationMethod final,
        DatatypeVariety variety,
        SimpleTypeDefinition? baseType,
        SimpleTypeDefinition? itemType,
        IReadOnlyList<SimpleTypeDefinition> memberTypes,
        IReadOnlyList<IFacet> facets,
        IDatatypeDescriptor? datatype = null,
        int builtInFacets = 0
    )
    {
        Name = name;
        Location = location;
        Final = final & SimpleMethods;
        Variety = variety;
        BaseType = baseType;
        ItemType = itemType;
        MemberTypes = memberTypes;
        Facets = facets;
        Datatype = datatype;
        BuiltInFacets = builtInFacets;
        Validator =
            baseType?.Validator is { } inherited && facets.Count == baseType.Facets.Count
                ? inherited
                : SimpleTypeValidator.Create(
                    name == default ? "anonymous" : name.LocalName,
                    variety,
                    datatype,
                    facets,
                    builtInFacets
                );
    }

    /// <inheritdoc />
    public XsdComponentKind Kind => XsdComponentKind.SimpleType;

    /// <inheritdoc />
    public XPathQName Name { get; }

    /// <inheritdoc />
    public DocumentLocation Location { get; }

    /// <summary>Gets a value indicating whether the type has no name.</summary>
    public bool IsAnonymous => Name == default;

    /// <summary>Gets the methods that may not derive further types from this one.</summary>
    public XsdDerivationMethod Final { get; }

    /// <summary>Gets the variety.</summary>
    public DatatypeVariety Variety { get; }

    /// <summary>Gets the base type, <see langword="null"/> for a built-in, list, or union.</summary>
    public SimpleTypeDefinition? BaseType { get; }

    /// <summary>Gets the item type of a list variety, <see langword="null"/> otherwise.</summary>
    public SimpleTypeDefinition? ItemType { get; }

    /// <summary>Gets the member types of a union variety in declaration order, empty otherwise.</summary>
    public IReadOnlyList<SimpleTypeDefinition> MemberTypes { get; }

    /// <summary>Gets the facets: those inherited from the base type, then the own ones.</summary>
    public IReadOnlyList<IFacet> Facets { get; }

    /// <summary>Gets the compiled validator of the facets this type adds to its built-in root, <see langword="null"/> when there are none or they cannot be compiled.</summary>
    internal SimpleTypeValidator? Validator { get; }

    private IDatatypeDescriptor? Datatype { get; }

    private int BuiltInFacets { get; }

    /// <summary>Wraps a datatype of the Xml package, for example a built-in, as a component.</summary>
    /// <param name="name">The expanded name.</param>
    /// <param name="location">The location.</param>
    /// <param name="datatype">The datatype that supplies variety and facets.</param>
    /// <param name="final">The final set.</param>
    /// <returns>The definition.</returns>
    internal static SimpleTypeDefinition FromDatatype(
        XPathQName name,
        DocumentLocation location,
        IDatatypeDescriptor datatype,
        XsdDerivationMethod final = XsdDerivationMethod.None
    )
    {
        ArgumentNullException.ThrowIfNull(datatype);

        return new(
            name,
            location,
            final,
            datatype.Variety,
            null,
            null,
            NoMembers,
            datatype.Facets,
            datatype,
            datatype.Facets.Count
        );
    }

    /// <summary>Defines a type by restriction; variety, item type, and member types come from the base type.</summary>
    /// <param name="name">The expanded name, <see langword="default"/> when anonymous.</param>
    /// <param name="location">The location of the declaration.</param>
    /// <param name="final">The final set.</param>
    /// <param name="baseType">The resolved base type.</param>
    /// <param name="facets">The own facets, added after the inherited ones.</param>
    /// <param name="reporter">The reporter.</param>
    /// <returns>The definition, <see langword="null"/> after an issue was reported.</returns>
    internal static SimpleTypeDefinition? Restrict(
        XPathQName name,
        DocumentLocation location,
        XsdDerivationMethod final,
        IXsdComponent baseType,
        IReadOnlyList<IFacet> facets,
        XsdIssueReporter reporter
    )
    {
        ArgumentNullException.ThrowIfNull(baseType);
        ArgumentNullException.ThrowIfNull(facets);
        ArgumentNullException.ThrowIfNull(reporter);

        if (baseType is not SimpleTypeDefinition parent)
        {
            Report(reporter, location, "The base type of a restriction must be a simple type.");
            return null;
        }

        if ((parent.Final & XsdDerivationMethod.Restriction) != XsdDerivationMethod.None)
        {
            Report(reporter, location, "The base type is final for restriction.");
            return null;
        }

        return new(
            name,
            location,
            final,
            parent.Variety,
            parent,
            parent.ItemType,
            parent.MemberTypes,
            Combine(parent.Facets, facets),
            parent.Datatype,
            parent.BuiltInFacets
        );
    }

    /// <summary>Defines a list type.</summary>
    /// <param name="name">The expanded name, <see langword="default"/> when anonymous.</param>
    /// <param name="location">The location of the declaration.</param>
    /// <param name="final">The final set.</param>
    /// <param name="itemType">The resolved item type: atomic, or a union of atomic types.</param>
    /// <param name="facets">The own facets.</param>
    /// <param name="reporter">The reporter.</param>
    /// <returns>The definition, <see langword="null"/> after an issue was reported.</returns>
    internal static SimpleTypeDefinition? List(
        XPathQName name,
        DocumentLocation location,
        XsdDerivationMethod final,
        IXsdComponent itemType,
        IReadOnlyList<IFacet> facets,
        XsdIssueReporter reporter
    )
    {
        ArgumentNullException.ThrowIfNull(itemType);
        ArgumentNullException.ThrowIfNull(facets);
        ArgumentNullException.ThrowIfNull(reporter);

        if (itemType is not SimpleTypeDefinition item)
        {
            Report(reporter, location, "The item type of a list must be a simple type.");
            return null;
        }

        if (item.ContainsList())
        {
            Report(reporter, location, "The item type of a list must be atomic or a union of atomic types.");
            return null;
        }

        if ((item.Final & XsdDerivationMethod.List) != XsdDerivationMethod.None)
        {
            Report(reporter, location, "The item type is final for list.");
            return null;
        }

        return new(name, location, final, DatatypeVariety.List, null, item, NoMembers, Combine(null, facets));
    }

    /// <summary>Defines a union type.</summary>
    /// <param name="name">The expanded name, <see langword="default"/> when anonymous.</param>
    /// <param name="location">The location of the declaration.</param>
    /// <param name="final">The final set.</param>
    /// <param name="memberTypes">The resolved member types in declaration order.</param>
    /// <param name="facets">The own facets.</param>
    /// <param name="reporter">The reporter.</param>
    /// <returns>The definition, <see langword="null"/> after an issue was reported.</returns>
    internal static SimpleTypeDefinition? Union(
        XPathQName name,
        DocumentLocation location,
        XsdDerivationMethod final,
        IReadOnlyList<IXsdComponent> memberTypes,
        IReadOnlyList<IFacet> facets,
        XsdIssueReporter reporter
    )
    {
        ArgumentNullException.ThrowIfNull(memberTypes);
        ArgumentNullException.ThrowIfNull(facets);
        ArgumentNullException.ThrowIfNull(reporter);

        var members = new SimpleTypeDefinition[memberTypes.Count];
        for (var i = 0; i < members.Length; i++)
        {
            if (memberTypes[i] is not SimpleTypeDefinition member)
            {
                Report(reporter, location, "Every member type of a union must be a simple type.");
                return null;
            }

            if ((member.Final & XsdDerivationMethod.Union) != XsdDerivationMethod.None)
            {
                Report(reporter, location, "A member type is final for union.");
                return null;
            }

            members[i] = member;
        }

        return new(name, location, final, DatatypeVariety.Union, null, null, members, Combine(null, facets));
    }

    private static IFacet[] Combine(IReadOnlyList<IFacet>? inherited, IReadOnlyList<IFacet> own)
    {
        var inheritedCount = inherited?.Count ?? 0;
        var all = new IFacet[inheritedCount + own.Count];
        for (var i = 0; i < inheritedCount; i++)
        {
            all[i] = inherited![i];
        }

        for (var i = 0; i < own.Count; i++)
        {
            all[inheritedCount + i] = own[i];
        }

        return all;
    }

    private static void Report(XsdIssueReporter reporter, DocumentLocation location, string message)
    {
        foreach (var rule in XsdRuleCatalog.All)
        {
            if (rule.Code == XsdRuleCodes.CosStRestricts)
            {
                reporter.Report(rule, location, message);
                return;
            }
        }
    }

    private bool ContainsList()
    {
        if (Variety == DatatypeVariety.List)
        {
            return true;
        }

        for (var i = 0; i < MemberTypes.Count; i++)
        {
            if (MemberTypes[i].ContainsList())
            {
                return true;
            }
        }

        return false;
    }
}
