namespace NetEvolve.Schemas.Xml.Dtd.Validation.Attributes;

using System;
using System.Collections.Generic;
using System.Text;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Attributes;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Reader.References;
using NetEvolve.Schemas.Xml.Text.CharClasses;

/// <summary>
/// The lexical rules of the attribute types of XML 1.0, section 3.3.1 (Name, Nmtoken, the token lists, and membership in an
/// enumeration or a notation list), and the expansion of the literal of a default value to the value that an element
/// receives (section 3.3.3).
/// </summary>
/// <remarks>
/// The expansion follows the reader: a literal tab, line feed, or carriage return (CR LF counts once in the literal itself) becomes
/// one space, character references and the predefined entities are replaced, a reference to a declared internal general
/// entity is replaced by its replacement text, which is scanned again, and the declared type then trims and collapses the
/// spaces. A literal that the reader refuses (a <c>&lt;</c>, an undeclared, external, or unparsed entity, an illegal
/// reference, a recursion, or a value beyond the limits) does not expand; the reader reports it as a well-formedness error
/// when it applies the default, so the step reports no validity error for it.
/// </remarks>
internal static class DtdAttributeValues
{
    /// <summary>Tests production 5 for the whole value.</summary>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> when the value is one Name.</returns>
    internal static bool IsName(ReadOnlySpan<char> value) =>
        value.Length > 0 && XmlCharClass.NameLength(value) == value.Length;

    /// <summary>Tests production 7 (<c>Nmtoken</c>) for the whole value.</summary>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> when the value is one name token.</returns>
    internal static bool IsNmtoken(ReadOnlySpan<char> value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (!char.IsHighSurrogate(c))
            {
                if (!XmlCharClass.IsNameChar(c))
                {
                    return false;
                }

                continue;
            }

            // A valid pair is a name character up to U+EFFFF only (production 4a).
            if (i + 1 >= value.Length || !char.IsLowSurrogate(value[i + 1]) || c > 0xDB7F)
            {
                return false;
            }

            i++;
        }

