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
/// Limitations: only the internal subset is known, so an undeclared entity is a well-formedness error exactly when
/// section 4.1 says no other declaration can exist (no DTD, <c>standalone="yes"</c>, or an internal subset without a
/// parameter-entity reference and without an external subset); otherwise the reference is skipped and the validity
/// constraint Entity Declared is left to the validation steps (the skipped reference raises no event yet, #647).
/// The name lookup allocates once per document type declaration and nothing per reference.
/// </remarks>
internal sealed class GeneralEntityResolver
{
    private readonly DtdDeclarations? _declarations;
    private readonly SpanInterningNameTable? _names;

    /// <summary>Initializes a new instance of the <see cref="GeneralEntityResolver"/> class.</summary>
    /// <param name="declarations">The declarations, <see langword="null"/> for a document without a document type declaration.</param>
    internal GeneralEntityResolver(DtdDeclarations? declarations)
    {
        _declarations = declarations;
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
    /// <param name="entity">The internal entity to expand; <see langword="null"/> when the result is an error or the reference is skipped.</param>
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
        if (key is null || !_declarations!.GeneralEntities.TryGetValue(key, out var declared))
        {
            return IsUndeclaredFatal(standalone) ? XmlErrorCodes.XML0008 : null;
        }

        if (declared.NotationName is not null)
        {
            return XmlErrorCodes.XML0021;
        }

        if (declared.Value is null)
        {
            return inAttributeValue ? XmlErrorCodes.XML0022 : XmlErrorCodes.XML2002;
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
