namespace NetEvolve.Schemas.Xml.Tests.Unit.Reader.Core;

using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Reader.Core;

/// <summary>A doctype handler that records the arguments of every call and returns a configured result.</summary>
internal sealed class FakeDoctypeHandler : IXmlDoctypeHandler
{
    internal FakeDoctypeHandler(XmlErrorCode? result = null) => Result = result;

    internal XmlErrorCode? Result { get; }

    internal List<(string Name, string PublicId, string SystemId, string Subset)> Calls { get; } = [];

    public XmlErrorCode? Handle(in XmlDoctypeInfo doctype)
    {
        Calls.Add(
            (
                doctype.Name.ToString(),
                doctype.PublicId.ToString(),
                doctype.SystemId.ToString(),
                doctype.InternalSubset.ToString()
            )
        );
        return Result;
    }
}
