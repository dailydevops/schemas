namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Sequences;

using System.Threading.Tasks;

/// <summary>
/// <c>fn:sum</c>, <c>fn:avg</c>, <c>fn:min</c>, and <c>fn:max</c> (Functions and Operators 1.0, 15.4): numeric promotion across the
/// sequence, <c>xs:untypedAtomic</c> cast to <c>xs:double</c>, NaN, the zero value of <c>fn:sum</c>, and FORG0006 for values that
/// cannot be added or ordered together.
/// </summary>
public class AggregateTests
{
    [Test]
    [Arguments("()", "integer:0")]
    [Arguments("integer:5", "integer:5")]
    [Arguments("integer:1|integer:2|integer:3", "integer:6")]
    [Arguments("integer:1|decimal:2.5", "decimal:3.5")]
    [Arguments("integer:1|double:2.5", "double:3.5")]
    [Arguments("decimal:0.1|decimal:0.2", "decimal:0.3")]
    [Arguments("float:1.5|float:2.5", "float:4")]
    [Arguments("float:1.5|double:2.5", "double:4")]
    [Arguments("float:1.5|integer:2", "float:3.5")]
    [Arguments("double:NaN|integer:1", "double:NaN")]
    [Arguments("integer:1|float:NaN", "float:NaN")]
    [Arguments("double:INF|double:-INF", "double:NaN")]
    [Arguments("double:INF|integer:1", "double:INF")]
    [Arguments("untypedAtomic:1|untypedAtomic:2", "double:3")]
    [Arguments("untypedAtomic:1.5|integer:1", "double:2.5")]
    [Arguments("untypedAtomic:abc", "error:FORG0001")]
    [Arguments("integer:9223372036854775807|integer:1", "decimal:9223372036854775808")]
    [Arguments("string:a", "error:FORG0006")]
    [Arguments("integer:1|string:1", "error:FORG0006")]
    [Arguments("boolean:true", "error:FORG0006")]
    [Arguments("anyURI:u", "error:FORG0006")]
    public async Task Sum_AddsWithNumericPromotion(string argument, string expected) =>
        await Assert.That(SequencesHarness.Run("sum", argument)).IsEqualTo(expected);

    [Test]
    [Arguments("()", "integer:7", "integer:7")]
    [Arguments("()", "double:0", "double:0")]
    [Arguments("()", "string:none", "string:none")]
    [Arguments("()", "()", "")]
    [Arguments("integer:1|integer:2", "string:ignored", "integer:3")]
    [Arguments("integer:1|integer:2", "()", "integer:3")]
    [Arguments("string:a", "integer:0", "error:FORG0006")]
    public async Task Sum_WithAZeroValue_ReturnsItOnlyForTheEmptySequence(
        string argument,
        string zero,
        string expected
    ) => await Assert.That(SequencesHarness.Run("sum", argument, zero)).IsEqualTo(expected);

    [Test]
    [Arguments("date:2000-01-01", "error:FORG0006")]
    [Arguments("duration:PT1H", "error:FORG0006")]
    public async Task Sum_OverDatesAndDurations_IsALimitationAndRaisesFORG0006(string argument, string expected) =>
        await Assert.That(SequencesHarness.Run("sum", argument)).IsEqualTo(expected);

    [Test]
    [Arguments("()", "")]
    [Arguments("integer:3|integer:4|integer:5", "decimal:4")]
    [Arguments("integer:1|integer:2", "decimal:1.5")]
    [Arguments("decimal:1|decimal:2", "decimal:1.5")]
    [Arguments("double:1|double:2", "double:1.5")]
    [Arguments("float:1|float:2", "float:1.5")]
    [Arguments("integer:1|double:2", "double:1.5")]
    [Arguments("untypedAtomic:2|untypedAtomic:4", "double:3")]
    [Arguments("double:NaN|integer:1", "double:NaN")]
    [Arguments("integer:7", "decimal:7")]
    [Arguments("string:a", "error:FORG0006")]
    [Arguments("integer:1|boolean:true", "error:FORG0006")]
    [Arguments("untypedAtomic:x", "error:FORG0001")]
    [Arguments("date:2000-01-01", "error:FORG0006")]
    public async Task Avg_IsTheSumDividedByTheCount(string argument, string expected) =>
        await Assert.That(SequencesHarness.Run("avg", argument)).IsEqualTo(expected);

    [Test]
    [Arguments("()", "")]
    [Arguments("integer:3|integer:1|integer:2", "integer:1")]
    [Arguments("integer:1|double:2.5", "double:1")]
    [Arguments("integer:1|decimal:2.5", "decimal:1")]
    [Arguments("float:3|integer:2", "float:2")]
    [Arguments("double:NaN|integer:1", "double:NaN")]
    [Arguments("integer:1|double:NaN|integer:2", "double:NaN")]
    [Arguments("float:NaN|integer:1", "float:NaN")]
    [Arguments("float:NaN|double:1", "double:NaN")]
    [Arguments("double:INF|double:-INF", "double:-INF")]
    [Arguments("untypedAtomic:5|integer:3", "double:3")]
    [Arguments("untypedAtomic:x", "error:FORG0001")]
    [Arguments("string:b|string:a|string:c", "string:a")]
    [Arguments("anyURI:b|string:a", "string:a")]
    [Arguments("anyURI:b|anyURI:a", "string:a")]
    [Arguments("string:�|string:\U0001F600", "string:�")]
    [Arguments("boolean:true|boolean:false", "boolean:false")]
    [Arguments("string:a|integer:1", "error:FORG0006")]
    [Arguments("boolean:true|integer:1", "error:FORG0006")]
    [Arguments("date:2000-01-01|date:2001-01-01", "error:FORG0006")]
    [Arguments("duration:PT1H|duration:PT2H", "error:FORG0006")]
    public async Task Min_IsTheLeastValueInThePromotedType(string argument, string expected) =>
        await Assert.That(SequencesHarness.Run("min", argument)).IsEqualTo(expected);

    [Test]
    [Arguments("()", "")]
    [Arguments("integer:3|integer:1|integer:2", "integer:3")]
    [Arguments("integer:1|double:2.5", "double:2.5")]
    [Arguments("integer:3|decimal:2.5", "decimal:3")]
    [Arguments("float:1|integer:2", "float:2")]
    [Arguments("double:NaN|integer:1", "double:NaN")]
    [Arguments("integer:1|double:NaN|integer:2", "double:NaN")]
    [Arguments("float:NaN|integer:1", "float:NaN")]
    [Arguments("double:INF|double:-INF", "double:INF")]
    [Arguments("untypedAtomic:5|integer:3", "double:5")]
    [Arguments("untypedAtomic:x", "error:FORG0001")]
    [Arguments("string:b|string:a|string:c", "string:c")]
    [Arguments("anyURI:b|string:a", "string:b")]
    [Arguments("string:�|string:\U0001F600", "string:\U0001F600")]
    [Arguments("boolean:false|boolean:true", "boolean:true")]
    [Arguments("string:a|integer:1", "error:FORG0006")]
    [Arguments("date:2000-01-01|date:2001-01-01", "error:FORG0006")]
    public async Task Max_IsTheGreatestValueInThePromotedType(string argument, string expected) =>
        await Assert.That(SequencesHarness.Run("max", argument)).IsEqualTo(expected);
}
