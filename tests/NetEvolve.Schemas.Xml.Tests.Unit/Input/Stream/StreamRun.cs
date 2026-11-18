namespace NetEvolve.Schemas.Xml.Tests.Unit.Input.Stream;

using NetEvolve.Schemas.Xml.Contracts;

/// <summary>The outcome of one run: status, events with consecutive text merged, and the final state.</summary>
internal sealed record StreamRun(
    XmlReadStatus Status,
    string Events,
    XmlPosition Position,
    XmlErrorCode? Code,
    XmlPosition? ErrorPosition
);
