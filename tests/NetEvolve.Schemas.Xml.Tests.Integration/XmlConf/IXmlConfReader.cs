namespace NetEvolve.Schemas.Xml.Tests.Integration.XmlConf;

/// <summary>The seam between the runner and an XML reader; a fake fills it until XML-53 binds the real reader pipeline.</summary>
internal interface IXmlConfReader
{
    /// <summary>Reads the document of a case.</summary>
    /// <param name="xmlConfCase">The case.</param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The observation; <see cref="XmlConfObservation.NotImplemented"/> when nothing was read.</returns>
    Task<XmlConfObservation> ReadAsync(XmlConfCase xmlConfCase, CancellationToken cancellationToken);
}
