namespace NetEvolve.Schemas.Xml.Contracts;

/// <summary>The origin of a <see cref="IXmlSink.Text"/> event; consecutive text events of any kind form one text node.</summary>
public enum XmlTextKind
{
    /// <summary>Character data.</summary>
    Text = 0,

    /// <summary>The content of a CDATA section; kept so a writer can re-emit the section.</summary>
    CData = 1,

    /// <summary>A resolved character or predefined entity reference, or a normalized line break.</summary>
    Reference = 2,
}
