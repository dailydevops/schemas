namespace NetEvolve.Schemas.Xsd.Validation.Annotations;

using System;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>The listener that hands the type, validity, and nilled state of every assessed element and attribute to an annotation sink.</summary>
internal sealed class AnnotationListener : ValidationListener
{
    private readonly IAnnotationSink? _sink;

    /// <summary>Initializes a new instance of the <see cref="AnnotationListener"/> class.</summary>
    /// <param name="sink">The sink, <see langword="null"/> for none: the listener then wants no hook.</param>
    internal AnnotationListener(IAnnotationSink? sink) => _sink = sink;

    /// <inheritdoc />
    public override ListenerInterest Interest =>
        _sink is null ? ListenerInterest.None : throw new NotSupportedException("Not implemented yet.");
}
