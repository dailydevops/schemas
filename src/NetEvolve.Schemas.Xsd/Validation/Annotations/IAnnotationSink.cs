namespace NetEvolve.Schemas.Xsd.Validation.Annotations;

/// <summary>
/// The consumer of the annotations that the streaming validator produces (design #146 DP-18). The validator feeds it through the
/// <see cref="AnnotationListener"/>; nothing is produced, and nothing is allocated, for a run without a sink.
/// </summary>
internal interface IAnnotationSink
{
    /// <summary>Receives the annotation of an element once its validity is known, at its end tag.</summary>
    /// <param name="record">The record.</param>
    void Element(in AnnotationRecord record);

    /// <summary>Receives the annotation of an attribute.</summary>
    /// <param name="record">The record.</param>
    void Attribute(in AnnotationRecord record);
}
