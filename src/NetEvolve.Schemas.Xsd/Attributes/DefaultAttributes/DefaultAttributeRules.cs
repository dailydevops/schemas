namespace NetEvolve.Schemas.Xsd.Attributes.DefaultAttributes;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>
/// The default attribute group of a schema document and its application to complex types (Structures 1.1, 3.4.2.3.1: the
/// <c>defaultAttributes</c> attribute of <c>schema</c> and the <c>defaultAttributesApply</c> attribute of
/// <c>complexType</c>).
/// </summary>
/// <remarks>
/// <para>
/// The group acts as one more <c>attributeGroup</c> reference at the end of the attribute children of a complex type, whatever
/// its content: an implicit one, a <c>complexContent</c> or <c>simpleContent</c> derivation by extension or restriction. The
/// attribute uses of the group are added after those of the type, and the attribute wildcard of the type is intersected with
/// that of the group; duplicate names are reported by the complex type, as for any attribute group.
/// </para>
/// <para>
/// Decision on #306: <c>defaultAttributes</c> is read from the <c>schema</c> element of the document that holds the complex
/// type, as the standard says, so a document that is included or imported does not inherit the default of the including
/// one. Limitations: the pipeline that reads the attributes and hands the results to the factories of
/// <see cref="ComplexTypes.Definitions.ComplexTypeDefinition"/> is XSD-78; the intersection of wildcards of different
/// implementations is that of <see cref="IWildcard"/>.
/// </para>
/// </remarks>
internal static class DefaultAttributeRules
{
    /// <summary>Reads the <c>defaultAttributesApply</c> attribute of a complex type.</summary>
    /// <param name="value">The attribute value, <see langword="null"/> when absent.</param>
    /// <returns><see langword="true"/> unless the value is false: the attribute defaults to true.</returns>
    internal static bool Applies(string? value) =>
        value is null
        || string.Equals(value, "true", StringComparison.Ordinal)
        || string.Equals(value, "1", StringComparison.Ordinal);

    /// <summary>Finds and resolves the attribute group that the <c>defaultAttributes</c> attribute of a schema element names.</summary>
    /// <param name="schema">The <c>schema</c> element.</param>
    /// <param name="scope">The namespace scope of the document.</param>
    /// <param name="registry">The registry that holds the attribute groups.</param>
    /// <param name="reporter">The reporter for a group that is not declared, and for the issues of resolving the group.</param>
    /// <returns>The resolved group, <see langword="null"/> when the schema names none or an issue was reported.</returns>
    internal static AttributeGroupDefinition? Find(
        SchemaSyntaxNode schema,
        SchemaNamespaceScope scope,
        IComponentRegistry registry,
        XsdIssueReporter reporter
    )
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(reporter);

        var value = schema.FindAttribute("defaultAttributes");
        if (value is null)
        {
            return null;
        }

        if (
            !scope.TryResolve(schema, value, out var name)
            || !registry.TryGet(XsdComponentKind.AttributeGroup, name, out var component)
            || component is not AttributeGroupDefinition group
        )
        {
            reporter.Report(
                XsdRuleLookup.SrcResolve,
                schema.Location,
                "The default attribute group '" + value + "' is not declared."
            );
            return null;
        }

        group.Resolve(registry, reporter);
        return group;
    }

    /// <summary>Adds the default attribute group to the attributes of a complex type.</summary>
    /// <param name="defaultGroup">The resolved group, <see langword="null"/> when the schema has none.</param>
    /// <param name="applies">Whether <c>defaultAttributesApply</c> of the type is true, see <see cref="Applies"/>.</param>
    /// <param name="ownUses">The attribute uses of the type, in document order.</param>
    /// <param name="ownWildcard">The complete attribute wildcard of the type, <see langword="null"/> for none.</param>
    /// <param name="location">The location of the type, for issues.</param>
    /// <param name="reporter">The reporter.</param>
    /// <param name="uses">The uses of the type followed by those of the group.</param>
    /// <param name="wildcard">The wildcard of the type intersected with that of the group.</param>
    /// <returns><see langword="false"/> after an issue was reported; <paramref name="wildcard"/> is then that of the type.</returns>
    internal static bool TryApply(
        AttributeGroupDefinition? defaultGroup,
        bool applies,
        IReadOnlyList<AttributeUse> ownUses,
        IWildcard? ownWildcard,
        DocumentLocation location,
        XsdIssueReporter reporter,
        out IReadOnlyList<AttributeUse> uses,
        out IWildcard? wildcard
    )
    {
        ArgumentNullException.ThrowIfNull(ownUses);
        ArgumentNullException.ThrowIfNull(reporter);

        uses = ownUses;
        wildcard = ownWildcard;
        if (!applies || defaultGroup is null)
        {
            return true;
        }

        var all = new List<AttributeUse>(ownUses.Count + defaultGroup.Uses.Count);
        for (var i = 0; i < ownUses.Count; i++)
        {
            all.Add(ownUses[i]);
        }

        for (var i = 0; i < defaultGroup.Uses.Count; i++)
        {
            all.Add(defaultGroup.Uses[i]);
        }

        uses = all.ToArray();
        var fromGroup = defaultGroup.AttributeWildcard;
        if (fromGroup is null)
        {
            return true;
        }

        if (ownWildcard is null)
        {
            wildcard = fromGroup;
            return true;
        }

        var intersection = ownWildcard.Intersect(fromGroup);
        if (intersection is null)
        {
            reporter.Report(
                XsdRuleLookup.Find(XsdRuleCodes.CosAwIntersect),
                location,
                "The attribute wildcards of the type and of the default attribute group have no expressible intersection."
            );
            return false;
        }

        wildcard = intersection;
        return true;
    }
}
