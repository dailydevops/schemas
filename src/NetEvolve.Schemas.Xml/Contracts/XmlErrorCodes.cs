namespace NetEvolve.Schemas.Xml.Contracts;

using System.Collections.Generic;

/// <summary>
/// The error code table of the XML package: the catalog approved in the XML core design (XML0001 to XML4003 with the
/// limit codes XML1101 to XML1116), each with an explicit <c>NES</c> constant, one constraint name, and a message.
/// </summary>
/// <remarks>
/// The <c>NES</c> number is <c>1000 + (100 * first digit) + last two digits</c> of the alias, so every code lies in
/// <see cref="DiagnosticRanges.Xml"/>. Codes may be added, never renumbered. Limitation: all well-formedness errors are
/// fatal; XML 1.1 and DTD validity codes are not part of this table (XML-60).
/// </remarks>
public static class XmlErrorCodes
{
    // Declared before the code fields: their initializers register into it.
    private static readonly List<XmlErrorCode> Table = [];

    /// <summary>Unexpected end of input.</summary>
    public static readonly XmlErrorCode XML0001 = Define(
        "XML0001",
        1001,
        "Production: document",
        "The input ended inside a construct."
    );

    /// <summary>Illegal character.</summary>
    public static readonly XmlErrorCode XML0002 = Define(
        "XML0002",
        1002,
        "Production: Char",
        "The input contains a character that is not allowed in XML."
    );

    /// <summary>Malformed XML declaration structure.</summary>
    public static readonly XmlErrorCode XML0003 = Define(
        "XML0003",
        1003,
        "Production: XMLDecl",
        "The XML declaration is malformed."
    );

    /// <summary>The sequence <c>]]&gt;</c> in character data.</summary>
    public static readonly XmlErrorCode XML0004 = Define(
        "XML0004",
        1004,
        "Production: CharData",
        "The sequence ']]>' is not allowed in character data."
    );

    /// <summary>End tag does not match the open element.</summary>
    public static readonly XmlErrorCode XML0005 = Define(
        "XML0005",
        1005,
        "WFC: Element Type Match",
        "The end tag does not match the name of the open element."
    );

    /// <summary>Duplicate attribute name.</summary>
    public static readonly XmlErrorCode XML0006 = Define(
        "XML0006",
        1006,
        "WFC: Unique Att Spec",
        "An attribute name appears more than once in a start tag."
    );

    /// <summary>XML declaration anywhere but at the start of the document.</summary>
    public static readonly XmlErrorCode XML0007 = Define(
        "XML0007",
        1007,
        "Production: prolog",
        "The XML declaration is only allowed at the very start of the document."
    );

    /// <summary>Reference to an entity other than the five predefined ones.</summary>
    public static readonly XmlErrorCode XML0008 = Define(
        "XML0008",
        1008,
        "WFC: Entity Declared",
        "The entity is not declared; only the five predefined entities are known."
    );

    /// <summary>Invalid character reference.</summary>
    public static readonly XmlErrorCode XML0009 = Define(
        "XML0009",
        1009,
        "WFC: Legal Character",
        "The character reference does not denote a legal XML character."
    );

    /// <summary>Unsupported XML version.</summary>
    public static readonly XmlErrorCode XML0010 = Define(
        "XML0010",
        1010,
        "Production: VersionInfo",
        "The XML version is not supported; only 1.0 is."
    );

    /// <summary>Syntax error where no more specific code applies.</summary>
    public static readonly XmlErrorCode XML0011 = Define(
        "XML0011",
        1011,
        "Production: document",
        "The input is not well formed."
    );

    /// <summary>Missing root element or more than one root element.</summary>
    public static readonly XmlErrorCode XML0012 = Define(
        "XML0012",
        1012,
        "Production: document",
        "A document must contain exactly one root element."
    );

    /// <summary>The character <c>&lt;</c> in an attribute value.</summary>
    public static readonly XmlErrorCode XML0013 = Define(
        "XML0013",
        1013,
        "WFC: No < in Attribute Values",
        "The character '<' is not allowed in an attribute value."
    );

    /// <summary>The sequence <c>--</c> inside a comment.</summary>
    public static readonly XmlErrorCode XML0014 = Define(
        "XML0014",
        1014,
        "Production: Comment",
        "The sequence '--' is not allowed inside a comment."
    );

