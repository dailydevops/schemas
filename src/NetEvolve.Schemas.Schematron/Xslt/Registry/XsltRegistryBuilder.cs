namespace NetEvolve.Schemas.Schematron.Xslt.Registry;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// Collects the entries of one <see cref="XsltInstructionRegistry"/>. A name registered twice, or a name outside the XSLT
/// namespace, is a programming error of the registering package and fails at once (design #150 EP-10).
/// </summary>
internal sealed class XsltRegistryBuilder
{
    private readonly Dictionary<XPathQName, IXsltInstruction> _named = [];
    private readonly IXsltInstruction?[] _synthetic = new IXsltInstruction?[XsltInstructionRegistry.SyntheticKinds];

    /// <summary>Adds an instruction under its expanded name.</summary>
    /// <param name="instruction">The instruction.</param>
    /// <exception cref="InvalidOperationException">The name is not in the XSLT namespace or is already registered.</exception>
    internal void Add(IXsltInstruction instruction)
    {
        ArgumentNullException.ThrowIfNull(instruction);

        var name = instruction.Name;
        if (
            !string.Equals(name.NamespaceName, XsltInstructionRegistry.Namespace, StringComparison.Ordinal)
            || name.LocalName.Length == 0
        )
        {
            throw new InvalidOperationException(
                $"The XSLT instruction name '{name}' is not an expanded name in the XSLT namespace."
            );
        }

        if (_named.ContainsKey(name))
        {
            throw new InvalidOperationException($"The XSLT instruction '{name}' is registered more than once.");
        }

        _named.Add(name, instruction);
    }

    /// <summary>Adds the entry of a construct that has no XSLT name.</summary>
    /// <param name="kind">The construct.</param>
    /// <param name="instruction">The instruction.</param>
    /// <exception cref="InvalidOperationException">The construct is already registered.</exception>
    internal void AddSynthetic(XsltSyntheticElement kind, IXsltInstruction instruction)
    {
        ArgumentNullException.ThrowIfNull(instruction);
        ArgumentOutOfRangeException.ThrowIfLessThan((int)kind, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((int)kind, XsltInstructionRegistry.SyntheticKinds);

        if (_synthetic[(int)kind] is not null)
        {
            throw new InvalidOperationException($"The XSLT construct '{kind}' is registered more than once.");
        }

        _synthetic[(int)kind] = instruction;
    }

    /// <summary>Creates a registry from a copy of the entries added so far.</summary>
    /// <returns>The registry.</returns>
    internal XsltInstructionRegistry Build() =>
        new(new Dictionary<XPathQName, IXsltInstruction>(_named), (IXsltInstruction?[])_synthetic.Clone());
}
