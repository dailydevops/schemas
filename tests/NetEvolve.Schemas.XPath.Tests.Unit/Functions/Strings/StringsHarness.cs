namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Strings;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Tests.Unit.Functions.Sequences;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Calls the string functions through the default catalog, the way a compiled call does: resolve by expanded name and arity, then
/// <see cref="XPathFunction.Call"/>. A string result comes back as the string itself; an error as <c>error:CODE</c>; any other result
/// in the form of <see cref="SequencesHarness.Show(XdmSequence)"/>.
/// </summary>
internal static class StringsHarness
{
    /// <summary>The URI of the Unicode codepoint collation.</summary>
    internal const string Codepoint = SequencesHarness.Codepoint;

    /// <summary>One <c>xs:string</c>.</summary>
    internal static XdmSequence Str(string value) => XdmSequence.Create(XdmItem.FromString(value));

    /// <summary>Several <c>xs:string</c> items.</summary>
    internal static XdmSequence Strs(params string[] values)
    {
        var items = new XdmItem[values.Length];
        for (var i = 0; i < items.Length; i++)
        {
            items[i] = XdmItem.FromString(values[i]);
        }

        return XdmSequence.Create(items);
    }

    /// <summary>The integers as a sequence.</summary>
    internal static XdmSequence Ints(params long[] values)
    {
        var items = new XdmItem[values.Length];
        for (var i = 0; i < items.Length; i++)
        {
            items[i] = XdmItem.FromInteger(values[i]);
        }

        return XdmSequence.Create(items);
    }

    /// <summary>One <c>xs:double</c>.</summary>
    internal static XdmSequence Dbl(double value) => XdmSequence.Create(XdmItem.FromDouble(value));

    /// <summary>One <c>xs:integer</c>.</summary>
    internal static XdmSequence Int(long value) => XdmSequence.Create(XdmItem.FromInteger(value));

    /// <summary>Calls a function; a string result is returned as it is.</summary>
    internal static string Text(string local, params XdmSequence[] arguments) => TextIn(null, local, arguments);

    /// <summary>Calls a function with a context item at position 1 of 1; a string result is returned as it is.</summary>
    internal static string TextWithContextItem(XdmItem item, string local, params XdmSequence[] arguments) =>
        TextIn(context => context.SetContextItem(item), local, arguments);

    /// <summary>Calls a function; the result is shown with the type of each item.</summary>
    internal static string Shown(string local, params XdmSequence[] arguments) =>
        SequencesHarness.Run(local, arguments);

    /// <summary>Calls a function and returns the sequence; an error is raised as it is.</summary>
    internal static XdmSequence Call(string local, params XdmSequence[] arguments) =>
        SequencesHarness.CallWith(local, arguments);

    /// <summary>The code points of a string, shown as hexadecimal numbers separated by a space.</summary>
    internal static string Hex(string text)
    {
        Argument.ThrowIfNull(text);

        var parts = new System.Collections.Generic.List<string>();
        for (var i = 0; i < text.Length; i++)
        {
            var codePoint =
                char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1])
                    ? char.ConvertToUtf32(text[i], text[++i])
                    : text[i];
            parts.Add(codePoint.ToString("X4", System.Globalization.CultureInfo.InvariantCulture));
        }

        return string.Join(" ", parts);
    }

    private static string TextIn(Action<XPathEvaluationContext>? setup, string local, XdmSequence[] arguments)
    {
        if (!SequencesHarness.TryResolve(local, arguments.Length, out var function))
        {
            return "error:not-registered";
        }

        try
        {
            using var context = new XPathEvaluationContext();
            setup?.Invoke(context);
            var result = function!.Call(context, arguments);
            return result.Count == 1 && result[0].TryGetString(out var text) ? text! : SequencesHarness.Show(result);
        }
        catch (XPathException failure)
        {
            return "error:" + failure.Diagnostic.Code;
        }
    }
}
