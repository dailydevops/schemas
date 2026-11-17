namespace NetEvolve.Schemas.Xml.Tests.Unit.Reader.Core;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Reader.Core;

/// <summary>An entity input stack that records the name and position of every reference and returns a configured result.</summary>
internal sealed class FakeEntityInputStack : IXmlEntityInputStack
{
    internal FakeEntityInputStack(XmlErrorCode? result = null) => Result = result;

    internal XmlErrorCode? Result { get; }

    internal List<(string Name, XmlPosition Position)> Calls { get; } = [];

    public XmlErrorCode? Enter(ReadOnlySpan<char> name, XmlPosition referencePosition)
    {
        Calls.Add((name.ToString(), referencePosition));
        return Result;
    }
}
