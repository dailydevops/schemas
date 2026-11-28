namespace NetEvolve.Schemas.XPath.Casting.Other;

using System.Runtime.CompilerServices;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The registration file of this folder: the rows of <c>xs:hexBinary</c>, <c>xs:base64Binary</c>, <c>xs:anyURI</c>, and <c>xs:QName</c> as data. Its module
/// initializer stages <see cref="RegisterRows"/> into the default casting catalog, which the constructor functions of these types follow
/// (<see cref="CastingRegistration"/>).
/// </summary>
/// <remarks>
/// The initializer only stages; the table is built, and the type handles are read, when the catalogs freeze on first use.
/// </remarks>
internal static class OtherRegistration
{
    // The types of the rows, in the order of the matrix and of the conversions: the two string types come first, because they are the
    // source of every cast from a lexical form and the target of every cast to a string.
    private static readonly string[] Names =
    [
        "string",
        "untypedAtomic",
        "hexBinary",
        "base64Binary",
        "anyURI",
        "QName",
    ];

    // The column of xs:QName: its conversion takes the namespaces of the cast and is not part of Conversions.
    private const int QNameColumn = 5;

    // The conversion into each type; the string types are the targets of every source of this folder.
    private static readonly CastConversion[] Conversions =
    [
        OtherConversions.ToStringType,
        OtherConversions.ToUntypedAtomic,
        OtherConversions.ToHexBinary,
        OtherConversions.ToBase64Binary,
        OtherConversions.ToAnyUri,
    ];

    // XQuery 1.0 and XPath 2.0 Functions and Operators, 17.1: one row per source type, one column per target type; Y always succeeds, M depends
    // on the value, N is not allowed (XPTY0004, no row), and - is a cell of the seven types of the casting core. A string reaches
    // xs:anyURI for the value to decide, as the lexical space of xs:anyURI is checked (laxly, #411 and #412). A string or an untyped value reaches
    // xs:QName for the value to decide as well: XPath 2.0 narrows the cell to a string literal while lowering (CastingLowerings), so the table keeps
    // the cell of the later versions.
    private static readonly string[] Matrix =
    [
        "--MMMM", // xs:string
        "--MMMM", // xs:untypedAtomic
        "YYYYNN", // xs:hexBinary
        "YYYYNN", // xs:base64Binary
        "YYNNYN", // xs:anyURI
        "YYNNNY", // xs:QName
    ];

    /// <summary>Adds the rows of <c>xs:hexBinary</c>, <c>xs:base64Binary</c>, <c>xs:anyURI</c>, and <c>xs:QName</c>.</summary>
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
                if (cell is not ('Y' or 'M'))
                {
                    continue;
                }

                var kind = cell == 'Y' ? CastCell.Always : CastCell.ValueDependent;
                builder.Add(
                    target == QNameColumn
                        ? new CastRow(types[source], types[target], kind, QNameConversions.ToQName)
                        : new CastRow(types[source], types[target], kind, Conversions[target])
                );
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
