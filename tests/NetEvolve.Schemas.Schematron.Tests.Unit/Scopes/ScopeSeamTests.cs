namespace NetEvolve.Schemas.Schematron.Tests.Unit.Scopes;

using System;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Scopes;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>The additive seam members of design #150 EP-5 that the scope package needs: the binder, the slots an expression reads, and the declared type.</summary>
public class ScopeSeamTests
{
    private sealed class BareExpression : QueryExpression { }

    [Test]
    public async Task SlotBinder_ImplementsTheInternalSeam()
    {
        var seam = typeof(ISlotBinder);

        await Assert.That(seam.IsInterface).IsTrue();
        await Assert.That(seam.IsPublic).IsFalse();
        await Assert.That(seam.Namespace).IsEqualTo("NetEvolve.Schemas.Schematron.Seams");
        await Assert.That(typeof(ISlotBinder).IsAssignableFrom(typeof(SlotBinder))).IsTrue();
    }

    [Test]
    public async Task SlotBinderSeam_EnsureBound_TakesASpanOfSlotsAndReturnsTheStatus()
    {
        var method = typeof(ISlotBinder).GetMethod(nameof(ISlotBinder.EnsureBound))!;

        await Assert.That(method.ReturnType).IsEqualTo(typeof(QueryStatus));
        await Assert.That(method.GetParameters().Length).IsEqualTo(1);
        await Assert.That(method.GetParameters()[0].ParameterType).IsEqualTo(typeof(ReadOnlySpan<int>));
    }

    [Test]
    public async Task QueryExpression_WithoutOverride_ReadsNoSlots()
    {
        var expression = new BareExpression();

        await Assert.That(expression.ReferencedSlots.Length).IsEqualTo(0);
    }

    [Test]
    public async Task QueryStaticScope_DeclaredType_DefaultsToNull_AndRoundTrips()
    {
        var plain = new QueryStaticScope([], [], null, SchematronEdition.Iso2016);
        var typed = new QueryStaticScope([], [], null, SchematronEdition.Iso2016, "xs:integer");

        await Assert.That(plain.DeclaredType).IsNull();
        await Assert.That(typed.DeclaredType).IsEqualTo("xs:integer");
    }

    [Test]
    public async Task ScopeTypes_AreInternal_InTheScopesNamespace()
    {
        foreach (
            var type in new[]
            {
                typeof(ScopePlan),
                typeof(PatternScope),
                typeof(RuleScope),
                typeof(VariableSlot),
                typeof(SlotBinder),
                typeof(ScopePlanner),
                typeof(ConstantValue),
            }
        )
        {
            await Assert.That(type.IsPublic).IsFalse();
            await Assert.That(type.Namespace).IsEqualTo("NetEvolve.Schemas.Schematron.Scopes");
        }
    }
}
