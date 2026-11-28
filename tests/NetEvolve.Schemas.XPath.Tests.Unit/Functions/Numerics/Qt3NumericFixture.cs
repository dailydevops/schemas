namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Numerics;

using System;
using NetEvolve.Schemas.XPath.Tests.Unit.Functions.Sequences;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Checks the result of a QT3 case against its assertion: the assertions of <see cref="Qt3SequenceFixture"/>, plus <c>Y:</c> for an item
/// of a type (<c>assert-type</c>), which the numeric functions need because the result type is part of their specification. An
/// expectation is a tab-separated list of alternatives; an alternative is a list of assertions that all hold (<c>all-of</c>), separated
/// by the unit separator (U+001F).
/// </summary>
internal static class Qt3NumericFixture
{
    /// <summary>Checks a result against the alternatives of an expectation.</summary>
    public static bool Satisfies(string expectation, XdmSequence result, string code)
    {
        ArgumentNullException.ThrowIfNull(expectation);

        foreach (var alternative in expectation.Split('\t'))
        {
            var all = true;
            foreach (var assertion in alternative.Split((char)0x1F))
            {
                if (!Holds(assertion, result, code))
                {
                    all = false;
                    break;
                }
            }

            if (all)
            {
                return true;
            }
        }

        return false;
    }

    private static bool Holds(string assertion, XdmSequence result, string code) =>
        assertion.StartsWith("Y:", StringComparison.Ordinal)
            ? code.Length == 0 && result.Count == 1 && IsInstanceOf(result[0], assertion.Substring(2))
            : Qt3SequenceFixture.Satisfies(assertion, result, code);

    // An integer beyond 64 bits is a decimal item that carries the xs:integer type; xs:integer is a subtype of xs:decimal.
    private static bool IsInstanceOf(in XdmItem item, string type)
    {
        var integer =
            item.Kind == XdmItemKind.XsInteger
            || (item.Kind == XdmItemKind.XsDecimal && item.TypeId == XdmTypeId.XsInteger);
        return type switch
        {
            "xs:integer" => integer,
            "xs:decimal" => integer || item.Kind == XdmItemKind.XsDecimal,
            "xs:float" => item.Kind == XdmItemKind.XsFloat,
            "xs:double" => item.Kind == XdmItemKind.XsDouble,
            "xs:boolean" => item.Kind == XdmItemKind.Boolean,
            "xs:string" => item.Kind == XdmItemKind.XsString,
            // A type of the derived types (xs:int, xs:token, and the like): the type annotation of the item derives from it.
            _ => type.StartsWith("xs:", StringComparison.Ordinal)
                && XdmTypes.TryGet(XdmTypes.SchemaNamespace, type.Substring(3), out var id)
                && XdmTypes.IsDerivedFrom(item.TypeId, id),
        };
    }
}
