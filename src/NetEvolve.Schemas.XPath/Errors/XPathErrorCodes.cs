namespace NetEvolve.Schemas.XPath.Errors;

using System;
using System.Collections.Generic;

/// <summary>
/// The code table: every error condition of XPath 2.0 (appendix G, <c>XPST</c>, <c>XPTY</c>, <c>XPDY</c>) and every
/// code of the Functions and Operators 1.0 error summary (<c>FO</c>), each with its default message.
/// </summary>
/// <remarks>
/// Limitation: codes added by XPath 3.0 and 3.1 and the XSLT codes of the Schematron host are not part of the table.
/// Codes the specification marks "not currently used" are listed so that a transcription of the appendix matches.
/// </remarks>
internal static class XPathErrorCodes
{
    // Declared before the code fields: their initializers register into it.
    private static readonly Dictionary<XPathErrorCode, string> Messages = [];

    internal static readonly XPathErrorCode XPST0001 = Define(
        "XPST0001",
        "A component of the static context has no value."
    );
    internal static readonly XPathErrorCode XPDY0002 = Define(
        "XPDY0002",
        "A component of the dynamic context has no value."
    );
    internal static readonly XPathErrorCode XPST0003 = Define(
        "XPST0003",
        "The expression is not valid according to the XPath grammar."
    );
    internal static readonly XPathErrorCode XPTY0004 = Define(
        "XPTY0004",
        "The type of a value does not match the required type."
    );
    internal static readonly XPathErrorCode XPST0005 = Define(
        "XPST0005",
        "The static type of the expression is the empty sequence."
    );
    internal static readonly XPathErrorCode XPTY0006 = Define("XPTY0006", "Not currently used by XPath 2.0.");
    internal static readonly XPathErrorCode XPTY0007 = Define("XPTY0007", "Not currently used by XPath 2.0.");
    internal static readonly XPathErrorCode XPST0008 = Define(
        "XPST0008",
        "A name is not defined in the static context."
    );
    internal static readonly XPathErrorCode XPST0010 = Define("XPST0010", "The axis is not supported.");
    internal static readonly XPathErrorCode XPST0017 = Define(
        "XPST0017",
        "No function matches the name and number of arguments."
    );
    internal static readonly XPathErrorCode XPTY0018 = Define(
        "XPTY0018",
        "The last step of a path returns both nodes and atomic values."
    );
    internal static readonly XPathErrorCode XPTY0019 = Define(
        "XPTY0019",
        "A step of a path other than the last returns an atomic value."
    );
    internal static readonly XPathErrorCode XPTY0020 = Define(
        "XPTY0020",
        "The context item of an axis step is not a node."
    );
    internal static readonly XPathErrorCode XPDY0021 = Define("XPDY0021", "Not currently used by XPath 2.0.");
    internal static readonly XPathErrorCode XPDY0050 = Define(
        "XPDY0050",
        "The operand of treat does not match the required sequence type."
    );
    internal static readonly XPathErrorCode XPST0051 = Define(
        "XPST0051",
        "The name is not an atomic type in the in-scope schema types."
    );
    internal static readonly XPathErrorCode XPST0080 = Define(
        "XPST0080",
        "The target type of cast or castable is not allowed."
    );
    internal static readonly XPathErrorCode XPST0081 = Define(
        "XPST0081",
        "A namespace prefix cannot be expanded to a namespace URI."
    );
    internal static readonly XPathErrorCode XPST0083 = Define("XPST0083", "Not currently used by XPath 2.0.");

