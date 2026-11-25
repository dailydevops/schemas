namespace NetEvolve.Schemas.XPath.Formatting.Dates;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>format-date</c>, <c>format-dateTime</c>, and <c>format-time</c> with two and with five arguments (Functions and Operators 3.1,
/// 9.8): the value formatted with a picture string. The empty sequence gives the empty sequence. The picture is read by
/// <see cref="DatePicture"/>, the components are written by <see cref="DateFormatter"/>. The function exists for XPath 3.0 and 3.1, and
/// for XPath 2.0 under <see cref="XPathHostRequirements.Xslt20Host"/> with the error codes of XSLT 2.0.
/// </summary>
/// <remarks>
/// Limitations: every language is English and every calendar is Gregorian. When the requested language is not English, the result starts
/// with <c>[Language: en]</c>; when the requested calendar is not <c>AD</c>, <c>CE</c>, or <c>ISO</c>, it starts with
/// <c>[Calendar: AD]</c>, which the specification prescribes for a fallback. The place is not used, because no time zone data is used.
/// </remarks>
internal sealed class FormatDateTimeFunction : XPathFunction
{
    private static readonly HashSet<string> KnownCalendars = new(StringComparer.Ordinal)
    {
        "AD",
        "AH",
        "AME",
        "AM",
        "AP",
        "AS",
        "BE",
        "CB",
        "CE",
        "CL",
        "CS",
        "EE",
        "FE",
        "ISO",
        "JE",
        "KE",
        "KY",
        "ME",
        "MS",
        "NS",
        "OS",
        "RS",
        "SE",
        "SH",
        "SS",
        "TE",
        "VE",
        "ZR",
    };

    private readonly bool _xslt20;
    private readonly int _arity;

    /// <summary>Initializes a new instance of the <see cref="FormatDateTimeFunction"/> class.</summary>
    /// <param name="kind">The type of the value: <see cref="XsdDateTimeKind.DateTime"/>, <see cref="XsdDateTimeKind.Date"/>, or <see cref="XsdDateTimeKind.Time"/>.</param>
    /// <param name="arity">2 or 5.</param>
    /// <param name="xslt20">Whether the function is the one of the XSLT 2.0 host, with its error codes.</param>
    public FormatDateTimeFunction(XsdDateTimeKind kind, int arity, bool xslt20)
        : base(
            new XPathQName(XPathStaticContext.Default.DefaultFunctionNamespace, NameOf(kind)),
            arity,
            arity,
            Parameters(kind, arity)
        )
    {
        _xslt20 = xslt20;
        _arity = arity;
    }

    /// <summary>Gets the language versions in which the function is visible: XPath 2.0 for the XSLT 2.0 function, XPath 3.0 and 3.1 for the other.</summary>
    public override XPathVersionMask Versions =>
        _xslt20 ? XPathVersionMask.V20 : XPathVersionMask.V30 | XPathVersionMask.V31;

    /// <summary>Gets the host flag: the XSLT 2.0 function is visible only when the host sets <see cref="XPathHostRequirements.Xslt20Host"/>.</summary>
    public override XPathHostRequirements HostRequirements =>
        _xslt20 ? XPathHostRequirements.Xslt20Host : XPathHostRequirements.None;

    private static string NameOf(XsdDateTimeKind kind) =>
        kind switch
        {
            XsdDateTimeKind.Date => "format-date",
            XsdDateTimeKind.Time => "format-time",
            _ => "format-dateTime",
        };

    private static XPathParameterType[] Parameters(XsdDateTimeKind kind, int arity)
    {
        var type = kind switch
        {
            XsdDateTimeKind.Date => XPathItemType.Date,
            XsdDateTimeKind.Time => XPathItemType.Time,
            _ => XPathItemType.DateTime,
        };
        var parameters = new XPathParameterType[arity];
        parameters[0] = new XPathParameterType(type, Occurrence.ZeroOrOne);
        parameters[1] = new XPathParameterType(XPathItemType.String, Occurrence.ExactlyOne);
        for (var i = 2; i < arity; i++)
        {
            parameters[i] = new XPathParameterType(XPathItemType.String, Occurrence.ZeroOrOne);
        }

        return parameters;
    }

    /// <inheritdoc />
    /// <exception cref="XPathException">
    /// The picture string or the calendar is not valid (FOFD1340, XTDE1340), or a component is not available for the type (FOFD1350, XTDE1350).
    /// </exception>
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var arguments = invocation.Arguments;
        if (arguments[0].IsEmpty)
        {
            return XPathEvaluationStatus.Ok;
        }

        _ = arguments[1][0].TryGetString(out var text);
        if (!DatePicture.TryParse(text!, out var picture, out var problem))
        {
            throw DateFormatErrors.InvalidPicture(_xslt20, problem!);
        }

        var prefix = string.Empty;
        var calendar = "AD";
        if (_arity == 5)
        {
            if (!IsEnglish(Text(arguments[2])))
            {
                prefix = "[Language: en]";
            }

            var fallback = ResolveCalendar(Text(arguments[3]), out calendar);
            prefix += fallback ? "[Calendar: AD]" : string.Empty;
        }

        _ = arguments[0][0].TryGetDateTime(out var value);
        invocation.Result = XdmSequence.Create(
            XdmItem.FromString(prefix + DateFormatter.Format(in value, picture!, calendar, _xslt20))
        );
        return XPathEvaluationStatus.Ok;
    }

    private static string? Text(XdmSequence argument) =>
        !argument.IsEmpty && argument[0].TryGetString(out var text) ? text : null;

    // English is the language of the tags en and en-*; an absent or empty language is the default language.
    private static bool IsEnglish(string? language)
    {
        if (string.IsNullOrEmpty(language))
        {
            return true;
        }

        var parts = language!.Split('-');
        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length is 0 or > 8 || !IsLettersOrDigits(parts[i], i == 0))
            {
                return false;
            }
        }

        return string.Equals(parts[0], "en", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLettersOrDigits(string text, bool lettersOnly)
    {
        foreach (var c in text)
        {
            if (!(c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' || (!lettersOnly && c is >= '0' and <= '9')))
            {
                return false;
            }
        }

        return true;
    }

    // The calendar is an EQName. A name in no namespace must be one of the calendars of the specification; a name in a namespace is a
    // calendar this library does not know. Returns whether the fallback to the Gregorian calendar was taken.
    private bool ResolveCalendar(string? requested, out string calendar)
    {
        calendar = "AD";
        if (string.IsNullOrEmpty(requested))
        {
            return false;
        }

        var name = requested!;
        string local;
        bool namespaced;
        if (name.StartsWith("Q{", StringComparison.Ordinal))
        {
            var close = name.AsSpan().IndexOf('}');
            if (close < 0)
            {
                throw InvalidCalendar(name);
            }

            namespaced = close > 2;
            local = name.Substring(close + 1);
        }
        else
        {
            var colon = name.AsSpan().IndexOf(':');
            namespaced = colon >= 0;
            if (colon >= 0 && !IsNcName(name.Substring(0, colon)))
            {
                throw InvalidCalendar(name);
            }

            local = name.Substring(colon + 1);
        }

        if (!IsNcName(local))
        {
            throw InvalidCalendar(name);
        }

        if (namespaced)
        {
            return true;
        }

        if (!KnownCalendars.Contains(local))
        {
            throw InvalidCalendar(name);
        }

        calendar = local == "ISO" ? "ISO" : "AD";
        return local is not ("AD" or "CE" or "ISO");
    }

    private XPathException InvalidCalendar(string name) =>
        DateFormatErrors.InvalidPicture(
            _xslt20,
            "The calendar '" + name + "' is not a valid name or names no known calendar."
        );

    private static bool IsNcName(string text)
    {
        if (text.Length == 0 || !(char.IsLetter(text[0]) || text[0] == '_'))
        {
            return false;
        }

        foreach (var c in text)
        {
            if (!(char.IsLetterOrDigit(c) || c is '_' or '-' or '.'))
            {
                return false;
            }
        }

        return true;
    }
}
