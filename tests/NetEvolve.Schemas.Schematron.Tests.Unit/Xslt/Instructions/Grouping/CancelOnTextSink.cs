namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Grouping;

using System;
using System.Threading;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>A sink that cancels a token when the first text arrives, so that a test cancels in the middle of an instruction.</summary>
internal sealed class CancelOnTextSink(CancellationTokenSource source) : ISequenceSink
{
    /// <summary>Gets the number of texts that arrived.</summary>
    internal int Texts { get; private set; }

    public SequenceSinkKind Kind => SequenceSinkKind.Items;

    public IXmlSink Content => throw new InvalidOperationException("No content.");

    public void Item(in XdmItem item) { }

    public void Text(ReadOnlySpan<char> text)
    {
        Texts++;
        source.Cancel();
    }

    public IXmlSink BeginTree() => throw new InvalidOperationException("No trees.");

    public XdmItem EndTree(IXmlSink tree) => throw new InvalidOperationException("No trees.");
}
