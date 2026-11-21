namespace NetEvolve.Schemas.Xml.Contracts;

using NetEvolve.Schemas.Xml.Dtd.Contracts;

/// <summary>
/// An optional extension of <see cref="IXmlSink"/> for a sink that wants the declaration facts of every attribute: the
/// declared type, whether the document specified the attribute, and its position. The reader reports them only when DTD
/// processing is on (<see cref="XmlReaderOptions.DtdProcessing"/>).
/// </summary>
/// <remarks>
/// <para>
/// The reader calls <see cref="AttributeInfo"/> right after the <see cref="IXmlSink.Attribute"/> event of the attribute, and
/// right after the <see cref="IXmlSink.NamespaceDeclaration"/> event for a namespace declaration, in the order of those
/// events. A defaulted attribute (section 3.3.2) is reported like a specified one, with <see cref="ValidationAttributeInfo.IsSpecified"/>
/// set to <see langword="false"/>; an attribute that no declaration covers has no
/// <see cref="ValidationAttributeInfo.DeclaredType"/>. The memory in the argument is valid only during the call.
/// </para>
/// <para>
/// Limitations: only a sink of a reference type is offered the event, because the reader reaches the optional interface
/// without boxing and a boxed copy of a struct sink would lose its changes (#879); a struct sink receives the events of
/// <see cref="IXmlSink"/> only. <see cref="NetEvolve.Schemas.Xml.Model.Traversal.XmlTreePump"/> does not report the event for a document model (#880). The flag that
/// tells whether the normalization by the declared type changed the value stays internal until the validating pipeline (XML-68)
/// settles the contract.
/// </para>
/// </remarks>
public interface IXmlAttributeInfoSink : IXmlSink
{
    /// <summary>Reports the declaration facts of the attribute or namespace declaration that the sink received last.</summary>
    /// <param name="attribute">The attribute: the name as written, the normalized value, the declared type, the specified flag, and the position.</param>
    void AttributeInfo(in ValidationAttributeInfo attribute);
}
