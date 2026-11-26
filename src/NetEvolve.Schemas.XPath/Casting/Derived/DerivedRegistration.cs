namespace NetEvolve.Schemas.XPath.Casting.Derived;

using System.Runtime.CompilerServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.Numeric;
using NetEvolve.Schemas.Xml.Datatypes.Strings;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The registration file of this folder: the rows of the built-in types derived from <c>xs:integer</c> and <c>xs:string</c> as data (Functions
/// and Operators, 17.2 to 17.5). Its module initializer stages <see cref="RegisterRows"/> into the default casting catalog, which the constructor
/// functions of these types follow (<see cref="CastingRegistration"/>).
/// </summary>
/// <remarks>
/// <para>
/// A cast to a derived type goes through its primitive ancestor: a derived integer type is reached from the seven types that cast to
/// <c>xs:integer</c>, and a derived string type from every primitive type, because every one of them casts to <c>xs:string</c>. The cell is always
/// value dependent, because the facets of the type decide. The derived types as a source need no rows of their own: the casting table casts them as
/// their nearest ancestor with a row (<see cref="CastingTable"/>), which makes a cast from <c>xs:byte</c> to <c>xs:unsignedByte</c> a cast from
/// <c>xs:integer</c> to it, checked against the bounds, and a cast from <c>xs:token</c> to <c>xs:integer</c> a cast from <c>xs:string</c>.
/// </para>
/// <para>
/// Left out: the two duration subtypes, which the temporal rows cover; <c>xs:NMTOKENS</c>, <c>xs:IDREFS</c>, and <c>xs:ENTITIES</c>, which are list
/// types and no atomic type; <c>xs:NOTATION</c>, which is abstract; and <c>xs:dateTimeStamp</c>, whose rows the temporal registration holds. A type defined by a schema
/// is not available until schema-aware processing exists (DEF-40, #52).
/// </para>
/// <para>
/// Limitation: a value of a derived integer type beyond the 64 bits of the integer item, which only <c>xs:unsignedLong</c> above 2^63 and the unbounded
/// types have, is a value of the derived type, but its cast to <c>xs:integer</c> raises FOCA0003 like every other integer beyond 64 bits (#265, D3). A
/// string literal only is cast to <c>xs:QName</c> in XPath 2.0 (Functions and Operators, 17.1.1), so a value of a derived string type is XPTY0004 there.
/// </para>
/// <para>The initializer only stages; the table is built, and the type handles are read, when the catalogs freeze on first use.</para>
/// </remarks>
internal static class DerivedRegistration
{
    // The types derived from xs:integer; the bounds are the facets of their datatypes.
    private static readonly string[] IntegerNames =
    [
        "nonPositiveInteger",
        "negativeInteger",
        "long",
        "int",
        "short",
        "byte",
        "nonNegativeInteger",
        "unsignedLong",
        "unsignedInt",
        "unsignedShort",
        "unsignedByte",
        "positiveInteger",
    ];

    // The types derived from xs:string.
    private static readonly string[] StringNames =
    [
        "normalizedString",
        "token",
        "language",
        "Name",
        "NCName",
        "ID",
        "IDREF",
        "ENTITY",
        "NMTOKEN",
    ];

    // The types that cast to xs:integer (Functions and Operators, 17.1): the seven types of the casting core.
    private static readonly string[] IntegerSources =
    [
        "string",
        "untypedAtomic",
        "float",
        "double",
        "decimal",
        "integer",
        "boolean",
    ];

    // The types that cast to xs:string: every primitive type of XPath 2.0, and the two types of the data model that are not primitive but are
    // the source of a cast, xs:untypedAtomic and the two duration subtypes; xs:NOTATION is abstract.
    private static readonly string[] StringSources =
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

    /// <summary>Adds the rows of the types derived from <c>xs:integer</c> and <c>xs:string</c>.</summary>
    /// <param name="builder">The builder.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static void RegisterRows(CastingTableBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var registry = BuiltInDatatypes.For(XsdVersion.V10);
        var integerSources = Types(IntegerSources);
        var stringSources = Types(StringSources);
        for (var i = 0; i < IntegerNames.Length; i++)
        {
            var type = new DerivedIntegerType(
                XdmTypes.Get(IntegerNames[i]),
                (IDatatypeDescriptor<XsdInteger>)registry.Get(IntegerNames[i])
            );
            AddRows(builder, integerSources, XdmTypes.Get(IntegerNames[i]), type.Convert);
        }

        for (var i = 0; i < StringNames.Length; i++)
        {
            var type = new DerivedStringType(
                XdmTypes.Get(StringNames[i]),
                (AtomicDescriptor<XsdString>)registry.Get(StringNames[i])
            );
            AddRows(builder, stringSources, XdmTypes.Get(StringNames[i]), type.Convert);
        }
    }

    private static void AddRows(
        CastingTableBuilder builder,
        XdmTypeId[] sources,
        XdmTypeId target,
        CastConversion conversion
    )
    {
        for (var i = 0; i < sources.Length; i++)
        {
            builder.Add(new CastRow(sources[i], target, CastCell.ValueDependent, conversion));
        }
    }

    private static XdmTypeId[] Types(string[] names)
    {
        var types = new XdmTypeId[names.Length];
        for (var i = 0; i < types.Length; i++)
        {
            types[i] = XdmTypes.Get(names[i]);
        }

        return types;
    }

    // CA2255 is suppressed on this one method: the design approved in #123 (AP8, option B) makes the module initializer the discovery
    // mechanism of the registration files, so that no shared list of rows exists. It only stages; nothing runs until the catalogs freeze.
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Initialize() => CastingCatalog.Default.Stage(RegisterRows);
}
