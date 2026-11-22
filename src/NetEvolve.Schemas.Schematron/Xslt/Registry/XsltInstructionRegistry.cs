namespace NetEvolve.Schemas.Schematron.Xslt.Registry;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// The registry of XSLT instructions, keyed by expanded name (design #150 EP-9). Recognition is by namespace URI, never by
/// prefix. The instance is immutable; it is created by <see cref="XsltRegistryBuilder"/>.
/// </summary>
/// <remarks>
/// Limitation: no instruction is registered yet, so every XSLT element is <see cref="XsltElementClass.Unregistered"/>; the
/// instruction packages register theirs.
/// </remarks>
internal sealed class XsltInstructionRegistry : IXsltInstructionRegistry
{
    /// <summary>The XSLT namespace URI.</summary>
    internal const string Namespace = "http://www.w3.org/1999/XSL/Transform";

    /// <summary>The number of <see cref="XsltSyntheticElement"/> members.</summary>
    internal const int SyntheticKinds = 2;

    private readonly Dictionary<XPathQName, IXsltInstruction> _named;
    private readonly IXsltInstruction?[] _synthetic;

    internal XsltInstructionRegistry(Dictionary<XPathQName, IXsltInstruction> named, IXsltInstruction?[] synthetic)
    {
        ArgumentNullException.ThrowIfNull(named);
        ArgumentNullException.ThrowIfNull(synthetic);

        _named = named;
        _synthetic = synthetic;
    }

    /// <inheritdoc />
    public XsltElementClass Classify(in XPathQName name, XsltPosition where, out IXsltInstruction instruction)
    {
        if (!string.Equals(name.NamespaceName, Namespace, StringComparison.Ordinal))
        {
            instruction = null!;
            return XsltElementClass.NotXslt;
        }

        if (!_named.TryGetValue(name, out instruction!))
        {
            return XsltElementClass.Unregistered;
        }

        return (instruction.Position & where) != XsltPosition.None
            ? XsltElementClass.Registered
            : XsltElementClass.Misplaced;
    }

    /// <inheritdoc />
    public bool TryGet(in XPathQName name, out IXsltInstruction instruction) =>
        _named.TryGetValue(name, out instruction!);

    /// <inheritdoc />
    public bool TryGetSynthetic(XsltSyntheticElement kind, out IXsltInstruction instruction)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan((int)kind, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((int)kind, SyntheticKinds);

        instruction = _synthetic[(int)kind]!;
        return instruction is not null;
    }

    /// <inheritdoc />
    public bool IsAvailable(in XPathQName name) =>
        _named.TryGetValue(name, out var instruction)
        && (instruction.Position & XsltPosition.Instruction) != XsltPosition.None;
}
