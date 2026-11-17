namespace NetEvolve.Schemas.Xsd.Contracts;

/// <summary>
/// A hook of the streaming validator. Per run the validator calls <see cref="BeginRun"/>, then for every element
/// <see cref="StartElement"/>, one <see cref="Attribute"/> per attribute, <see cref="SimpleValue"/> for simple content, and
/// <see cref="EndElement"/>, and finally <see cref="EndRun"/>. A hook outside <see cref="Interest"/> is never called.
/// </summary>
internal abstract class ValidationListener
{
    /// <summary>Gets the hooks this listener wants.</summary>
    public abstract ListenerInterest Interest { get; }

    /// <summary>Called before the first event.</summary>
    /// <param name="context">The run context.</param>
    public virtual void BeginRun(in ValidationRunContext context) { }

    /// <summary>Called at the end of the attributes, after declaration, type, and xsi are decided.</summary>
    /// <param name="info">The element.</param>
    public virtual void StartElement(in ElementStartInfo info) { }

    /// <summary>Called per attribute, specified or defaulted, after <see cref="StartElement"/>.</summary>
    /// <param name="info">The attribute.</param>
    public virtual void Attribute(in AttributeInfo info) { }

    /// <summary>Called at the end tag for simple content.</summary>
    /// <param name="info">The value.</param>
    public virtual void SimpleValue(in SimpleValueInfo info) { }

    /// <summary>Called after the validity of the content is known.</summary>
    /// <param name="info">The element.</param>
    public virtual void EndElement(in ElementEndInfo info) { }

    /// <summary>Called after the last event, for deferred checks.</summary>
    public virtual void EndRun() { }
}
