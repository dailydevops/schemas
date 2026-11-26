namespace NetEvolve.Schemas.Xml.Contracts;

/// <summary>
/// A reader that tells a sink where the event it is delivering began. The events of <see cref="IXmlSink"/> carry a position for
/// the start of an element only; a sink that needs the position of an attribute, of text, or of an end tag asks the reader
/// that pumps it, from inside the callback.
/// </summary>
/// <remarks>
/// The readers of this package implement the interface; a sink learns of the source from its creator, because
/// <see cref="IXmlReader.Pump{TSink}"/> does not pass the reader to the sink. The value is meaningful only while a sink
/// callback runs and describes the event of that callback.
/// </remarks>
public interface IXmlPositionSource
{
    /// <summary>
    /// Gets the position at which the event that is being delivered began: the name of an attribute, the first character of a
    /// text event, the <c>&lt;/</c> of an end tag (the start tag of an element written as an empty-element tag), and the start
    /// of a comment, processing instruction, or element otherwise. Inside the replacement text of an entity it is the position
    /// of the reference.
    /// </summary>
    XmlPosition Current { get; }
}
