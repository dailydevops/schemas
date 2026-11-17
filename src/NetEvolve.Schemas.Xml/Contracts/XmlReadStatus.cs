namespace NetEvolve.Schemas.Xml.Contracts;

/// <summary>The outcome of one <see cref="IXmlReader.Pump{TSink}"/> call.</summary>
public enum XmlReadStatus
{
    /// <summary>The document ended and every event, including the end of the document, reached the sink.</summary>
    Completed = 0,

    /// <summary>The input ended inside a construct; the caller supplies more data and pumps again.</summary>
    NeedMoreData = 1,

    /// <summary>The input is not well formed or a limit was exceeded; <see cref="IXmlReader.Diagnostic"/> describes why.</summary>
    Error = 2,
}