    internal static readonly XPathErrorCode FOER0000 = Define("FOER0000", "Unidentified error.");
    internal static readonly XPathErrorCode FOAR0001 = Define("FOAR0001", "Division by zero.");
    internal static readonly XPathErrorCode FOAR0002 = Define("FOAR0002", "Numeric operation overflow or underflow.");
    internal static readonly XPathErrorCode FOCA0001 = Define("FOCA0001", "Input value too large for decimal.");
    internal static readonly XPathErrorCode FOCA0002 = Define("FOCA0002", "Invalid lexical value.");
    internal static readonly XPathErrorCode FOCA0003 = Define("FOCA0003", "Input value too large for integer.");
    internal static readonly XPathErrorCode FOCA0005 = Define("FOCA0005", "NaN supplied as float or double value.");
    internal static readonly XPathErrorCode FOCA0006 = Define(
        "FOCA0006",
        "String to be cast to decimal has too many digits of precision."
    );
    internal static readonly XPathErrorCode FOCH0001 = Define("FOCH0001", "Code point not valid.");
    internal static readonly XPathErrorCode FOCH0002 = Define("FOCH0002", "Unsupported collation.");
    internal static readonly XPathErrorCode FOCH0003 = Define("FOCH0003", "Unsupported normalization form.");
    internal static readonly XPathErrorCode FOCH0004 = Define(
        "FOCH0004",
        "Collation does not support collation units."
    );
    internal static readonly XPathErrorCode FODC0001 = Define("FODC0001", "No context document.");
    internal static readonly XPathErrorCode FODC0002 = Define("FODC0002", "Error retrieving resource.");
    internal static readonly XPathErrorCode FODC0003 = Define("FODC0003", "Function stability not defined.");
    internal static readonly XPathErrorCode FODC0004 = Define("FODC0004", "Invalid argument to fn:collection.");
    internal static readonly XPathErrorCode FODC0005 = Define(
        "FODC0005",
        "Invalid argument to fn:doc or fn:doc-available."
    );
    internal static readonly XPathErrorCode FODT0001 = Define(
        "FODT0001",
        "Overflow or underflow in date or time operation."
    );
    internal static readonly XPathErrorCode FODT0002 = Define(
        "FODT0002",
        "Overflow or underflow in duration operation."
    );
    internal static readonly XPathErrorCode FODT0003 = Define("FODT0003", "Invalid timezone value.");
    internal static readonly XPathErrorCode FONS0004 = Define("FONS0004", "No namespace found for prefix.");
    internal static readonly XPathErrorCode FONS0005 = Define(
        "FONS0005",
        "Base URI not defined in the static context."
    );
    internal static readonly XPathErrorCode FORG0001 = Define("FORG0001", "Invalid value for cast or constructor.");
    internal static readonly XPathErrorCode FORG0002 = Define("FORG0002", "Invalid argument to fn:resolve-uri.");
    internal static readonly XPathErrorCode FORG0003 = Define(
        "FORG0003",
        "fn:zero-or-one called with a sequence containing more than one item."
    );
    internal static readonly XPathErrorCode FORG0004 = Define(
        "FORG0004",
        "fn:one-or-more called with a sequence containing no items."
    );
    internal static readonly XPathErrorCode FORG0005 = Define(
        "FORG0005",
        "fn:exactly-one called with a sequence containing zero or more than one item."
    );
    internal static readonly XPathErrorCode FORG0006 = Define("FORG0006", "Invalid argument type.");
    internal static readonly XPathErrorCode FORG0008 = Define(
        "FORG0008",
        "The two arguments to fn:dateTime have inconsistent timezones."
    );
    internal static readonly XPathErrorCode FORG0009 = Define(
        "FORG0009",
        "Error resolving a relative URI against a base URI in fn:resolve-uri."
    );
    internal static readonly XPathErrorCode FORX0001 = Define("FORX0001", "Invalid regular expression flags.");
    internal static readonly XPathErrorCode FORX0002 = Define("FORX0002", "Invalid regular expression.");
    internal static readonly XPathErrorCode FORX0003 = Define(
        "FORX0003",
        "Regular expression matches zero-length string."
    );
    internal static readonly XPathErrorCode FORX0004 = Define("FORX0004", "Invalid replacement string.");
    internal static readonly XPathErrorCode FOTY0012 = Define("FOTY0012", "Argument node does not have a typed value.");
    internal static readonly XPathErrorCode FOTY0013 = Define(
        "FOTY0013",
        "Atomization is not defined for a function item."
    );
    internal static readonly XPathErrorCode XQST0039 = Define(
        "XQST0039",
        "Two parameters of an inline function expression have the same name."
    );
    internal static readonly XPathErrorCode XQDY0137 = Define(
        "XQDY0137",
        "A map constructor has two entries with the same key."
    );
    internal static readonly XPathErrorCode FOJS0003 = Define(
        "FOJS0003",
        "The input of map:merge has duplicate keys and the duplicates option is reject."
    );
    internal static readonly XPathErrorCode FOJS0005 = Define("FOJS0005", "An option has an invalid value.");

    internal static readonly XPathErrorCode FOAY0001 = Define("FOAY0001", "Array index out of bounds.");
    internal static readonly XPathErrorCode FOAY0002 = Define("FOAY0002", "Negative array length.");

    /// <summary>Gets every code of the table.</summary>
    internal static IReadOnlyCollection<XPathErrorCode> All => Messages.Keys;

    /// <summary>Gets the default message of a code.</summary>
    /// <param name="code">The code.</param>
    /// <param name="message">The message, when the code is in the table.</param>
    /// <returns><see langword="true"/> when the code is in the table.</returns>
    internal static bool TryGetMessage(XPathErrorCode code, out string message) =>
        Messages.TryGetValue(code, out message!);

    /// <summary>Gets the kind of a code from its prefix; anything outside the table's scheme is an engine error.</summary>
    /// <param name="code">The code.</param>
    /// <returns>The kind.</returns>
    internal static XPathErrorKind KindOf(XPathErrorCode code)
    {
        if (!string.Equals(code.Namespace, XPathErrorCode.StandardNamespace, StringComparison.Ordinal))
        {
            return XPathErrorKind.Engine;
        }

        var name = code.LocalName;
        if (name.StartsWith("XPST", StringComparison.Ordinal) || name.StartsWith("XQST", StringComparison.Ordinal))
        {
            return XPathErrorKind.Static;
        }

        if (name.StartsWith("XPTY", StringComparison.Ordinal))
        {
            return XPathErrorKind.Type;
        }

        if (name.StartsWith("XPDY", StringComparison.Ordinal) || name.StartsWith("XQDY", StringComparison.Ordinal))
        {
            return XPathErrorKind.Dynamic;
        }

        return name.StartsWith("FO", StringComparison.Ordinal) ? XPathErrorKind.Function : XPathErrorKind.Engine;
    }

    private static XPathErrorCode Define(string localName, string message)
    {
        var code = new XPathErrorCode(localName);
        Messages.Add(code, message);
        return code;
    }
}
