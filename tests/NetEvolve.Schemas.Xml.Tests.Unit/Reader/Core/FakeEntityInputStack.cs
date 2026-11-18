namespace NetEvolve.Schemas.Xml.Tests.Unit.Reader.Core;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Reader.Core;

/// <summary>An entity input stack that records every reference and returns a configured result and replacement text.</summary>
internal sealed class FakeEntityInputStack : IXmlEntityInputStack
{
    internal FakeEntityInputStack(XmlErrorCode? result = null, string? replacement = null)
    {
        Result = result;
        Replacement = replacement;
    }

    internal XmlErrorCode? Result { get; }

    internal string? Replacement { get; }

    internal List<(string Name, bool InAttributeValue, bool Standalone)> Calls { get; } = [];

    internal int Leaves { get; private set; }

    public XmlErrorCode? Enter(ReadOnlySpan<char> name, bool inAttributeValue, bool standalone, out string? replacement)
    {
        Calls.Add((name.ToString(), inAttributeValue, standalone));
        replacement = Result is null ? Replacement : null;

        return Result;
    }

    public void Leave() => Leaves++;
}
