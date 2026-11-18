namespace NetEvolve.Schemas.XPath.Tests.Unit.Casting.Temporal;

using System;
using NetEvolve.Schemas.XPath.Tests.Unit.Casting.Core;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>Renders the result of an expression as the kind of its item and its canonical string value, so that one string tells type and value apart.</summary>
internal static class CastShow
{
    /// <summary>The kind and the string value of the single item of the result, or <c>count:n</c> for another cardinality.</summary>
    public static string Show(string expression)
    {
        var result = CastFixture.Evaluate(expression);
        if (result.Count != 1)
        {
            return "count:" + result.Count;
        }

        var kind = result[0].Kind;
        var text = CastFixture.Evaluate("(" + expression + ") cast as xs:string");
        _ = text[0].TryGetString(out var value);
        return kind + ":" + value;
    }

    /// <summary>The kind of the single item of the result.</summary>
    public static XdmItemKind KindOf(string expression)
    {
        var result = CastFixture.Evaluate(expression);
        return result.Count == 1 ? result[0].Kind : throw new InvalidOperationException("count:" + result.Count);
    }

    /// <summary>Whether the cast of the expression is allowed, by evaluating <c>castable as</c>.</summary>
    public static bool Castable(string expression, string type)
    {
        var result = CastFixture.Evaluate("(" + expression + ") castable as xs:" + type);
        _ = result[0].TryGetBoolean(out var value);
        return value;
    }
}
