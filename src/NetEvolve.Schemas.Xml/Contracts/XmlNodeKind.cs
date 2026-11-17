namespace NetEvolve.Schemas.Xml.Contracts;

/// <summary>The node kinds of the XQuery 1.0 and XPath 2.0 Data Model.</summary>
public enum XmlNodeKind
{
    /// <summary>A document node.</summary>
    Document = 0,

    /// <summary>An element node.</summary>
    Element = 1,

    /// <summary>An attribute node.</summary>
    Attribute = 2,

    /// <summary>A text node; text and CDATA content merge into one node.</summary>
    Text = 3,

    /// <summary>A comment node.</summary>
    Comment = 4,

    /// <summary>A processing instruction node.</summary>
    ProcessingInstruction = 5,

    /// <summary>A namespace node, derived from the in-scope declarations of an element.</summary>
    Namespace = 6,
}