    /// <summary>Reserved processing instruction target.</summary>
    public static readonly XmlErrorCode XML0015 = Define(
        "XML0015",
        1015,
        "Production: PITarget",
        "The processing instruction target 'xml' is reserved."
    );

    /// <summary>Malformed reference syntax.</summary>
    public static readonly XmlErrorCode XML0016 = Define(
        "XML0016",
        1016,
        "Production: Reference",
        "The reference is malformed."
    );

    /// <summary>Invalid <c>encoding</c> or <c>standalone</c> value in the XML declaration.</summary>
    public static readonly XmlErrorCode XML0017 = Define(
        "XML0017",
        1017,
        "Production: EncodingDecl",
        "The encoding or standalone value of the XML declaration is invalid."
    );

    /// <summary>Character data or a reference outside the root element.</summary>
    public static readonly XmlErrorCode XML0018 = Define(
        "XML0018",
        1018,
        "Production: document",
        "Character data is not allowed outside the root element."
    );

    /// <summary><c>MaxDocumentCharacters</c> exceeded.</summary>
    public static readonly XmlErrorCode XML1101 = Define(
        "XML1101",
        1101,
        "Limit: MaxDocumentCharacters",
        "The document exceeds the maximum number of characters.",
        1
    );

    /// <summary><c>MaxElementDepth</c> exceeded.</summary>
    public static readonly XmlErrorCode XML1102 = Define(
        "XML1102",
        1102,
        "Limit: MaxElementDepth",
        "The element nesting exceeds the maximum depth.",
        2
    );

    /// <summary><c>MaxAttributesPerElement</c> exceeded.</summary>
    public static readonly XmlErrorCode XML1103 = Define(
        "XML1103",
        1103,
        "Limit: MaxAttributesPerElement",
        "An element exceeds the maximum number of attributes.",
        3
    );

    /// <summary><c>MaxNamespaceDeclarationsPerElement</c> exceeded.</summary>
    public static readonly XmlErrorCode XML1104 = Define(
        "XML1104",
        1104,
        "Limit: MaxNamespaceDeclarationsPerElement",
        "An element exceeds the maximum number of namespace declarations."
    );

    /// <summary><c>MaxNameLength</c> exceeded.</summary>
    public static readonly XmlErrorCode XML1105 = Define(
        "XML1105",
        1105,
        "Limit: MaxNameLength",
        "A name exceeds the maximum length."
    );

    /// <summary><c>MaxTokenLength</c> exceeded.</summary>
    public static readonly XmlErrorCode XML1106 = Define(
        "XML1106",
        1106,
        "Limit: MaxTokenLength",
        "A comment or processing instruction exceeds the maximum length."
    );

    /// <summary><c>MaxTextNodeLength</c> exceeded.</summary>
    public static readonly XmlErrorCode XML1107 = Define(
        "XML1107",
        1107,
        "Limit: MaxTextNodeLength",
        "A text node exceeds the maximum length."
    );

    /// <summary><c>MaxNodeCount</c> exceeded.</summary>
    public static readonly XmlErrorCode XML1108 = Define(
        "XML1108",
        1108,
        "Limit: MaxNodeCount",
        "The document exceeds the maximum number of nodes."
    );

    /// <summary><c>MaxEntityExpansionCharacters</c> exceeded.</summary>
    public static readonly XmlErrorCode XML1109 = Define(
        "XML1109",
        1109,
        "Limit: MaxEntityExpansionCharacters",
        "Entity expansion exceeds the maximum number of characters.",
        4
    );

    /// <summary><c>MaxEntityDepth</c> exceeded.</summary>
    public static readonly XmlErrorCode XML1110 = Define(
        "XML1110",
        1110,
        "Limit: MaxEntityDepth",
        "Entity references are nested deeper than the maximum depth.",
        5
    );

    /// <summary><c>MaxDistinctNames</c> exceeded.</summary>
    public static readonly XmlErrorCode XML1111 = Define(
        "XML1111",
        1111,
        "Limit: MaxDistinctNames",
        "The name table exceeds the maximum number of distinct names."
    );

