namespace NetEvolve.Schemas.Xsd.Attributes.DefaultAttributes;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>The default attribute group of a schema document and its application to complex types.</summary>
internal static class DefaultAttributeRules
{
    /// <summary>Reads <c>defaultAttributesApply</c> of a complex type.</summary>
    /// <param name="value">The attribute value, <see langword="null"/> when absent.</param>
    /// <returns><see langword="true"/> unless the value is false.</returns>
    internal static bool Applies(string? value) => throw new NotSupportedException();

    /// <summary>Finds and resolves the group named by <c>defaultAttributes</c> of a schema element.</summary>
    /// <param name="schema">The schema element.</param>
    /// <param name="scope">The namespace scope of the document.</param>
    /// <param name="registry">The registry.</param>
    /// <param name="reporter">The reporter.</param>
    /// <returns>The group, <see langword="null"/> when the schema names none or an issue was reported.</returns>
    internal static AttributeGroupDefinition? Find(
        SchemaSyntaxNode schema,
        SchemaNamespaceScope scope,
        IComponentRegistry registry,
        XsdIssueReporter reporter
    ) => throw new NotSupportedException();

    /// <summary>Adds the default attribute group to the attributes of a complex type.</summary>
    /// <param name="defaultGroup">The resolved group, <see langword="null"/> for none.</param>
    /// <param name="applies">Whether <c>defaultAttributesApply</c> is true.</param>
    /// <param name="ownUses">The uses of the type.</param>
    /// <param name="ownWildcard">The attribute wildcard of the type.</param>
    /// <param name="location">The location of the type.</param>
    /// <param name="reporter">The reporter.</param>
    /// <param name="uses">The uses with those of the group.</param>
    /// <param name="wildcard">The attribute wildcard with that of the group.</param>
    /// <returns><see langword="false"/> after an issue was reported.</returns>
    internal static bool TryApply(
        AttributeGroupDefinition? defaultGroup,
        bool applies,
        IReadOnlyList<AttributeUse> ownUses,
        IWildcard? ownWildcard,
        DocumentLocation location,
        XsdIssueReporter reporter,
        out IReadOnlyList<AttributeUse> uses,
        out IWildcard? wildcard
    ) => throw new NotSupportedException();
}
