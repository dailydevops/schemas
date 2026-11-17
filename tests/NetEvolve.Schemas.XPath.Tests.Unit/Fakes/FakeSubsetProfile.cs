namespace NetEvolve.Schemas.XPath.Tests.Unit.Fakes;

using NetEvolve.Schemas.XPath.Abstractions;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>A fake subset profile: records the tree it receives and rejects it when asked to.</summary>
internal sealed class FakeSubsetProfile(bool reject) : IXPathSubsetProfile
{
    public Expression? Received { get; private set; }

    public bool Accepts(Expression tree, out XPathDiagnostic violation)
    {
        Received = tree;
        violation = reject ? XPathDiagnostic.Create(XPathErrorCodes.XPST0003, 0, 1) : default;
        return !reject;
    }
}
