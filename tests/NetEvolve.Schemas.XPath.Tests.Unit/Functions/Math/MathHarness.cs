namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Math;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>Calls the functions of the <c>math</c> namespace through the default catalog, the way a compiled call does.</summary>
internal static class MathHarness
{
    internal const string MathNamespace = "http://www.w3.org/2005/xpath-functions/math";

    /// <summary>Resolves a function of the <c>math</c> namespace for a language version.</summary>
    internal static bool TryResolve(string local, int arity, out XPathFunction? function, XPathVersion version) =>
        FunctionCatalog.Default.Registry.TryResolve(
            XPathStaticContext.Default.ToBuilder().WithVersion(version).Build(),
            new XPathQName(MathNamespace, local),
            arity,
            XPathHostRequirements.None,
            out function
        );

    /// <summary>Calls a function of the <c>math</c> namespace in XPath 3.1.</summary>
    internal static XdmSequence Call(string local, params XdmSequence[] arguments)
    {
        if (!TryResolve(local, arguments.Length, out var function, XPathVersion.V31))
        {
            throw new InvalidOperationException($"math:{local}#{arguments.Length} is not registered.");
        }

        using var context = new XPathEvaluationContext();
        return function!.Call(context, arguments);
    }

    /// <summary>Calls a function with <c>xs:double</c> arguments and returns its one <c>xs:double</c> result.</summary>
    internal static double CallDouble(string local, params double[] arguments)
    {
        var sequences = new XdmSequence[arguments.Length];
        for (var i = 0; i < sequences.Length; i++)
        {
            sequences[i] = XdmSequence.Create(XdmItem.FromDouble(arguments[i]));
        }

        var result = Call(local, sequences);
        return result.Count == 1 && result[0].TryGetDouble(out var value)
            ? value
            : throw new InvalidOperationException($"math:{local} did not return one xs:double.");
    }

    /// <summary>Gets the code of the error a call raises.</summary>
    internal static string ErrorOf(string local, params XdmSequence[] arguments)
    {
        try
        {
            _ = Call(local, arguments);
            return "none";
        }
        catch (XPathException failure)
        {
            return failure.Diagnostic.Code;
        }
    }

    /// <summary>Compares two doubles bit for bit; every NaN is the same value.</summary>
    internal static bool Same(double actual, double expected) =>
        double.IsNaN(expected)
            ? double.IsNaN(actual)
            : BitConverter.DoubleToInt64Bits(actual) == BitConverter.DoubleToInt64Bits(expected);
}
