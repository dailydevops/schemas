namespace NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;

internal sealed class FakeXsltInstruction(XPathQName name) : IXsltInstruction
{
    internal FakeXsltExecutable Executable { get; } = new();

    public XPathQName Name { get; } = name;

    public bool TryCompile(XsltCompileContext context, out XsltExecutable executable, out SchematronProblem problem)
    {
        executable = Executable;
        problem = default;
        return true;
    }
}
