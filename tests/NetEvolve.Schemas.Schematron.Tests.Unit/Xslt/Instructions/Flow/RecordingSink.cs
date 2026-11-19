namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Flow;

using System;
using System.Collections.Generic;
using System.Text;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>A sink that records every push as one entry: <c>item:value</c> or <c>text:value</c>.</summary>
internal sealed class RecordingSink : ISequenceSink
{
    internal List<string> Events { get; } = [];

    /// <summary>Gets the text pushed, in order, without the items.</summary>
    internal string Joined()
    {
        var joined = new StringBuilder();
        foreach (var entry in Events)
        {
            if (entry.StartsWith("text:", StringComparison.Ordinal))
            {
                _ = joined.Append(entry, 5, entry.Length - 5);
            }
        }

        return joined.ToString();
    }

    /// <summary>Gets all entries joined with a vertical bar.</summary>
    internal string All() => string.Join("|", Events);

    public SequenceSinkKind Kind => SequenceSinkKind.Items;

    public IXmlSink Content => throw new InvalidOperationException("No content.");

    public void Item(in XdmItem item)
    {
        var text = new StringBuilder("item:");
        if (item.TryGetInteger(out var integer))
        {
            _ = text.Append(integer);
        }
        else if (item.TryGetString(out var value))
        {
            _ = text.Append(value);
        }
        else
        {
            _ = text.Append(item.Kind);
        }

        Events.Add(text.ToString());
    }

    public void Text(ReadOnlySpan<char> text) => Events.Add("text:" + text.ToString());

    public IXmlSink BeginTree() => throw new InvalidOperationException("No trees.");

    public XdmItem EndTree(IXmlSink tree) => throw new InvalidOperationException("No trees.");
}
