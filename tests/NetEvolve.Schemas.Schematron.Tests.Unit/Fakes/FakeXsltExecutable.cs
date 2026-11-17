namespace NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;

internal sealed class FakeXsltExecutable : XsltExecutable
{
    internal int Executions { get; private set; }

    internal override QueryStatus TryExecute(XsltFrame frame, ISequenceSink sink)
    {
        Executions++;
        sink.Text("done".AsSpan());
        return QueryStatus.Ok;
    }
}
