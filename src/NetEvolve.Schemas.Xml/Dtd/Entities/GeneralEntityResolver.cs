namespace NetEvolve.Schemas.Xml.Dtd.Entities;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Text.NameTable;

/// <summary>
/// Looks up a general entity reference in the declarations of a document and applies the well-formedness constraints of
/// XML 1.0 (Fifth Edition) sections 4.1 and 4.4: Entity Declared, Parsed Entity, and No External Entity References.
/// </summary>
/// <remarks>
/// An undeclared entity is a well-formedness error exactly when section 4.1 says no other declaration can exist (no DTD,
/// <c>standalone="yes"</c>, or an internal subset without a parameter-entity reference and without an external subset);
/// with <c>standalone="yes"</c> an entity that is declared in the external subset or in an external parameter entity does
/// not count as declared. Otherwise the reference is skipped and the validity constraint Entity Declared is left to the
/// validation steps (the skipped reference raises no event yet, #647). Limitation: an entity that is declared in the
/// replacement text of an internal parameter entity counts as declared with <c>standalone="yes"</c>, although section 4.1
/// excludes parameter entities.
/// An external parsed entity in content is handed back for the caller to load when the resolver is created for external
/// content (XML-62); otherwise it is <c>XML2002</c>, and in an attribute value it is always <c>XML0022</c>.
/// The name lookup allocates once per document type declaration and nothing per reference.
/// </remarks>
internal sealed class GeneralEntityResolver
{
    private readonly DtdDeclarations? _declarations;
    private readonly SpanInterningNameTable? _names;
    private readonly bool _externalContent;

    /// <summary>Initializes a new instance of the <see cref="GeneralEntityResolver"/> class.</summary>
    /// <param name="declarations">The declarations, <see langword="null"/> for a document without a document type declaration.</param>
    /// <param name="externalContent"><see langword="true"/> when the caller loads an external parsed entity that is referenced in content, <see langword="false"/> to refuse it with <c>XML2002</c>.</param>
    internal GeneralEntityResolver(DtdDeclarations? declarations, bool externalContent = false)
    {
        _declarations = declarations;
        _externalContent = externalContent;
        if (declarations is null || declarations.GeneralEntities.Count == 0)
        {
            return;
        }

        // The table answers "which declared name is this span" without allocating a string per reference.
        _names = new SpanInterningNameTable(
            declarations.GeneralEntities.Count + 1,
            declarations.GeneralEntities.Count + 1
        );
        foreach (var declared in declarations.GeneralEntities.Keys)
        {
            _ = _names.TryIntern(declared.AsSpan(), out _);
        }
    }

    /// <summary>Resolves a reference to an entity that is not predefined.</summary>
    /// <param name="name">The entity name without the ampersand and the semicolon.</param>
    /// <param name="inAttributeValue">Whether the reference stands in an attribute value.</param>
    /// <param name="standalone">Whether the XML declaration says <c>standalone="yes"</c>.</param>
    /// <param name="entity">The entity to expand: an internal one, or an external parsed one for the caller to load when the value is <see langword="null"/>; <see langword="null"/> when the result is an error or the reference is skipped.</param>
    /// <returns>The error code, or <see langword="null"/> when the reference is accepted (expanded when <paramref name="entity"/> is set, otherwise skipped).</returns>
    internal XmlErrorCode? Resolve(
        ReadOnlySpan<char> name,
        bool inAttributeValue,
        bool standalone,
        out DtdEntityDeclaration? entity
    )
    {
        entity = null;
        var key = _names?.Lookup(name);
        if (
            key is null
            || !_declarations!.GeneralEntities.TryGetValue(key, out var declared)
            || (standalone && declared.InExternalText)
        )
        {
            return IsUndeclaredFatal(standalone) ? XmlErrorCodes.XML0008 : null;
        }

        if (declared.NotationName is not null)
        {
            return XmlErrorCodes.XML0021;
        }

        if (declared.Value is null)
        {
            if (inAttributeValue)
            {
                return XmlErrorCodes.XML0022;
            }

            if (!_externalContent)
            {
                return XmlErrorCodes.XML2002;
            }
        }

        entity = declared;
        return null;
    }

    // Section 4.1: the constraint is well-formedness only when no declaration outside the internal subset can exist.
    private bool IsUndeclaredFatal(bool standalone) =>
        _declarations is null
        || standalone
        || (!_declarations.HasExternalSubset && !_declarations.HasParameterEntityReferences);
}
