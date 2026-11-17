namespace NetEvolve.Schemas.Xml.Contracts;

/// <summary>The kinds of events a reader pushes into an <see cref="IXmlSink"/>, one per sink member except <c>Names</c>.</summary>
public enum XmlEventKind
{
    /// <summary><see cref="IXmlSink.StartDocument"/>.</summary>
    StartDocument = 0,

    /// <summary><see cref="IXmlSink.EndDocument"/>.</summary>
    EndDocument = 1,

    /// <summary><see cref="IXmlSink.StartElement"/>.</summary>
    StartElement = 2,

    /// <summary><see cref="IXmlSink.NamespaceDeclaration"/>.</summary>
    NamespaceDeclaration = 3,

    /// <summary><see cref="IXmlSink.Attribute"/>.</summary>
    Attribute = 4,

    /// <summary><see cref="IXmlSink.EndAttributes"/>.</summary>
    EndAttributes = 5,

    /// <summary><see cref="IXmlSink.EndElement"/>.</summary>
    EndElement = 6,

    /// <summary><see cref="IXmlSink.Text"/>.</summary>
    Text = 7,

    /// <summary><see cref="IXmlSink.Comment"/>.</summary>
    Comment = 8,

    /// <summary><see cref="IXmlSink.ProcessingInstruction"/>.</summary>
    ProcessingInstruction = 9,

    /// <summary><see cref="IXmlSink.Doctype"/>.</summary>
    Doctype = 10,
}
