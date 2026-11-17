namespace NetEvolve.Schemas.Xml.Tests.Integration.XmlConf;

/// <summary>The reader of this package: none is bound, so every case is a known failure.</summary>
internal sealed class UnboundXmlConfReader : IXmlConfReader
{
    public static UnboundXmlConfReader Instance { get; } = new();

    public Task<XmlConfObservation> ReadAsync(XmlConfCase xmlConfCase, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(XmlConfObservation.NotImplemented);
    }
}
