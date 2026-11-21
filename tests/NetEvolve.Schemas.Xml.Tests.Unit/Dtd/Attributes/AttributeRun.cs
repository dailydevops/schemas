namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Attributes;

using NetEvolve.Schemas.Xml.Contracts;

/// <summary>The outcome of one read: status, the alias of the error code, its position, the event dump, and the sink.</summary>
internal sealed record AttributeRun(
    XmlReadStatus Status,
    string? Code,
    XmlPosition? ErrorPosition,
    string Events,
    InfoRecordingSink Sink
);
