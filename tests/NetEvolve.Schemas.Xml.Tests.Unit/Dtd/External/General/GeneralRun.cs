namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.General;

using NetEvolve.Schemas.Xml.Contracts;

/// <summary>The outcome of one read: status, the alias of the error code, its position, the event dump, and the sampled base URIs.</summary>
internal sealed record GeneralRun(
    XmlReadStatus Status,
    string? Code,
    XmlPosition? ErrorPosition,
    string Events,
    string Bases,
    string? BaseAfterwards
);
