namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions;

using NetEvolve.Schemas.Schematron.Evaluation.Assertions;

/// <summary>The position and the location path of a reported node (design #137 AP-14).</summary>
public class NodeLocationTests
{
    private const string Root = "/*[local-name()='root' and namespace-uri()=''][1]";
    private const string Order1 = Root + "/*[local-name()='order' and namespace-uri()=''][1]";

    [Test]
    [Arguments("root", 1, 1)]
    [Arguments("order1", 2, 3)]
    [Arguments("item2", 4, 5)]
    [Arguments("px", 6, 3)]
    [Arguments("id", 2, 3)]
    [Arguments("txt", 2, 3)]
    [Arguments("c", 2, 3)]
    [Arguments("pi", 2, 3)]
    [Arguments("doc", 0, 0)]
    public async Task Of_IsTheElementPosition_ParentPositionForOtherNodes_AndZeroForTheDocument(
        string key,
        int line,
        int column
    )
    {
        var node = AssertionTree.Node(key);

        var location = NodeLocation.Of(in node, "file:///a.xml");

        await Assert.That(location.Source).IsEqualTo("file:///a.xml");
        await Assert.That(location.Line).IsEqualTo(line);
        await Assert.That(location.Column).IsEqualTo(column);
    }

    [Test]
    public async Task Of_WithoutASource_KeepsTheSourceNull()
    {
        var node = AssertionTree.Node("root");

        var location = NodeLocation.Of(in node, null);

        await Assert.That(location.Source).IsNull();
    }

    [Test]
    [Arguments("doc", "/")]
    [Arguments("root", Root)]
    [Arguments("order1", Order1)]
    [Arguments("order2", Root + "/*[local-name()='order' and namespace-uri()=''][2]")]
    [Arguments("item2", Order1 + "/*[local-name()='item' and namespace-uri()=''][2]")]
    [Arguments("px", Root + "/*[local-name()='x' and namespace-uri()='urn:p'][1]")]
    [Arguments("id", Order1 + "/@*[local-name()='id' and namespace-uri()='']")]
    [Arguments("txt", Order1 + "/text()[1]")]
    [Arguments("c", Order1 + "/comment()[1]")]
    [Arguments("pi", Order1 + "/processing-instruction('pi')[1]")]
    public async Task AppendPath_BuildsTheLocationPathOfTheNode(string key, string expected)
    {
        var node = AssertionTree.Node(key);
        using var sink = new TextSink(1000);

        NodeLocation.AppendPath(in node, sink);

        await Assert.That(sink.ToString()).IsEqualTo(expected);
    }

    [Test]
    public async Task AppendPath_NullSink_Throws()
    {
        var node = AssertionTree.Node("root");

        await Assert.That(() => NodeLocation.AppendPath(in node, null!)).Throws<ArgumentNullException>();
    }
}
