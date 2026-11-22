namespace NetEvolve.Schemas.Xml.Dtd.Validation.Attributes;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;

/// <summary>The lexical rules of the attribute types of XML 1.0, section 3.3.1; the rules follow with the implementation.</summary>
internal static class DtdAttributeValues
{
    internal static bool Matches(DtdAttributeDefinition definition, ReadOnlySpan<char> value) =>
        throw new NotSupportedException();

    internal static bool TryNormalizeDefault(
        string literal,
        DtdAttributeType type,
        IDtdDeclarations declarations,
        XmlReaderLimits limits,
        out string value
    ) => throw new NotSupportedException();
}
