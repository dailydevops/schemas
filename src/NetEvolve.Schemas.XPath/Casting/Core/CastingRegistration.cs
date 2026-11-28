namespace NetEvolve.Schemas.XPath.Casting.Core;

using System.Runtime.CompilerServices;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The registration file of this folder: the rows of the seven types as data, and the staging of the rows and of the constructor
/// functions generated from the table.
/// </summary>
/// <remarks>
/// The constructor functions are built when the function catalog freezes, from the table as it is then, so a row that a later package
/// stages for another target type gets its constructor without a change here. Staging order: that build reads the casting table, so the
/// first read of the function catalog freezes the casting table too, and a module initializer of another package that stages rows
/// afterwards gets an <see cref="System.InvalidOperationException"/> from <see cref="CastingCatalog.Stage"/>. A package that owns rows stages them from its
/// module initializer, which runs before any use of either catalog.
/// </remarks>
internal static class CastingRegistration
{
    // The source and target types of the rows, in the order of the matrix and of the conversions below.
    private static readonly XdmTypeId[] Types =
    [
        XdmTypeId.XsString,
        XdmTypeId.UntypedAtomic,
        XdmTypeId.XsFloat,
        XdmTypeId.XsDouble,
        XdmTypeId.XsDecimal,
        XdmTypeId.XsInteger,
        XdmTypeId.Boolean,
    ];

    // The conversion into each target type.
    private static readonly CastConversion[] Conversions =
    [
        PrimitiveConversions.ToStringType,
        PrimitiveConversions.ToUntypedAtomic,
        PrimitiveConversions.ToFloat,
        PrimitiveConversions.ToDouble,
        PrimitiveConversions.ToDecimal,
        PrimitiveConversions.ToInteger,
        PrimitiveConversions.ToBoolean,
    ];

    // XQuery 1.0 and XPath 2.0 Functions and Operators, 17.1: one row per source type, one column per target type;
    // Y always succeeds, M depends on the value. There is no cell that is not allowed among these types.
    // Limitation: a decimal or a float or double cast to xs:integer can still reach the 64 bits of the integer item (FOCA0003).
    private static readonly string[] Matrix =
    [
        "YYMMMMM", // xs:string
        "YYMMMMM", // xs:untypedAtomic
        "YYYYMMY", // xs:float
        "YYYYMMY", // xs:double
        "YYYYYYY", // xs:decimal
        "YYYYYYY", // xs:integer
        "YYYYYYY", // xs:boolean
    ];

    /// <summary>Adds the rows of the seven types.</summary>
    /// <param name="builder">The builder.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static void RegisterRows(CastingTableBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        for (var source = 0; source < Types.Length; source++)
        {
            for (var target = 0; target < Types.Length; target++)
            {
                var cell = Matrix[source][target] == 'Y' ? CastCell.Always : CastCell.ValueDependent;
                builder.Add(new CastRow(Types[source], Types[target], cell, Conversions[target]));
            }
        }
    }

    // CA2255 is suppressed on this one method: the design approved in #123 (AP8, option B) makes the module initializer the discovery
    // mechanism of the registration files, so that no shared list of function groups or rows exists. It only stages; nothing runs until
    // the catalogs freeze on first use.
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Initialize()
    {
        CastingCatalog.Default.Stage(RegisterRows);
        FunctionCatalog.Default.Stage(static builder =>
        {
            foreach (var function in ConstructorFunctions.Create(CastingCatalog.Default.Table))
            {
                builder.Add(function);
            }
        });
    }
}
