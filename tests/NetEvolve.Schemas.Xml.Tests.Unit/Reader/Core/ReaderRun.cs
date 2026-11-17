namespace NetEvolve.Schemas.Xml.Tests.Unit.Reader.Core;

using NetEvolve.Schemas.Xml.Contracts;

/// <summary>The outcome of one parse: the event dump, the element positions, the status, and the diagnostic.</summary>
internal sealed record ReaderRun(
    string Events,
    string Positions,
    XmlReadStatus Status,
    XmlErrorCode? Code,
    XmlPosition? ErrorPosition,
    XmlPosition Position
);
