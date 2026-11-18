namespace NetEvolve.Schemas.XPath.Tests.Unit.Casting.Temporal;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Tests.Unit.Casting.Core;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The casting table of XQuery 1.0 and XPath 2.0 Functions and Operators, 17.1, for the primitive types of XPath 2.0 plus the two derived
/// duration types, as data: one row per source type, one column per target type, Y (always succeeds), M (the value decides), N (never,
/// XPTY0004). The N cells match the QT3 grid of the K-SeqExprCast cases of the XPath 2.0 selection. Cells of the sources
/// <c>xs:string</c> and <c>xs:untypedAtomic</c> to <c>xs:QName</c> are M: the version of the language narrows them (XPath 2.0 casts
/// only a string literal).
/// </summary>
internal static class CastMatrix
{
    public static readonly string[] Names =
    [
        "string",
        "untypedAtomic",
        "float",
        "double",
        "decimal",
        "integer",
        "boolean",
        "duration",
        "yearMonthDuration",
        "dayTimeDuration",
        "dateTime",
        "date",
        "time",
        "gYearMonth",
        "gYear",
        "gMonthDay",
        "gDay",
        "gMonth",
        "hexBinary",
        "base64Binary",
        "anyURI",
        "QName",
    ];

    // Groups: seven types | durations | dateTime date time | five Gregorian | two binary | anyURI | QName.
    private static readonly string[] Rows =
    [
        "YYMMMMM MMM MMM MMMMM MM Y M", // string
        "YYMMMMM MMM MMM MMMMM MM Y M", // untypedAtomic
        "YYYYMMY NNN NNN NNNNN NN N N", // float
        "YYYYMMY NNN NNN NNNNN NN N N", // double
        "YYYYYYY NNN NNN NNNNN NN N N", // decimal
        "YYYYYYY NNN NNN NNNNN NN N N", // integer
        "YYYYYYY NNN NNN NNNNN NN N N", // boolean
        "YYNNNNN YYY NNN NNNNN NN N N", // duration
        "YYNNNNN YYY NNN NNNNN NN N N", // yearMonthDuration
        "YYNNNNN YYY NNN NNNNN NN N N", // dayTimeDuration
        "YYNNNNN NNN YYY YYYYY NN N N", // dateTime
        "YYNNNNN NNN YYN YYYYY NN N N", // date
        "YYNNNNN NNN NNY NNNNN NN N N", // time
        "YYNNNNN NNN NNN YNNNN NN N N", // gYearMonth
        "YYNNNNN NNN NNN NYNNN NN N N", // gYear
        "YYNNNNN NNN NNN NNYNN NN N N", // gMonthDay
        "YYNNNNN NNN NNN NNNYN NN N N", // gDay
        "YYNNNNN NNN NNN NNNNY NN N N", // gMonth
        "YYNNNNN NNN NNN NNNNN YY N N", // hexBinary
        "YYNNNNN NNN NNN NNNNN YY N N", // base64Binary
        "YYNNNNN NNN NNN NNNNN NN Y N", // anyURI
        "YYNNNNN NNN NNN NNNNN NN N Y", // QName
    ];

    // A value of each type, as an expression.
    public static readonly string[] Samples =
    [
        "'abc'",
        "xs:untypedAtomic('abc')",
        "xs:float('1.5')",
        "xs:double('1.5')",
        "xs:decimal('1.5')",
        "xs:integer('1')",
        "xs:boolean('true')",
        "xs:duration('P1Y2M3DT10H30M')",
        "xs:yearMonthDuration('P1Y2M')",
        "xs:dayTimeDuration('P3DT10H30M')",
        "xs:dateTime('2004-10-13T12:30:00Z')",
        "xs:date('2004-10-13Z')",
        "xs:time('12:30:00Z')",
        "xs:gYearMonth('2004-10')",
        "xs:gYear('2004')",
        "xs:gMonthDay('--10-13')",
        "xs:gDay('---13')",
        "xs:gMonth('--10')",
        "xs:hexBinary('0FB7')",
        "xs:base64Binary('Dzw=')",
        "xs:anyURI('http://example.com/a')",
        "xs:QName('local')",
    ];

    // A valid lexical form of each type, for the M cells of a string source.
    public static readonly string[] Lexicals =
    [
        "abc",
        "abc",
        "1.5",
        "1.5",
        "1.5",
        "1",
        "true",
        "P1Y2M3DT10H30M",
        "P1Y2M",
        "P3DT10H30M",
        "2004-10-13T12:30:00Z",
        "2004-10-13Z",
        "12:30:00Z",
        "2004-10",
        "2004",
        "--10-13",
        "---13",
        "--10",
        "0FB7",
        "Dzw=",
        "http://example.com/a",
        "local",
    ];

    public static readonly XdmItemKind[] Kinds =
    [
        XdmItemKind.XsString,
        XdmItemKind.UntypedAtomic,
        XdmItemKind.XsFloat,
        XdmItemKind.XsDouble,
        XdmItemKind.XsDecimal,
        XdmItemKind.XsInteger,
        XdmItemKind.Boolean,
        XdmItemKind.Duration,
        XdmItemKind.YearMonthDuration,
        XdmItemKind.DayTimeDuration,
        XdmItemKind.DateTime,
        XdmItemKind.Date,
        XdmItemKind.Time,
        XdmItemKind.GYearMonth,
        XdmItemKind.GYear,
        XdmItemKind.GMonthDay,
        XdmItemKind.GDay,
        XdmItemKind.GMonth,
        XdmItemKind.HexBinary,
        XdmItemKind.Base64Binary,
        XdmItemKind.AnyUri,
        XdmItemKind.QName,
    ];

    // The first index of each group of new types.
    public const int FirstTemporal = 7;
    public const int FirstOther = 18;

    public static bool IsTemporal(int index) => index >= FirstTemporal && index < FirstOther;

    public static bool IsOther(int index) => index >= FirstOther;

    /// <summary>The character of a cell: Y, M, or N.</summary>
    public static char Cell(int source, int target)
    {
        var row = Rows[source].Replace(" ", string.Empty, StringComparison.Ordinal);
        return row[target];
    }

    /// <summary>The cell of the table for the expected character.</summary>
    public static CastCell Expected(int source, int target) =>
        Cell(source, target) switch
        {
            'Y' => CastCell.Always,
            'M' => CastCell.ValueDependent,
            _ => CastCell.NotAllowed,
        };

    public static XdmTypeId Type(int index) => XdmTypes.Get(Names[index]);

    /// <summary>The pairs of the matrix that involve at least one type of the group and no type of a later group.</summary>
    public static IEnumerable<Func<(int Source, int Target)>> Pairs(Func<int, bool> inGroup, Func<int, bool> excluded)
    {
        for (var s = 0; s < Names.Length; s++)
        {
            for (var t = 0; t < Names.Length; t++)
            {
                if ((inGroup(s) || inGroup(t)) && !excluded(s) && !excluded(t))
                {
                    var pair = (s, t);
                    yield return () => pair;
                }
            }
        }
    }

    /// <summary>Gets the number of cells that are not N, that is the number of rows of the table.</summary>
    public static int RowCount()
    {
        var count = 0;
        for (var s = 0; s < Names.Length; s++)
        {
            for (var t = 0; t < Names.Length; t++)
            {
                count += Cell(s, t) == 'N' ? 0 : 1;
            }
        }

        return count;
    }
}
