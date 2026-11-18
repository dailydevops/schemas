namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Sequences;

using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:distinct-values</c> and <c>fn:index-of</c> (Functions and Operators 1.0, 15.1.6 and 15.1.3): values are equal by <c>eq</c>
/// after numeric promotion, <c>xs:untypedAtomic</c> compares as <c>xs:string</c>, values of types that cannot be compared are
/// different and raise nothing, and NaN is equal to NaN in <c>fn:distinct-values</c> only.
/// </summary>
public class ValueSetTests
{
    [Test]
    [Arguments("()", "")]
    [Arguments("integer:1|integer:2|integer:1|integer:3|integer:2", "integer:1,integer:2,integer:3")]
    [Arguments("string:b|string:a|string:b", "string:b,string:a")]
    [Arguments("integer:1|decimal:1.0|double:1|float:1", "integer:1")]
    [Arguments("double:1|integer:1", "double:1")]
    [Arguments("integer:1|decimal:1.5|double:1.5", "integer:1,decimal:1.5")]
    [Arguments("double:NaN|double:NaN|float:NaN", "double:NaN")]
    [Arguments("double:NaN|integer:1|double:NaN", "double:NaN,integer:1")]
    [Arguments("double:0|double:-0", "double:0")]
    [Arguments("double:INF|double:INF|double:-INF", "double:INF,double:-INF")]
    [Arguments("string:a|untypedAtomic:a", "string:a")]
    [Arguments("untypedAtomic:a|untypedAtomic:a|untypedAtomic:b", "untypedAtomic:a,untypedAtomic:b")]
    [Arguments("anyURI:u|string:u", "anyURI:u")]
    [Arguments("string:1|integer:1", "string:1,integer:1")]
    [Arguments("boolean:true|boolean:true|boolean:false|integer:1", "boolean:true,boolean:false,integer:1")]
    [Arguments("date:2000-01-01|date:2000-01-01|date:2001-01-01", "date:2000-01-01,date:2001-01-01")]
    [Arguments("string:|string:", "string:")]
    public async Task DistinctValues_KeepsTheFirstOfEachGroupOfEqualValues(string argument, string expected) =>
        await Assert.That(SequencesHarness.Run("distinct-values", argument)).IsEqualTo(expected);

    [Test]
    public async Task DistinctValues_OverManyItems_KeepsEachValueOnceInInputOrder()
    {
        var items = new XdmItem[5000];
        for (var i = 0; i < items.Length; i++)
        {
            items[i] = XdmItem.FromInteger(i % 1000);
        }

        var result = SequencesHarness.CallWith("distinct-values", XdmSequence.Create(items));

        await Assert.That(result.Count).IsEqualTo(1000);
        await Assert.That(result[0]).IsEqualTo(XdmItem.FromInteger(0));
        await Assert.That(result[999]).IsEqualTo(XdmItem.FromInteger(999));
    }

    [Test]
    [Arguments("integer:10|integer:20|integer:30|integer:20", "integer:20", "integer:2,integer:4")]
    [Arguments("()", "integer:1", "")]
    [Arguments("integer:1|integer:2", "integer:3", "")]
    [Arguments("integer:1|decimal:1.0|double:1|float:1", "integer:1", "integer:1,integer:2,integer:3,integer:4")]
    [Arguments("integer:1|decimal:2.5", "double:2.5", "integer:2")]
    [Arguments("double:NaN|double:NaN", "double:NaN", "")]
    [Arguments("string:a|integer:1|string:a", "string:a", "integer:1,integer:3")]
    [Arguments("string:a|integer:1|string:a", "integer:1", "integer:2")]
    [Arguments("untypedAtomic:a|string:a|anyURI:a", "string:a", "integer:1,integer:2,integer:3")]
    [Arguments("untypedAtomic:1|integer:1", "string:1", "integer:1")]
    [Arguments("boolean:true|boolean:false|boolean:true", "boolean:true", "integer:1,integer:3")]
    [Arguments("boolean:true|integer:1", "integer:1", "integer:2")]
    [Arguments("date:2000-01-01|string:2000-01-01", "date:2000-01-01", "integer:1")]
    public async Task IndexOf_ListsThePositionsOfTheEqualValues(string sequence, string search, string expected) =>
        await Assert.That(SequencesHarness.Run("index-of", sequence, search)).IsEqualTo(expected);

    [Test]
    [Arguments("integer:1|integer:2", "integer:1|integer:2")]
    [Arguments("integer:1", "()")]
    public async Task IndexOf_SearchValueThatIsNotOneItem_RaisesXPTY0004(string sequence, string search) =>
        await Assert.That(SequencesHarness.Run("index-of", sequence, search)).IsEqualTo("error:XPTY0004");

    [Test]
    [Arguments("distinct-values", "integer:1|integer:1")]
    [Arguments("min", "integer:2|integer:1")]
    [Arguments("max", "integer:2|integer:1")]
    public async Task CollationArgument_TheCodepointCollation_IsAccepted(string function, string argument)
    {
        var result = SequencesHarness.Run(function, argument, "string:" + SequencesHarness.Codepoint);

        await Assert.That(result).DoesNotStartWith("error:");
    }

    [Test]
    [Arguments("index-of", "integer:1|integer:2", "integer:1")]
    [Arguments("deep-equal", "integer:1", "integer:1")]
    public async Task CollationArgument_TheCodepointCollation_IsAcceptedInTheThreeArgumentForms(
        string function,
        string first,
        string second
    )
    {
        var result = SequencesHarness.Run(function, first, second, "string:" + SequencesHarness.Codepoint);

        await Assert.That(result).DoesNotStartWith("error:");
    }

    [Test]
    [Arguments("distinct-values")]
    [Arguments("min")]
    [Arguments("max")]
    public async Task CollationArgument_AnyOtherCollation_RaisesFOCH0002(string function) =>
        await Assert
            .That(SequencesHarness.Run(function, "integer:1", "string:http://example.com/collation"))
            .IsEqualTo("error:FOCH0002");

    [Test]
    [Arguments("index-of")]
    [Arguments("deep-equal")]
    public async Task CollationArgument_AnyOtherCollation_RaisesFOCH0002InTheThreeArgumentForms(string function) =>
        await Assert
            .That(SequencesHarness.Run(function, "integer:1", "integer:1", "string:http://example.com/collation"))
            .IsEqualTo("error:FOCH0002");
}
