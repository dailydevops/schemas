namespace NetEvolve.Schemas.XPath.Tests.Unit.Compilation.Optimization;

using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Compilation.Optimization;
using NetEvolve.Schemas.XPath.Evaluation.Axes;
using static OptimizationFixture;

/// <summary>A name test is bound to the name table of a tree and compares interned handles, never strings (XPath 2.0, 3.2.1.1).</summary>
public class InternedNameTestTests
{
    [Test]
    public async Task Matcher_HoldsInternedHandlesAndNoString()
    {
        var fields = typeof(XPathNodeMatcher).GetFields(BindingFlags.Instance | BindingFlags.NonPublic);

        await Assert.That(fields.Any(static f => f.FieldType == typeof(string))).IsFalse();
        await Assert.That(fields.Count(static f => f.FieldType == typeof(XmlSymbol))).IsEqualTo(2);
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task NameTest_OverTreesThatInternTheNameUnderAnotherHandle_FindsTheSameNodes(bool internOtherFirst)
    {
        var tree = new ChildrenTree(3, internOtherFirst);

        await Assert.That(Run("//x", XPathOptimizations.All, tree)).IsEqualTo("#2 #3 #4");
        await Assert.That(Run("/r/x", XPathOptimizations.All, tree)).IsEqualTo("#2 #3 #4");
        await Assert.That(Run("//y", XPathOptimizations.All, tree)).IsEqualTo(string.Empty);
    }
}
