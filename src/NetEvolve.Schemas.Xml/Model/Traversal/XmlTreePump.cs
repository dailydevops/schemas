namespace NetEvolve.Schemas.Xml.Model.Traversal;

using System;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>Pumps a document or an element subtree of the model into a sink.</summary>
public static class XmlTreePump
{
    /// <summary>Pumps a node and its descendants into a class sink.</summary>
    /// <param name="node">The document, element, text, comment or processing instruction node.</param>
    /// <param name="sink">The sink.</param>
    public static void Pump(XmlNode node, IXmlSink sink) => Pump(node, ref sink);

    /// <summary>Pumps a node and its descendants into a class sink or a mutable struct sink.</summary>
    /// <typeparam name="TSink">The sink type.</typeparam>
    /// <param name="node">The document, element, text, comment or processing instruction node.</param>
    /// <param name="sink">The sink, passed by reference so a struct sink keeps its state.</param>
    public static void Pump<TSink>(XmlNode node, ref TSink sink)
        where TSink : IXmlSink => throw new NotSupportedException();
}
