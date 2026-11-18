namespace NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;

internal sealed class FakeXsltInstructionRegistry : IXsltInstructionRegistry
{
    private const string XsltNamespace = "http://www.w3.org/1999/XSL/Transform";

    private readonly Dictionary<XPathQName, IXsltInstruction> _instructions = [];
    private readonly Dictionary<XsltSyntheticElement, IXsltInstruction> _synthetic = [];

    internal void Register(IXsltInstruction instruction) => _instructions[instruction.Name] = instruction;

    internal void RegisterSynthetic(XsltSyntheticElement kind, IXsltInstruction instruction) =>
        _synthetic[kind] = instruction;

    public XsltElementClass Classify(in XPathQName name, XsltPosition where, out IXsltInstruction instruction)
    {
        if (!string.Equals(name.NamespaceName, XsltNamespace, StringComparison.Ordinal))
        {
            instruction = null!;
            return XsltElementClass.NotXslt;
        }

        if (!_instructions.TryGetValue(name, out instruction!))
        {
            return XsltElementClass.Unregistered;
        }

        return (instruction.Position & where) != XsltPosition.None
            ? XsltElementClass.Registered
            : XsltElementClass.Misplaced;
    }

    public bool TryGet(in XPathQName name, out IXsltInstruction instruction) =>
        _instructions.TryGetValue(name, out instruction!);

    public bool TryGetSynthetic(XsltSyntheticElement kind, out IXsltInstruction instruction) =>
        _synthetic.TryGetValue(kind, out instruction!);

    public bool IsAvailable(in XPathQName name) =>
        _instructions.TryGetValue(name, out var instruction)
        && (instruction.Position & XsltPosition.Instruction) != XsltPosition.None;
}
