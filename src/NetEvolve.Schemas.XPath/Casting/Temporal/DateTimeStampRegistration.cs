namespace NetEvolve.Schemas.XPath.Casting.Temporal;

using System.Runtime.CompilerServices;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The registration file of <c>xs:dateTimeStamp</c>, a type of XPath 3.1 (Functions and Operators 3.1, 19.2): a cast to it is a cast to
/// <c>xs:dateTime</c> whose result must have an explicit timezone, so every cell is value dependent. The types that cast to <c>xs:dateTime</c> are its
/// sources; the type as a source needs no rows of its own, because the casting table casts it as <c>xs:dateTime</c> (<see cref="CastingTable"/>).
/// </summary>
/// <remarks>
/// The type is visible in XPath 3.1 only: the binder of the item types rejects it before, and its constructor function is a function of XPath 3.1
/// (<see cref="ConstructorFunction"/>). The initializer only stages; the table is built when the catalogs freeze on first use.
/// </remarks>
internal static class DateTimeStampRegistration
{
    // The types that cast to xs:dateTime (the column of the matrix of the temporal rows).
    private static readonly string[] Sources = ["string", "untypedAtomic", "dateTime", "date"];

    /// <summary>Adds the rows of <c>xs:dateTimeStamp</c>.</summary>
    /// <param name="builder">The builder.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static void RegisterRows(CastingTableBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var target = XdmTypes.Get("dateTimeStamp");
        foreach (var name in Sources)
        {
            builder.Add(
                new CastRow(XdmTypes.Get(name), target, CastCell.ValueDependent, TemporalConversions.ToDateTimeStamp)
            );
        }
    }

    // CA2255 is suppressed on this one method: the design approved in #123 (AP8, option B) makes the module initializer the discovery
    // mechanism of the registration files, so that no shared list of rows exists. It only stages; nothing runs until the catalogs freeze.
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Initialize() => CastingCatalog.Default.Stage(RegisterRows);
}
