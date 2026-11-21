namespace NetEvolve.Schemas.Xml.Reader.Core;

using System;
using NetEvolve.Schemas.Xml.Dtd.Attributes;

/// <summary>
/// The seam for the attribute definitions of the document type declaration (XML 1.0 sections 3.3.2 and 3.3.3). A doctype
/// handler that also implements it lets the parser add the defaulted attributes of a start tag and normalize the values by
/// their declared types; without it the parser leaves every attribute as written.
/// </summary>
internal interface IXmlAttributeDeclarations
{
    /// <summary>Finds the attribute definitions of an element type.</summary>
    /// <param name="elementName">The element name as written, with its prefix; valid only during the call.</param>
    /// <returns>The definitions, <see langword="null"/> when no attribute list declares the element type.</returns>
    DtdElementAttributes? Find(ReadOnlySpan<char> elementName);
}
