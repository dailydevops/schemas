namespace NetEvolve.Schemas.Xml.Contracts;

/// <summary>
/// A resumable push reader. <see cref="Pump{TSink}"/> drives a sink until the document ends, the input needs more data, or an
/// error is found. The core never throws for input problems and has no async pump; span entry points are static helpers of XML-53.
/// </summary>
/// <remarks>Limitation: a pull facade over this contract is the on-hold work item #400.</remarks>
public interface IXmlReader
{
    /// <summary>Gets the position after the last event that was pumped.</summary>
    XmlPosition Position { get; }

    /// <summary>Gets the error, <see langword="null"/> unless <see cref="Pump{TSink}"/> returned <see cref="XmlReadStatus.Error"/>.</summary>
    XmlDiagnostic? Diagnostic { get; }

    /// <summary>Pushes events into the sink until the document ends, more data is needed, or an error is found.</summary>
    /// <typeparam name="TSink">The sink type, a class or a mutable struct.</typeparam>
    /// <param name="sink">The sink; passed by reference so a mutable struct sink keeps its state.</param>
    /// <returns>The status of the pump.</returns>
    XmlReadStatus Pump<TSink>(ref TSink sink)
        where TSink : IXmlSink;
}
