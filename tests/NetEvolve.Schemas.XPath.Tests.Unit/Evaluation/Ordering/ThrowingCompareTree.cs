namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Ordering;

using System;
using System.Buffers;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Text.NameTable;
using NetEvolve.Schemas.XPath.Evaluation.Ordering;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>A tree whose comparison fails.</summary>
internal sealed class ThrowingCompareTree(int elements) : CountingTree(elements)
{
    protected override int OnCompare(int first, int second) =>
        throw new InvalidOperationException("The comparison failed.");
}
