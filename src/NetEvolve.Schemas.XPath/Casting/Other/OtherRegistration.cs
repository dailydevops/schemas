namespace NetEvolve.Schemas.XPath.Casting.Other;

using System.Runtime.CompilerServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The registration file of this folder: the rows of <c>xs:hexBinary</c>, <c>xs:base64Binary</c>, and <c>xs:anyURI</c> as data. Its module
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
    private static readonly string[] Names = ["string", "untypedAtomic", "hexBinary", "base64Binary", "anyURI"];

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
    // xs:anyURI with certainty: every string is a URI.
    private static readonly string[] Matrix =
    [
        "--MMY", // xs:string
        "--MMY", // xs:untypedAtomic
        "YYYYN", // xs:hexBinary
        "YYYYN", // xs:base64Binary
        "YYNNY", // xs:anyURI
    ];

    /// <summary>Adds the rows of <c>xs:hexBinary</c>, <c>xs:base64Binary</c>, and <c>xs:anyURI</c>.</summary>
    /// <param name="builder">The builder.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static void RegisterRows(CastingTableBuilder builder)
    {
        Argument.ThrowIfNull(builder);

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
