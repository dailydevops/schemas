namespace NetEvolve.Schemas.XPath.Evaluation.Axes;

/// <summary>The shapes of a node test; a name test is an element or attribute test chosen by the principal node kind of its axis.</summary>
internal enum XPathNodeTestKind
{
    /// <summary><c>node()</c></summary>
    AnyNode = 0,

    /// <summary><c>text()</c></summary>
    Text = 1,

    /// <summary><c>comment()</c></summary>
    Comment = 2,

    /// <summary><c>processing-instruction()</c>, the target in the local part</summary>
    ProcessingInstruction = 3,

    /// <summary><c>document-node()</c></summary>
    Document = 4,

    /// <summary><c>element()</c> or a name test on an element axis</summary>
    Element = 5,

    /// <summary><c>attribute()</c> or a name test on the attribute axis</summary>
    Attribute = 6,
}
