namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Static;

using System.Runtime.InteropServices;
using NetEvolve.Schemas.Conformance.Results;

/// <summary>The verdict of one assertion check.</summary>
/// <param name="Outcome">The outcome.</param>
/// <param name="Reason">The failure message or skip reason.</param>
[StructLayout(LayoutKind.Auto)]
internal readonly record struct AssertionVerdict(CaseOutcome Outcome, string? Reason = null);
