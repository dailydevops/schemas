#pragma warning disable CA1028 // The approved contract fixes byte as the underlying type.

namespace NetEvolve.Schemas.Resources;

/// <summary>The kind of an external resource, so a resolver can allow entities but deny schemas.</summary>
public enum ResourceKind : byte
{
    /// <summary>External DTD subset.</summary>
    ExternalSubset = 0,

    /// <summary>External general entity.</summary>
    ExternalEntity = 1,

    /// <summary>External parameter entity.</summary>
    ExternalParameterEntity = 2,

    /// <summary>Schema document reached by an import or include.</summary>
    SchemaDocument = 3,

    /// <summary>Schema document reached by a location hint.</summary>
    SchemaLocationHint = 4,

    /// <summary>Document loaded by an XPath function.</summary>
    XPathDocument = 5,

    /// <summary>Text loaded by an XPath function.</summary>
    XPathText = 6,
}
