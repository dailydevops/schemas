namespace NetEvolve.Schemas.Schematron.Evaluation.Assertions;

using System;
using NetEvolve.Schemas.Schematron.Diagnostics;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The <see cref="SequenceSinkKind.Text"/> sink: a pooled character buffer that holds the text of one message.</summary>
internal sealed class TextSink : ISequenceSink, IDisposable
{
    internal TextSink(int maxLength) => throw NotImplementedFailure.For(GetType().Name);

    public SequenceSinkKind Kind => throw NotImplementedFailure.For(GetType().Name);

    public IXmlSink Content => throw NotImplementedFailure.For(GetType().Name);

    /// <summary>Gets the text written so far; valid until the next write or <see cref="Clear"/>.</summary>
    internal ReadOnlySpan<char> Written => throw NotImplementedFailure.For(GetType().Name);

    /// <summary>Gets a value indicating whether text was cut at the maximum length.</summary>
    internal bool IsTruncated => throw NotImplementedFailure.For(GetType().Name);

    /// <summary>Gets the reason the last atomic value could not be converted, or <see langword="null"/>.</summary>
    internal string? Failure => throw NotImplementedFailure.For(GetType().Name);

    public void Item(in XdmItem item) => throw NotImplementedFailure.For(GetType().Name);

    public void Text(ReadOnlySpan<char> text) => throw NotImplementedFailure.For(GetType().Name);

    public IXmlSink BeginTree() => throw NotImplementedFailure.For(GetType().Name);

    public XdmItem EndTree(IXmlSink tree) => throw NotImplementedFailure.For(GetType().Name);

    /// <summary>Empties the sink and keeps its buffer for the next message.</summary>
    internal void Clear() => throw NotImplementedFailure.For(GetType().Name);

    /// <inheritdoc />
    public override string ToString() => throw NotImplementedFailure.For(GetType().Name);

    /// <inheritdoc />
    public void Dispose() => throw NotImplementedFailure.For(GetType().Name);
}