        return true;
    }

    /// <summary>Takes the first token of a list that is separated by single spaces (productions 6 and 8).</summary>
    /// <param name="rest">The text that is left; it starts after the returned token on return.</param>
    /// <param name="last"><see langword="true"/> when the token is the last one.</param>
    /// <returns>The token, possibly empty.</returns>
    internal static ReadOnlySpan<char> NextToken(ref ReadOnlySpan<char> rest, out bool last)
    {
        var space = rest.IndexOf(' ');
        last = space < 0;
        var token = last ? rest : rest.Slice(0, space);
        rest = last ? default : rest.Slice(space + 1);

        return token;
    }

    /// <summary>Tests whether a value is one of the tokens or notation names of a definition, by ordinal comparison.</summary>
    /// <param name="values">The declared values.</param>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> when the value is declared.</returns>
    internal static bool Contains(IReadOnlyList<string> values, ReadOnlySpan<char> value)
    {
        for (var i = 0; i < values.Count; i++)
        {
            if (value.SequenceEqual(values[i].AsSpan()))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Tests whether a normalized value matches the lexical rules of the type of a definition.</summary>
    /// <param name="definition">The attribute definition.</param>
    /// <param name="value">The value after the normalization of its type.</param>
    /// <returns><see langword="true"/> when the value is a legal value of the type; CDATA accepts every value.</returns>
    internal static bool Matches(DtdAttributeDefinition definition, ReadOnlySpan<char> value)
    {
        Argument.ThrowIfNull(definition);

        switch (definition.Type)
        {
            case DtdAttributeType.Id:
            case DtdAttributeType.IdRef:
            case DtdAttributeType.Entity:
                return IsName(value);
            case DtdAttributeType.IdRefs:
            case DtdAttributeType.Entities:
                return AllTokens(value, names: true);
            case DtdAttributeType.NmToken:
                return IsNmtoken(value);
            case DtdAttributeType.NmTokens:
                return AllTokens(value, names: false);
            case DtdAttributeType.Notation:
            case DtdAttributeType.Enumeration:
                return Contains(definition.Values, value);
            default:
                return true;
        }
    }

    /// <summary>Expands the literal of a default value and normalizes the result by the declared type.</summary>
    /// <param name="literal">The literal as the declaration wrote it.</param>
    /// <param name="type">The declared type.</param>
    /// <param name="declarations">The declarations whose general entities the literal may refer to.</param>
    /// <param name="limits">The limits of the depth of the expansion and of the value length.</param>
    /// <param name="value">The value an element receives; empty when the literal does not expand.</param>
    /// <returns><see langword="true"/> when the literal expanded.</returns>
    internal static bool TryNormalizeDefault(
        string literal,
        DtdAttributeType type,
        IDtdDeclarations declarations,
        XmlReaderLimits limits,
        out string value
    )
    {
        Argument.ThrowIfNull(literal);
        Argument.ThrowIfNull(declarations);
        Argument.ThrowIfNull(limits);

        var builder = new StringBuilder();
        if (!Expand(literal.AsSpan(), isLiteral: true, declarations, limits, builder, 0))
        {
            value = string.Empty;

            return false;
        }

        if (type == DtdAttributeType.Cdata)
        {
            value = builder.ToString();

            return true;
        }

        var characters = builder.ToString().ToCharArray();
        value = new string(characters, 0, DtdAttributeNormalizer.Collapse(characters));

        return true;
    }

    // Production 6 and 8: at least one token, each of the kind, separated by single spaces.
    private static bool AllTokens(ReadOnlySpan<char> value, bool names)
    {
        var rest = value;
        bool last;
        do
        {
            var token = NextToken(ref rest, out last);
            if (!(names ? IsName(token) : IsNmtoken(token)))
            {
                return false;
            }
        } while (!last);

        return true;
    }

    private static bool Expand(
        ReadOnlySpan<char> text,
        bool isLiteral,
        IDtdDeclarations declarations,
        XmlReaderLimits limits,
        StringBuilder builder,
        int depth
    )
    {
        if (depth > limits.MaxEntityDepth)
        {
            return false;
        }

        var i = 0;
        while (i < text.Length)
        {
            if (builder.Length > limits.MaxAttributeValueLength)
            {
                return false;
            }

            var c = text[i];
            switch (c)
            {
                case '<':
                    return false;
                case '\t':
                case '\n':
                    builder.Append(' ');
                    i++;
                    break;
                case '\r':
                    // CR LF of the literal is one line end (section 2.11); in an entity value it was normalized when declared.
                    builder.Append(' ');
                    i += isLiteral && i + 1 < text.Length && text[i + 1] == '\n' ? 2 : 1;
                    break;
                case '&':
                    var length = ExpandReference(text.Slice(i), declarations, limits, builder, depth);
                    if (length < 0)
                    {
                        return false;
                    }

                    i += length;
                    break;
                default:
                    builder.Append(c);
                    i++;
                    break;
            }
        }

        return builder.Length <= limits.MaxAttributeValueLength;
    }

    // The length of the reference at the front of the text, -1 when it does not expand.
    private static int ExpandReference(
        ReadOnlySpan<char> text,
        IDtdDeclarations declarations,
        XmlReaderLimits limits,
        StringBuilder builder,
        int depth
    )
    {
        var error = XmlReferenceResolver.TryParseReference(text, out var length, out var codePoint);
        if (error is null)
        {
            builder.Append(char.ConvertFromUtf32(codePoint));

            return length;
        }

        if (
            !ReferenceEquals(error, XmlErrorCodes.XML0008)
            || !declarations.GeneralEntities.TryGetValue(text.Slice(1, length - 2).ToString(), out var entity)
            || entity.Value is not { } replacement
        )
        {
            return -1;
        }

        return Expand(replacement.AsSpan(), isLiteral: false, declarations, limits, builder, depth + 1) ? length : -1;
    }
}
