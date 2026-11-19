namespace NetEvolve.Schemas.XPath.Evaluation.Temporal;

using System.Numerics;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.XPath.Errors;

/// <summary>
/// The range of the date, time, and duration values that the operators produce. Functions and Operators 1.0 leaves the range to the
/// implementation and names FODT0001 for a date or time result outside it and FODT0002 for a duration result outside it. A result is
/// in range when its lexical form is in the range of the datatype layer: a year of at most 17 digits (#100 AP-9), and a duration whose
/// months and seconds a lexical form of 17-digit components can write, so that every result can be cast back to its lexical form (#301 D4).
/// </summary>
/// <remarks>Limitation: the range is the implementation-defined range that the specification allows, not the unbounded value space of XML Schema.</remarks>
internal static class TemporalRange
{
    /// <summary>The largest year of either sign: 17 digits.</summary>
    public const long MaxYear = 99_999_999_999_999_999;

    /// <summary>The largest number of months of either sign: 17-digit years and a 17-digit month component, 13 months for each digit position of the years.</summary>
    public const long MaxMonths = 13L * MaxYear;

    // 17-digit days, hours, minutes, and seconds in one duration: (10^17 - 1) x (86400 + 3600 + 60 + 1).
    private static readonly XsdDecimal MaxSeconds = new(new BigInteger(MaxYear) * 90061, 0);

    private static readonly XsdDecimal MinSeconds = MaxSeconds.Negate();

    /// <summary>Checks the year of a date or time result.</summary>
    /// <param name="year">The year.</param>
    /// <exception cref="XPathException">The year has more than 17 digits (FODT0001).</exception>
    public static void CheckYear(long year)
    {
        if (year is > MaxYear or < -MaxYear)
        {
            throw XPathErrors.Exception(XPathErrorCodes.FODT0001);
        }
    }

    /// <summary>Checks the months of a duration result.</summary>
    /// <param name="months">The months.</param>
    /// <returns>The months as a 64-bit integer.</returns>
    /// <exception cref="XPathException">The months are beyond the range (FODT0002).</exception>
    public static long CheckMonths(BigInteger months) =>
        months > MaxMonths || months < -MaxMonths
            ? throw XPathErrors.Exception(XPathErrorCodes.FODT0002)
            : (long)months;

    /// <summary>Checks the seconds of a duration result.</summary>
    /// <param name="seconds">The seconds.</param>
    /// <param name="overflow">The error for a result beyond the range: FODT0002 for a duration operation, FODT0001 for the difference of two moments, which is an operation on dates and times.</param>
    /// <returns>The seconds.</returns>
    /// <exception cref="XPathException">The seconds are beyond the range.</exception>
    public static XsdDecimal CheckSeconds(XsdDecimal seconds, XPathErrorCode overflow) =>
        seconds.CompareTo(MaxSeconds) > 0 || seconds.CompareTo(MinSeconds) < 0
            ? throw XPathErrors.Exception(overflow)
            : seconds;

    /// <summary>Gets the year as the numbering of XML Schema 1.1 that the timeline of the datatype layer uses: the year before 1 is 0.</summary>
    /// <param name="year">The year in the numbering of XML Schema 1.0, which XPath 2.0 refers to: there is no year 0, and -1 precedes 1.</param>
    /// <returns>The year with a year 0.</returns>
    public static long ToTimelineYear(long year) => year < 0 ? year + 1 : year;

    /// <summary>Gets the year in the numbering of XML Schema 1.0 from the one of the timeline.</summary>
    /// <param name="year">The year with a year 0.</param>
    /// <returns>The year without a year 0.</returns>
    public static long FromTimelineYear(long year) => year <= 0 ? year - 1 : year;
}
