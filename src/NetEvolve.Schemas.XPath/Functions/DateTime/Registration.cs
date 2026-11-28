namespace NetEvolve.Schemas.XPath.Functions.DateTime;

using System.Runtime.CompilerServices;
using NetEvolve.Schemas.XPath.Functions.Binding;

/// <summary>
/// The registration file of the date and time group: its module initializer stages <see cref="Register"/> into the default catalog
/// (#123, AP8, option B), so no shared list names the group. The group holds the component extraction functions, the timezone
/// adjustment functions, the context functions that read the current date and time, and <c>parse-ietf-date</c>.
/// </summary>
/// <remarks>
/// Limitation: the context functions live in this folder because the work package owns it; the design names a <c>Functions/Context</c> group for them.
/// </remarks>
internal static class Registration
{
    // Decision (#123, AP8, option B): a library registers its function groups through a module initializer, so CA2255 is suppressed in this file, once, and not in any other.
#pragma warning disable CA2255
    [ModuleInitializer]
    internal static void Stage() => FunctionCatalog.Default.Stage(Register);
#pragma warning restore CA2255

    /// <summary>Adds the functions of the group.</summary>
    /// <param name="builder">The catalog builder.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    internal static void Register(FunctionCatalogBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Add(new YearFromDateTimeFunction());
        builder.Add(new MonthFromDateTimeFunction());
        builder.Add(new DayFromDateTimeFunction());
        builder.Add(new HoursFromDateTimeFunction());
        builder.Add(new MinutesFromDateTimeFunction());
        builder.Add(new SecondsFromDateTimeFunction());
        builder.Add(new TimezoneFromDateTimeFunction());
        builder.Add(new YearFromDateFunction());
        builder.Add(new MonthFromDateFunction());
        builder.Add(new DayFromDateFunction());
        builder.Add(new TimezoneFromDateFunction());
        builder.Add(new HoursFromTimeFunction());
        builder.Add(new MinutesFromTimeFunction());
        builder.Add(new SecondsFromTimeFunction());
        builder.Add(new TimezoneFromTimeFunction());
        builder.Add(new YearsFromDurationFunction());
        builder.Add(new MonthsFromDurationFunction());
        builder.Add(new DaysFromDurationFunction());
        builder.Add(new HoursFromDurationFunction());
        builder.Add(new MinutesFromDurationFunction());
        builder.Add(new SecondsFromDurationFunction());
        builder.Add(new AdjustDateTimeToTimezoneFunction());
        builder.Add(new AdjustDateToTimezoneFunction());
        builder.Add(new AdjustTimeToTimezoneFunction());
        builder.Add(new DateTimeConstructorFunction());
        builder.Add(new CurrentDateTimeFunction());
        builder.Add(new CurrentDateFunction());
        builder.Add(new CurrentTimeFunction());
        builder.Add(new ImplicitTimezoneFunction());
        builder.Add(new ParseIetfDateFunction());
    }
}
