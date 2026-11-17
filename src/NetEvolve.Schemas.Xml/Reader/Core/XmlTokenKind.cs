namespace NetEvolve.Schemas.Xml.Reader.Core;

/// <summary>The kinds of lexical tokens; the tokenizer knows no nesting, so an end tag is only a name.</summary>
internal enum XmlTokenKind
{
    /// <summary>The XML declaration: version slice, encoding slice, and the standalone flag in <c>Char0</c> (<c>y</c>, <c>n</c>, or none).</summary>
    Declaration = 0,

    /// <summary>The raw name after <c>&lt;</c>.</summary>
    StartTagName = 1,

    /// <summary>The raw name of an attribute; the equals sign and the opening quote are consumed by the following call.</summary>
    AttributeName = 2,

    /// <summary>The closing quote of an attribute value.</summary>
    AttributeValueEnd = 3,

    /// <summary>The <c>&gt;</c> of a start tag.</summary>
    StartTagEnd = 4,

    /// <summary>The <c>/&gt;</c> of an empty-element tag.</summary>
    EmptyTagEnd = 5,

    /// <summary>The raw name after <c>&lt;/</c>.</summary>
    EndTagName = 6,

    /// <summary>The <c>&gt;</c> of an end tag.</summary>
    EndTagEnd = 7,

    /// <summary>A literal run of character data, or of an attribute value.</summary>
    Text = 8,

    /// <summary>One normalized line break in content (inline <c>\n</c>).</summary>
    LineBreak = 9,

    /// <summary>A resolved character or predefined entity reference, or a normalized white space of an attribute value (inline characters).</summary>
    Reference = 10,

    /// <summary>A reference to another entity; only with an entity input stack. The slice is the name.</summary>
    EntityReference = 11,

    /// <summary>The <c>&lt;![CDATA[</c> opener.</summary>
    CDataStart = 12,

    /// <summary>A literal run, or an inline <c>\n</c>, of a CDATA section.</summary>
    CData = 13,

    /// <summary>A comment; the slice is the text without delimiters.</summary>
    Comment = 14,

    /// <summary>A processing instruction; the first slice is the target, the second the data without leading white space.</summary>
    ProcessingInstruction = 15,

    /// <summary>A document type declaration; only with a doctype handler. The parts are in <see cref="XmlTokenizer.LastDoctype"/>.</summary>
    Doctype = 16,
}
