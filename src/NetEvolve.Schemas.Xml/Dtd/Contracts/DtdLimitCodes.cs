namespace NetEvolve.Schemas.Xml.Dtd.Contracts;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>
/// The codes of the limits that DTD validation adds (design #122, section 9). They are rows of the table type of the
/// reader, <see cref="XmlErrorCode"/>, so the reader and the steps raise them as fatal statuses without a second definition.
/// </summary>
/// <remarks>
/// The numbers continue the limit codes <c>XML1101</c> to <c>XML1116</c> and keep the formula of <see cref="XmlErrorCodes"/>
/// (<c>NES1117</c> to <c>NES1119</c>). Exceeding a limit is never a validity issue.
/// </remarks>
public static class DtdLimitCodes
{
    // Declared before the code fields: their initializers register into it.
    private static readonly List<XmlErrorCode> Table = [];

    // Read-only wrapper over the list above: callers cannot cast it back to a mutable list.
    private static readonly ReadOnlyCollection<XmlErrorCode> TableView = new(Table);

    /// <summary>The number of resolver calls per document exceeds <see cref="DtdValidationLimits.MaxExternalEntityRequests"/>; raised by the entity loader (XML-61).</summary>
    public static readonly XmlErrorCode XML1117 = Define(
        "XML1117",
        1117,
        "Limit: MaxExternalEntityRequests",
        "The document needs more external resources than the maximum number of resolver requests."
    );

    /// <summary>A content model needs more automaton positions than <see cref="DtdValidationLimits.MaxContentModelStates"/>; raised by the content step (XML-63).</summary>
    public static readonly XmlErrorCode XML1118 = Define(
        "XML1118",
        1118,
        "Limit: MaxContentModelStates",
        "A content model exceeds the maximum number of compiled automaton positions."
    );

    /// <summary>The document declares more than <see cref="DtdValidationLimits.MaxDeclarations"/> declarations; raised by the declaration parser (XML-20).</summary>
    public static readonly XmlErrorCode XML1119 = Define(
        "XML1119",
        1119,
        "Limit: MaxDeclarations",
        "The document type exceeds the maximum number of declarations."
    );

    /// <summary>Gets every limit code, in code order.</summary>
    public static IReadOnlyList<XmlErrorCode> All => TableView;

    private static XmlErrorCode Define(string alias, int number, string constraint, string message)
    {
        var code = new XmlErrorCode(alias, new DiagnosticCode(number), constraint, message, default);
        Table.Add(code);

        return code;
    }
}
