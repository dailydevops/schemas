namespace NetEvolve.Schemas.Xml.Dtd.Attributes;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Entities;
using NetEvolve.Schemas.Xml.Dtd.Model;

/// <summary>
/// Checks the entity references in the default values of the attribute definitions when the document type declaration is
/// complete (XML 1.0 sections 4.1 and 4.4.4): a default value must not refer to an external entity (<c>XML0022</c>) or to an
/// unparsed entity (<c>XML0021</c>), and an entity that no other declaration can supply must be declared (<c>XML0008</c>).
/// The constraints hold for the declaration, so they are checked whether or not an element ever uses the default.
/// </summary>
/// <remarks>
/// Limitations: the declaration order (a default value may refer only to an entity that an earlier declaration binds, section
/// 4.1, #878) is not checked, because the declaration model keeps no order; <c>standalone="yes"</c> is not known here, so an
/// undeclared entity is fatal only where no other declaration can exist, and the expansion applies the flag where a default is
/// used. The replacement text is not scanned here: a <c>&lt;</c> in it and a recursion are found when the default is applied.
/// </remarks>
internal static class DtdDefaultReferences
{
    /// <summary>Checks every default value of the declarations.</summary>
    /// <param name="declarations">The declarations of the document type declaration.</param>
    /// <returns>The code of the first violation in the order of the declarations, <see langword="null"/> when there is none.</returns>
    internal static XmlErrorCode? Validate(DtdDeclarations declarations)
    {
        ArgumentNullException.ThrowIfNull(declarations);

        GeneralEntityResolver? resolver = null;
        foreach (var list in declarations.AttributeLists.Values)
        {
            foreach (var definition in list)
            {
                if (definition.DefaultValue is not { } literal || literal.AsSpan().IndexOf('&') < 0)
                {
                    continue;
                }

                resolver ??= new GeneralEntityResolver(declarations);
                var error = ValidateLiteral(literal, resolver);
                if (error is not null)
                {
                    return error;
                }
            }
        }

        return null;
    }

    // The declaration parser has checked the syntax of every reference, so a reference is "&", an optional "#", and "...;".
    private static XmlErrorCode? ValidateLiteral(string literal, GeneralEntityResolver resolver)
    {
        var rest = literal.AsSpan();
        while (true)
        {
            var start = rest.IndexOf('&');
            if (start < 0)
            {
                return null;
            }

            var length = rest.Slice(start).IndexOf(';');
            var name = rest.Slice(start + 1, length - 1);
            if (name[0] != '#' && !IsPredefined(name))
            {
                var error = resolver.Resolve(name, inAttributeValue: true, standalone: false, out _);
                if (error is not null)
                {
                    return error;
                }
            }

            rest = rest.Slice(start + length + 1);
        }
    }

    private static bool IsPredefined(ReadOnlySpan<char> name) =>
        name.SequenceEqual("amp".AsSpan())
        || name.SequenceEqual("lt".AsSpan())
        || name.SequenceEqual("gt".AsSpan())
        || name.SequenceEqual("apos".AsSpan())
        || name.SequenceEqual("quot".AsSpan());
}
