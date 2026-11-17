namespace NetEvolve.Schemas.Xml.Datatypes.Duration;

/// <summary>The lexical space of a duration family type.</summary>
public enum DurationKind
{
    /// <summary><c>duration</c>: the full <c>PnYnMnDTnHnMnS</c> form (XSD 1.1 Part 2, section 3.3.6).</summary>
    Duration = 0,

    /// <summary><c>yearMonthDuration</c>: year and month components only (section 3.4.26).</summary>
    YearMonth,

    /// <summary><c>dayTimeDuration</c>: day and time components only (section 3.4.27).</summary>
    DayTime,
}
