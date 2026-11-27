namespace NetEvolve.Schemas.Xsd.SchemaDocuments;

using System;
using NetEvolve.Schemas.Xml.Text.CharClasses;

/// <summary>Checks the lexical form of schema attribute values; white space around and between tokens is collapsed first.</summary>
internal static class SchemaAttributeValues
{
    internal static bool IsValid(SchemaAttributeRule rule, string value)
    {
        var text = value.AsSpan().Trim(" \t\r\n".AsSpan());
        switch (rule.Kind)
        {
            case SchemaAttributeKind.Boolean:
                return text.SequenceEqual("true".AsSpan())
                    || text.SequenceEqual("false".AsSpan())
                    || text.SequenceEqual("1".AsSpan())
                    || text.SequenceEqual("0".AsSpan());
            case SchemaAttributeKind.NonEmpty:
                return !text.IsEmpty;
            case SchemaAttributeKind.XPathDefaultNamespace:
                // Limitation: xs:anyURI is checked laxly (#411), so the reserved ## space is the one value shape that is rejected.
                return !text.StartsWith("##".AsSpan(), StringComparison.Ordinal)
                    || text.SequenceEqual("##defaultNamespace".AsSpan())
                    || text.SequenceEqual("##targetNamespace".AsSpan())
                    || text.SequenceEqual("##local".AsSpan());
            case SchemaAttributeKind.NCName:
                return IsNCName(text);
            case SchemaAttributeKind.QName:
                return IsQName(text);
            case SchemaAttributeKind.UnsignedInteger:
                return IsUnsignedInteger(text);
            case SchemaAttributeKind.MaxOccurs:
                return text.SequenceEqual("unbounded".AsSpan()) || IsUnsignedInteger(text);
            case SchemaAttributeKind.Token:
                return Contains(rule.Tokens, text);
            case SchemaAttributeKind.QNameList:
                return AllTokens(text, rule, qNames: true);
            case SchemaAttributeKind.TokenList:
                return text.SequenceEqual("#all".AsSpan()) || AllTokens(text, rule, qNames: false);
            default:
                return true;
        }
    }

    private static bool AllTokens(ReadOnlySpan<char> text, SchemaAttributeRule rule, bool qNames)
    {
        while (!text.IsEmpty)
        {
            var end = 0;
            while (end < text.Length && !IsSpace(text[end]))
            {
                end++;
            }

            var token = text.Slice(0, end);
            if (!(qNames ? IsQName(token) : Contains(rule.Tokens, token)))
            {
                return false;
            }

            text = text.Slice(end);
            var skip = 0;
            while (skip < text.Length && IsSpace(text[skip]))
            {
                skip++;
            }

            text = text.Slice(skip);
        }

        return true;
    }

    private static bool IsSpace(char c) => c is ' ' or '\t' or '\r' or '\n';

    private static bool Contains(string[] tokens, ReadOnlySpan<char> token)
    {
        for (var i = 0; i < tokens.Length; i++)
        {
            if (token.SequenceEqual(tokens[i].AsSpan()))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsUnsignedInteger(ReadOnlySpan<char> text)
    {
        if (!text.IsEmpty && text[0] == '+')
        {
            text = text.Slice(1);
        }

        if (text.IsEmpty)
        {
            return false;
        }

        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] is < '0' or > '9')
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsQName(ReadOnlySpan<char> text)
    {
        var colon = text.IndexOf(':');
        return colon < 0 ? IsNCName(text) : IsNCName(text.Slice(0, colon)) && IsNCName(text.Slice(colon + 1));
    }

    private static bool IsNCName(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty)
        {
            return false;
        }

        for (var i = 0; i < text.Length; i++)
        {
            var first = i == 0;
            int codePoint = text[i];
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                codePoint = char.ConvertToUtf32(text[i], text[i + 1]);
                i++;
            }

            if (
                codePoint == ':'
                || !(first ? XmlCharClass.IsNameStartChar(codePoint) : XmlCharClass.IsNameChar(codePoint))
            )
            {
                return false;
            }
        }

        return true;
    }
}
