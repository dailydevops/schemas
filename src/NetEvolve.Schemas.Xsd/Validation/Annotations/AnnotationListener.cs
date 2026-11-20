namespace NetEvolve.Schemas.Xsd.Validation.Annotations;

using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>
/// The listener that hands the type, validity, and nilled state of every assessed element and attribute to an annotation sink
/// (XSD-16, #305; design #146 DP-18). Without a sink it asks for no hook, so a run that nobody reads annotations from does no
/// annotation work and allocates nothing for it.
/// </summary>
/// <remarks>
/// <para>
/// An element is annotated at its end tag, when its validity is known; the listener must therefore come after every listener that
/// can make an element invalid (design #146 DP-11 puts it last). An attribute is annotated when it is reported, with its index among the
/// attributes of its element in the order of the hooks: the specified attributes in source order, then the defaulted ones. A node that
/// has no type, because the validator did not assess it, gets no record and reads as not known. The nilled state counts only for a valid
/// element.
/// </para>
/// <para>
/// Limitations: the validator reports <c>isNilled: false</c> until <c>xsi:nil</c> is evaluated (XSD-55, #308), and it hands no hook for the
/// attributes of the schema instance namespace that control assessment, so those attributes have no index and no record. Validity is
/// valid or invalid; <c>[validation attempted]</c> partial, which a lax or skipped descendant causes, is not tracked, so such an element is
/// annotated as fully assessed.
/// </para>
/// <para>Not thread-safe; one instance serves one run at a time and may be reused for the next.</para>
/// </remarks>
internal sealed class AnnotationListener : ValidationListener
{
    private readonly IAnnotationSink? _sink;
    private int _owner;
    private int _index;

    /// <summary>Initializes a new instance of the <see cref="AnnotationListener"/> class.</summary>
    /// <param name="sink">The sink, <see langword="null"/> for none: the listener then wants no hook.</param>
    internal AnnotationListener(IAnnotationSink? sink) => _sink = sink;

    /// <inheritdoc />
    public override ListenerInterest Interest =>
        _sink is null ? ListenerInterest.None : ListenerInterest.Attribute | ListenerInterest.EndElement;

    /// <inheritdoc />
    public override void BeginRun(in ValidationRunContext context)
    {
        _owner = 0;
        _index = 0;
    }

    /// <inheritdoc />
    public override void Attribute(in AttributeInfo info)
    {
        var index = info.OwnerOrdinal == _owner ? _index : 0;
        _owner = info.OwnerOrdinal;
        _index = index + 1;
        if (info.Type is not null)
        {
            _sink!.Attribute(
                new AnnotationRecord(
                    info.OwnerOrdinal,
                    index,
                    info.Type,
                    info.IsValid ? XsdValidity.Valid : XsdValidity.Invalid,
                    false
                )
            );
        }
    }

    /// <inheritdoc />
    public override void EndElement(in ElementEndInfo info)
    {
        if (info.Type is not null)
        {
            _sink!.Element(
                new AnnotationRecord(
                    info.Ordinal,
                    -1,
                    info.Type,
                    info.IsValid ? XsdValidity.Valid : XsdValidity.Invalid,
                    info.IsValid && info.IsNilled
                )
            );
        }
    }
}
