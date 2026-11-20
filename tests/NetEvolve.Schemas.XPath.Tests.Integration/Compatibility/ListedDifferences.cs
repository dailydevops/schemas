namespace NetEvolve.Schemas.XPath.Tests.Integration.Compatibility;

using System.Collections.Generic;

/// <summary>
/// The cases in which the library in compatibility mode gives another result than System.Xml.XPath, each with the result of the
/// library (<c>Ours</c>) and the reason. A reason is a rule of XPath 2.0 (Appendix I, or the sections it names) that makes the difference follow
/// from the specification; a difference without a rule is a defect and has its own issue instead. A difference that is not listed
/// fails the tests, and so does a listed difference that has gone.
/// </summary>
internal static class ListedDifferences
{
    private const string AppI1_bool =
        "XPath 2.0, Appendix I.1: a boolean compared with a number by < or > is not compared as two numbers; in 3.5.2 the number first becomes its effective boolean value, so true() > number('0.5') compares true with true, which is false even in compatibility mode.";

    private const string AppI1_exp =
        "XPath 2.0, Appendix I.1: a number in exponential notation, such as 1e2, converts to a number under XPath 2.0, where XPath 1.0 gave NaN.";

    private const string AppI1_inf =
        "XPath 2.0, Appendix I.1: the strings INF and -INF convert to infinity under XPath 2.0, where XPath 1.0 gave NaN.";

    private const string AppI1_infinity =
        "XPath 2.0, Appendix I.1: the strings Infinity and -Infinity are not recognized under XPath 2.0 and convert to NaN in compatibility mode, where XPath 1.0 gave infinity.";

    private const string AppI1_plus =
        "XPath 2.0, Appendix I.1: a number written with a leading + sign converts to a number under XPath 2.0 (fn:number gives 4), where XPath 1.0 gave NaN.";

    private const string AppI1_string_inf =
        "XPath 2.0, Appendix I.1: the rules for converting numbers to strings have changed; an xs:double infinity is written INF or -INF (Functions and Operators 17.1.2), where XPath 1.0 wrote Infinity.";

    private const string AppI1_string_sci =
        "XPath 2.0, Appendix I.1: outside the range 1E-6 to 1E+6 a non-integral xs:float or xs:double is written in scientific notation (Functions and Operators 17.1.2), where XPath 1.0 never used it.";

    private const string AppI2_empty =
        "XPath 2.0, Appendix I.2: an empty node-set passed to a function that expects a number is no longer converted to NaN; fn:floor of the empty sequence is the empty sequence, and 3.1.5 in compatibility mode converts for xs:double parameters only.";

    private const string AppI2_string =
        "XPath 2.0, Appendix I.2: the arguments of a function are no longer converted implicitly to the required type; 3.1.5 in compatibility mode converts for xs:string and xs:double parameters only, so a string for the numeric parameter stays XPTY0004.";

    private const string AppI2_untyped =
        "XPath 2.0, Appendix I.2: an untyped value passed to a function that expects a number no longer converts an unrecognized string to NaN but raises a dynamic error (FORG0001); compatibility mode does not change it, because 3.1.5 names xs:string and xs:double parameters only.";

    /// <summary>Gets the listed differences by <see cref="CorpusCase.Key"/>.</summary>
    public static IReadOnlyDictionary<string, (string Ours, string Reason)> All { get; } =
        new Dictionary<string, (string Ours, string Reason)>(StringComparer.Ordinal)
        {
            ["clean|true() > number('0.5')"] = ("boolean:false", AppI1_bool),
            ["clean|4 = '+4'"] = ("boolean:true", AppI1_plus),
            ["numbers|number('+4')"] = ("number:4", AppI1_plus),
            ["numbers|number(/n/v[3])"] = ("number:4", AppI1_plus),
            ["numbers|/n/v[3] = 4"] = ("boolean:true", AppI1_plus),
            ["numbers|number('1e2')"] = ("number:100", AppI1_exp),
            ["numbers|number(/n/v[4])"] = ("number:100", AppI1_exp),
            ["numbers|/n/v[4] = 100"] = ("boolean:true", AppI1_exp),
            ["numbers|number('INF')"] = ("number:INF", AppI1_inf),
            ["numbers|number('Infinity')"] = ("number:NaN", AppI1_infinity),
            ["shop|string(1 div 0)"] = ("string:INF", AppI1_string_inf),
            ["shop|string(-1 div 0)"] = ("string:-INF", AppI1_string_inf),
            ["shop|string(100000000 * 100000000 * 100000000)"] = ("string:1.0E24", AppI1_string_sci),
            ["shop|string(1 div 3 div 1000000)"] = ("string:3.333333333333333E-7", AppI1_string_sci),
            ["numbers|sum(/n/v)"] = ("error:FORG0001", AppI2_untyped),
            ["numbers|floor(/n/v[2])"] = ("error:FORG0001", AppI2_untyped),
            ["shop|floor('3.5')"] = ("error:XPTY0004", AppI2_string),
            ["shop|round('abc')"] = ("error:XPTY0004", AppI2_string),
            ["shop|floor(//nothing)"] = ("nodes:", AppI2_empty),
        };
}
