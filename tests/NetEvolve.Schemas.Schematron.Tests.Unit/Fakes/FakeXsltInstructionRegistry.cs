namespace NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;

internal sealed class FakeXsltInstructionRegistry : IXsltInstructionRegistry
{
    private readonly Dictionary<XPathQName, IXsltInstruction> _instructions = [];

    internal void Register(IXsltInstruction instruction) => _instructions[instruction.Name] = instruction;

    public bool TryGet(in XPathQName name, out IXsltInstruction instruction) =>
        _instructions.TryGetValue(name, out instruction!);

    public bool IsAvailable(in XPathQName name) => _instructions.ContainsKey(name);
}
