namespace NetEvolve.Schemas.Tests.LegacyCompiler.Schematron;

using System.Collections.Generic;
using System.Reflection;

/// <summary>The error ids of a consumer compilation and, when it compiled, its assembly.</summary>
internal sealed class ConsumerCompilation(IReadOnlyList<string> errors, Assembly? assembly)
{
    internal IReadOnlyList<string> Errors { get; } = errors;

    /// <summary>Calls a static method of the type <c>Probe</c> with one string argument.</summary>
    internal object Invoke(string method, string argument) =>
        assembly!.GetType("Probe")!.GetMethod(method)!.Invoke(null, [argument])!;
}
