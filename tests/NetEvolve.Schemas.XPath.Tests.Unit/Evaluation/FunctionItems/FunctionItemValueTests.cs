namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.FunctionItems;

using System.Threading;
using System.Threading.Tasks;
using static FunctionItemFixture;

/// <summary>A function item has no typed value and no effective boolean value (XPath 3.0, 2.4.2 and 2.4.3; Data Model 3.0, function items).</summary>
public class FunctionItemValueTests
{
    [Test]
    [Arguments("data(abs#1)")]
    [Arguments("data(function() { 1 })")]
    [Arguments("abs#1 + 1")]
    [Arguments("1 + abs#1")]
    [Arguments("abs#1 = 1")]
    [Arguments("abs#1 eq 1")]
    [Arguments("-abs#1")]
    [Arguments("(1, abs#1) = 1")]
    [Arguments("(function($a as xs:integer) { $a })(abs#1)")]
    [Arguments("concat(abs#1, 'a')")]
    public async Task Atomization_OfAFunctionItem_IsFOTY0013(string expression, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Code(expression)).IsEqualTo("FOTY0013");
    }

    [Test]
    [Arguments("boolean(abs#1)")]
    [Arguments("not(abs#1)")]
    [Arguments("if (abs#1) then 1 else 2")]
    [Arguments("abs#1 and true()")]
    [Arguments("true() and abs#1")]
    [Arguments("some $f in abs#1 satisfies $f")]
    [Arguments("(1, 2)[abs#1]")]
    [Arguments("boolean((abs#1, abs#1))")]
    public async Task TheEffectiveBooleanValue_OfAFunctionItem_IsFORG0006(
        string expression,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Code(expression)).IsEqualTo("FORG0006");
    }

    [Test]
    public async Task AFunctionItem_PassesThroughSequencesUnchanged(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run("count((abs#1, 1, function() { 2 }))")).IsEqualTo("integer:3");
        await Assert.That(Run("(abs#1, 2)[2]")).IsEqualTo("integer:2");
        await Assert.That(Run("(abs#1, 2)[1] instance of function(*)")).IsEqualTo("boolean:True");
    }
}
