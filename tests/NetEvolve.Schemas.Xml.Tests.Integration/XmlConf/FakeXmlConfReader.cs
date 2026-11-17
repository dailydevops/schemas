namespace NetEvolve.Schemas.Xml.Tests.Integration.XmlConf;

internal sealed class FakeXmlConfReader(Func<XmlConfCase, XmlConfObservation> verdict) : IXmlConfReader
{
    public List<string> Calls { get; } = [];

    public Task<XmlConfObservation> ReadAsync(XmlConfCase xmlConfCase, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls.Add(xmlConfCase.Id);
        return Task.FromResult(verdict(xmlConfCase));
    }
}
