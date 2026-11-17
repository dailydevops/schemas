namespace NetEvolve.Schemas.Schematron.Seams;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>Receives a constructed sequence: items, text, and temporary trees.</summary>
internal interface ISequenceSink
{
    /// <summary>Gets the mode of the sink.</summary>
    SequenceSinkKind Kind { get; }

    /// <summary>Gets the events of the tree under construction.</summary>
    /// <exception cref="InvalidOperationException">The sink is not of kind <see cref="SequenceSinkKind.Tree"/>.</exception>
    IXmlSink Content { get; }

    /// <summary>Adds an atomic value or node without copying it.</summary>
    void Item(in XdmItem item);

    /// <summary>Adds text; adjacent text coalesces.</summary>
    void Text(ReadOnlySpan<char> text);

    /// <summary>Begins a temporary tree.</summary>
    IXmlSink BeginTree();

    /// <summary>Completes a temporary tree and returns its document node as an item.</summary>
    XdmItem EndTree(IXmlSink tree);
}
