#pragma warning disable CA1065, MA0025 // Stub per #155: throwing bodies stay until the implementation commit of XML-08 replaces them.

namespace NetEvolve.Schemas.Xml.Limits;

using NetEvolve.Schemas.Xml.Contracts;

/// <summary>One budget for one resource that hostile input can inflate; the reader and the entity stack charge it.</summary>
internal struct XmlBudget
{
    internal XmlBudget(long limit, XmlErrorCode code) => throw Stub.For("XML-08");

    internal readonly long Limit => throw Stub.For("XML-08");

    internal readonly long Used => throw Stub.For("XML-08");

    internal readonly XmlErrorCode Code => throw Stub.For("XML-08");

    internal bool TryCharge(long amount) => throw Stub.For("XML-08");

    internal void Release(long amount) => throw Stub.For("XML-08");

    internal void Reset() => throw Stub.For("XML-08");

    internal static XmlBudget ElementDepth(XmlReaderLimits limits) => throw Stub.For("XML-08");

    internal static XmlBudget NameLength(XmlReaderLimits limits) => throw Stub.For("XML-08");

    internal static XmlBudget AttributeCount(XmlReaderLimits limits) => throw Stub.For("XML-08");

    internal static XmlBudget TextLength(XmlReaderLimits limits) => throw Stub.For("XML-08");

    internal static XmlBudget DocumentCharacters(XmlReaderLimits limits) => throw Stub.For("XML-08");

    internal static XmlBudget ExpansionCharacters(XmlReaderLimits limits) => throw Stub.For("XML-08");

    internal static XmlBudget EntityDepth(XmlReaderLimits limits) => throw Stub.For("XML-08");
}
