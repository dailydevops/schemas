namespace NetEvolve.Schemas.Xsd.Contracts;

using NetEvolve.Schemas.XPath.Api;

/// <summary>A located element of a parsed schema document, the input of the component builders.</summary>
internal interface ISchemaSyntaxElement
{
    /// <summary>Gets the expanded name of the element.</summary>
    XPathQName Name { get; }

    /// <summary>Gets the location of the start tag.</summary>
    DocumentLocation Location { get; }

    /// <summary>Gets the number of attributes, namespace declarations excluded.</summary>
    int AttributeCount { get; }

    /// <summary>Gets the number of child elements.</summary>
    int ChildCount { get; }

    /// <summary>Gets an attribute in document order.</summary>
    /// <param name="index">The zero-based index.</param>
    /// <returns>The attribute.</returns>
    XsdSyntaxAttribute GetAttribute(int index);

    /// <summary>Gets a child element in document order.</summary>
    /// <param name="index">The zero-based index.</param>
    /// <returns>The child.</returns>
    ISchemaSyntaxElement GetChild(int index);
}