    /// <summary><c>MaxAttributeValueLength</c> exceeded.</summary>
    public static readonly XmlErrorCode XML1112 = Define(
        "XML1112",
        1112,
        "Limit: MaxAttributeValueLength",
        "An attribute value exceeds the maximum length."
    );

    /// <summary><c>MaxStartTagCharacters</c> exceeded.</summary>
    public static readonly XmlErrorCode XML1113 = Define(
        "XML1113",
        1113,
        "Limit: MaxStartTagCharacters",
        "A start tag exceeds the maximum number of characters."
    );

    /// <summary><c>MaxXmlDeclarationLength</c> exceeded.</summary>
    public static readonly XmlErrorCode XML1114 = Define(
        "XML1114",
        1114,
        "Limit: MaxXmlDeclarationLength",
        "The XML declaration exceeds the maximum length."
    );

    /// <summary><c>MaxDoctypeLength</c> exceeded.</summary>
    public static readonly XmlErrorCode XML1115 = Define(
        "XML1115",
        1115,
        "Limit: MaxDoctypeLength",
        "The document type declaration exceeds the maximum length."
    );

    /// <summary>The name table is frozen and does not contain the name.</summary>
    public static readonly XmlErrorCode XML1116 = Define(
        "XML1116",
        1116,
        "Limit: NameTableFrozen",
        "The name table is frozen and does not contain the name."
    );

    /// <summary>Document type declaration not allowed.</summary>
    public static readonly XmlErrorCode XML2001 = Define(
        "XML2001",
        1201,
        "Production: doctypedecl",
        "A document type declaration is not allowed."
    );

    /// <summary>External access not allowed.</summary>
    public static readonly XmlErrorCode XML2002 = Define(
        "XML2002",
        1202,
        "Production: ExternalID",
        "Access to an external resource is not allowed."
    );

    /// <summary>Unbound namespace prefix.</summary>
    public static readonly XmlErrorCode XML3001 = Define(
        "XML3001",
        1301,
        "NSC: Prefix Declared",
        "The namespace prefix is not declared."
    );

    /// <summary>Reserved prefix or namespace name misused.</summary>
    public static readonly XmlErrorCode XML3002 = Define(
        "XML3002",
        1302,
        "NSC: Reserved Prefixes and Namespace Names",
        "A reserved namespace prefix or name is misused."
    );

    /// <summary>Duplicate attribute by expanded name.</summary>
    public static readonly XmlErrorCode XML3003 = Define(
        "XML3003",
        1303,
        "NSC: Attributes Unique",
        "Two attributes of an element have the same expanded name."
    );

    /// <summary>Invalid qualified name.</summary>
    public static readonly XmlErrorCode XML3004 = Define(
        "XML3004",
        1304,
        "Production: QName",
        "The name is not a valid qualified name."
    );

    /// <summary>Empty prefixed namespace declaration.</summary>
    public static readonly XmlErrorCode XML3005 = Define(
        "XML3005",
        1305,
        "Production: NSAttName",
        "A prefixed namespace declaration must not be empty."
    );

    /// <summary>Unsupported encoding.</summary>
    public static readonly XmlErrorCode XML4001 = Define(
        "XML4001",
        1401,
        "Production: EncodingDecl",
        "The character encoding is not supported."
    );

    /// <summary>Byte order mark and declared encoding conflict.</summary>
    public static readonly XmlErrorCode XML4002 = Define(
        "XML4002",
        1402,
        "Appendix F: Autodetection of Character Encodings",
        "The byte order mark and the declared encoding conflict."
    );

    /// <summary>Invalid byte sequence for the encoding.</summary>
    public static readonly XmlErrorCode XML4003 = Define(
        "XML4003",
        1403,
        "Section 4.3.3: Character Encoding in Entities",
        "The input contains a byte sequence that is invalid for its encoding."
    );

    /// <summary>Gets every code of the table, in declaration order.</summary>
    public static IReadOnlyList<XmlErrorCode> All => Table;

    private static XmlErrorCode Define(string alias, int number, string constraint, string message, int coreNumber = 0)
    {
        var code = new XmlErrorCode(
            alias,
            new DiagnosticCode(number),
            constraint,
            message,
            coreNumber == 0 ? default : new DiagnosticCode(coreNumber)
        );
        Table.Add(code);

        return code;
    }
}
