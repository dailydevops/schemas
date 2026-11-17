namespace NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core;

using System.Collections.Generic;

/// <summary>An element of the hand-built node model: name, namespace, start position, attributes, and content.</summary>
internal sealed class XmlElem
{
    internal XmlElem(string local, string namespaceUri, int line, int column, object[] content)
    {
        Local = local;
        NamespaceUri = namespaceUri;
        Line = line;
        Column = column;
        foreach (var item in content)
        {
            if (item is XmlAttr attribute)
            {
                Attributes.Add(attribute);
            }
            else
            {
                Content.Add(item);
            }
        }
    }

    internal string Local { get; }

    internal string NamespaceUri { get; }

    internal int Line { get; }

    internal int Column { get; }

    internal List<XmlAttr> Attributes { get; } = [];

    /// <summary>Gets the content: <see cref="XmlElem"/> values and <see cref="string"/> text.</summary>
    internal List<object> Content { get; } = [];
}
