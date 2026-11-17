namespace NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;

using System;
using System.Collections.Generic;
using System.Threading;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

internal sealed class FakeQueryExpression(string text) : QueryExpression
{
    internal string Text { get; } = text;
}
