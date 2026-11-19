namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Numerics;

using System;
using System.Globalization;
using System.Numerics;
using System.Text;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Tests.Unit.Functions.Sequences;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Calls the boolean and numeric functions through the default catalog, the way a compiled call does: resolve by expanded name and
/// arity, then <see cref="XPathFunction.Call"/>. An argument is written <c>kind:payload</c> as in
/// <see cref="SequencesHarness"/>, where an <c>integer</c> may be of any width, and <c>()</c> is the empty sequence. A result is shown
/// with the type of each item, an error as <c>error:CODE</c>. Unlike the shared harness, an integer beyond 64 bits is shown as
/// <c>integer:</c> and not as <c>decimal:</c>, so that a result of the wrong type cannot hide.
/// </summary>
internal static class NumericsHarness
{
    internal const string FunctionNamespace = SequencesHarness.FunctionNamespace;

    /// <summary>Resolves a built-in function of the default catalog for a language version.</summary>
    internal static bool TryResolve(
        string local,
        int arity,
        out XPathFunction? function,
        XPathVersion version = XPathVersion.V20
    ) =>
        FunctionCatalog.Default.Registry.TryResolve(
            XPathStaticContext.Default.ToBuilder().WithVersion(version).Build(),
            new XPathQName(FunctionNamespace, local),
            arity,
            XPathHostRequirements.None,
            out function
        );

    /// <summary>Calls a function in a fresh evaluation context without a focus.</summary>
    internal static string Run(string local, params string[] arguments) => RunWith(null, local, arguments);

    /// <summary>Calls a function with the given context item, at position 1 of 1.</summary>
    internal static string RunWithContextItem(string contextItem, string local, params string[] arguments) =>
        RunWith(context => context.SetContextItem(ParseItem(contextItem)), local, arguments);

    /// <summary>Calls a function after <paramref name="setup"/> configured the evaluation context.</summary>
    internal static string RunWith(Action<XPathEvaluationContext>? setup, string local, params string[] arguments)
    {
        if (!TryResolve(local, arguments.Length, out var function))
        {
            return "error:not-registered";
        }

        var sequences = new XdmSequence[arguments.Length];
        for (var i = 0; i < sequences.Length; i++)
        {
            sequences[i] = Parse(arguments[i]);
        }

        try
        {
            using var context = new XPathEvaluationContext();
            setup?.Invoke(context);
            return Show(function!.Call(context, sequences));
        }
        catch (XPathException failure)
        {
            return "error:" + failure.Diagnostic.Code;
        }
    }

    /// <summary>Parses a sequence: <c>()</c> is empty, items are joined by <c>|</c>.</summary>
    internal static XdmSequence Parse(string argument)
    {
        if (string.Equals(argument, "()", StringComparison.Ordinal))
        {
            return XdmSequence.Empty;
        }

        var parts = argument.Split('|');
        var items = new XdmItem[parts.Length];
        for (var i = 0; i < items.Length; i++)
        {
            items[i] = ParseItem(parts[i]);
        }

        return XdmSequence.Create(items);
    }

    /// <summary>Parses one item written <c>kind:payload</c>; an <c>integer</c> may be of any width.</summary>
    internal static XdmItem ParseItem(string text) =>
        text.StartsWith("integer:", StringComparison.Ordinal)
            ? XdmItem.FromBigInteger(
                BigInteger.Parse(text.AsSpan(8), NumberStyles.Integer, CultureInfo.InvariantCulture)
            )
            : SequencesHarness.ParseItem(text);

    /// <summary>Shows a result sequence: each item with its type, separated by a comma.</summary>
    internal static string Show(XdmSequence sequence)
    {
        var text = new StringBuilder();
        for (var i = 0; i < sequence.Count; i++)
        {
            if (i > 0)
            {
                _ = text.Append(',');
            }

            var item = sequence[i];
            _ =
                item.Kind == XdmItemKind.XsDecimal && item.TypeId == XdmTypeId.XsInteger
                    ? text.Append("integer:")
                        .Append(
                            (item.TryGetBigInteger(out var wide) ? wide : default).ToString(
                                CultureInfo.InvariantCulture
                            )
                        )
                    : text.Append(SequencesHarness.Show(XdmSequence.Create(item)));
        }

        return text.ToString();
    }
}
