namespace NetEvolve.Schemas.XPath.Casting.Temporal;

using System.Runtime.CompilerServices;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The registration file of this folder: the rows of the date, time, Gregorian, and duration types as data. Its module initializer stages
/// <see cref="RegisterRows"/> into the default casting catalog, which the constructor functions of these types follow
/// (<see cref="CastingRegistration"/>).
/// </summary>
/// <remarks>
/// The initializer only stages; the table is built, and the type handles are read, when the catalogs freeze on first use.
/// </remarks>
internal static class TemporalRegistration
{
    // The types of the rows, in the order of the matrix and of the conversions: the two string types come first, because they are the
    // source of every cast from a lexical form and the target of every cast to a string.
    private static readonly string[] Names =
    [
        "string",
        "untypedAtomic",
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
    ];

    // The conversion into each type; the string types are the targets of every temporal source.
    private static readonly CastConversion[] Conversions =
    [
        TemporalConversions.ToStringType,
        TemporalConversions.ToUntypedAtomic,
        TemporalConversions.ToDuration,
        TemporalConversions.ToYearMonthDuration,
        TemporalConversions.ToDayTimeDuration,
        TemporalConversions.ToDateTime,
        TemporalConversions.ToDate,
        TemporalConversions.ToTime,
        TemporalConversions.ToGYearMonth,
        TemporalConversions.ToGYear,
        TemporalConversions.ToGMonthDay,
        TemporalConversions.ToGDay,
        TemporalConversions.ToGMonth,
    ];

    // XQuery 1.0 and XPath 2.0 Functions and Operators, 17.1: one row per source type, one column per target type; Y always succeeds, M depends
    // on the value, N is not allowed (XPTY0004, no row), and - is a cell of the seven types of the casting core.
    private static readonly string[] Matrix =
    [
        "--MMMMMMMMMMM", // xs:string
        "--MMMMMMMMMMM", // xs:untypedAtomic
        "YYYYYNNNNNNNN", // xs:duration
        "YYYYYNNNNNNNN", // xs:yearMonthDuration
        "YYYYYNNNNNNNN", // xs:dayTimeDuration
        "YYNNNYYYYYYYY", // xs:dateTime
        "YYNNNYYNYYYYY", // xs:date
        "YYNNNNNYNNNNN", // xs:time
        "YYNNNNNNYNNNN", // xs:gYearMonth
        "YYNNNNNNNYNNN", // xs:gYear
        "YYNNNNNNNNYNN", // xs:gMonthDay
        "YYNNNNNNNNNYN", // xs:gDay
        "YYNNNNNNNNNNY", // xs:gMonth
    ];

    /// <summary>Adds the rows of the date, time, Gregorian, and duration types.</summary>
    /// <param name="builder">The builder.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static void RegisterRows(CastingTableBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var types = new XdmTypeId[Names.Length];
        for (var i = 0; i < types.Length; i++)
        {
            types[i] = XdmTypes.Get(Names[i]);
        }

        for (var source = 0; source < types.Length; source++)
        {
            for (var target = 0; target < types.Length; target++)
            {
                var cell = Matrix[source][target];
                if (cell is 'Y' or 'M')
                {
                    builder.Add(
                        new CastRow(
                            types[source],
                            types[target],
                            cell == 'Y' ? CastCell.Always : CastCell.ValueDependent,
                            Conversions[target]
                        )
                    );
                }
            }
        }
    }

    // CA2255 is suppressed on this one method: the design approved in #123 (AP8, option B) makes the module initializer the discovery
    // mechanism of the registration files, so that no shared list of rows exists. It only stages; nothing runs until the catalogs freeze.
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Initialize() => CastingCatalog.Default.Stage(RegisterRows);
}
