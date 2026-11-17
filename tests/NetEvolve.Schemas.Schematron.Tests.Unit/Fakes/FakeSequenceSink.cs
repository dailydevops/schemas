namespace NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;

internal sealed class FakeSequenceSink : ISequenceSink
{
    internal List<string> Texts { get; } = [];

    public void Item(in XdmItem item) => Texts.Add("item");

    public void Text(ReadOnlySpan<char> text) => Texts.Add(text.ToString());

    public IXmlSink BeginTree() =>
        throw new NotSupportedException("Temporary trees belong to the XSLT construction package.");

    public XdmItem EndTree(IXmlSink tree) =>
        throw new NotSupportedException("Temporary trees belong to the XSLT construction package.");
}
