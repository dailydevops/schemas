namespace NetEvolve.Schemas.Xsd.Tests.Unit.Wildcards.Core;

using NetEvolve.Schemas.Xsd.Wildcards.Core;

public class NamespaceConstraintTests
{
    private const string Tns = "urn:t";

    internal static NamespaceConstraint Parse(string value, string targetNamespace = Tns)
    {
        var parsed = NamespaceConstraint.TryParse(value, targetNamespace, out var constraint);
        if (!parsed)
        {
            throw new InvalidOperationException("Not a namespace constraint: " + value);
        }

        return constraint!;
    }

    [Test]
    [Arguments("##any", "urn:t", true)]
    [Arguments("##any", "urn:o", true)]
    [Arguments("##any", "", true)]
    [Arguments("##other", "urn:t", false)]
    [Arguments("##other", "urn:o", true)]
    [Arguments("##other", "", false)]
    [Arguments("##local", "urn:t", false)]
    [Arguments("##local", "urn:o", false)]
    [Arguments("##local", "", true)]
    [Arguments("##targetNamespace", "urn:t", true)]
    [Arguments("##targetNamespace", "urn:o", false)]
    [Arguments("##targetNamespace", "", false)]
    [Arguments("urn:a urn:b", "urn:a", true)]
    [Arguments("urn:a urn:b", "urn:t", false)]
    [Arguments("urn:a urn:b", "", false)]
    [Arguments("urn:a ##local ##targetNamespace", "urn:a", true)]
    [Arguments("urn:a ##local ##targetNamespace", "urn:t", true)]
    [Arguments("urn:a ##local ##targetNamespace", "", true)]
    [Arguments("urn:a ##local ##targetNamespace", "urn:o", false)]
    [Arguments("", "urn:t", false)]
    public async Task Allows_FollowsTheConstraintForm(string constraint, string name, bool expected) =>
        await Assert.That(Parse(constraint).Allows(name)).IsEqualTo(expected);

    [Test]
    public async Task Allows_OtherWithoutTargetNamespace_ExcludesTheAbsentNamespaceToo()
    {
        var other = Parse("##other", "");

        await Assert.That(other.Allows("")).IsFalse();
        await Assert.That(other.Allows("urn:o")).IsTrue();
        await Assert.That(other.ToString()).IsEqualTo("not(##local)");
    }

    [Test]
    [Arguments("##any ##local")]
    [Arguments("##other urn:a")]
    [Arguments("urn:a ##any")]
    [Arguments("##foo")]
    [Arguments("##targetnamespace")]
    public async Task TryParse_RejectsInvalidValues(string value)
    {
        var parsed = NamespaceConstraint.TryParse(value, Tns, out var constraint);

        await Assert.That(parsed).IsFalse();
        await Assert.That(constraint).IsNull();
    }

    [Test]
    public async Task TryParse_NullValue_IsAny()
    {
        var parsed = NamespaceConstraint.TryParse(null, Tns, out var constraint);

        await Assert.That(parsed).IsTrue();
        await Assert.That(constraint!.ToString()).IsEqualTo("##any");
    }

    [Test]
    public async Task TryParse_SplitsOnXmlWhitespaceAndRemovesDuplicates()
    {
        var constraint = Parse(" urn:b\t##local\r\nurn:b  ##local ");

        await Assert.That(constraint.ToString()).IsEqualTo("{##local,urn:b}");
    }

    [Test]
    [Arguments("##any", "##any", "##any")]
    [Arguments("##any", "##other", "##any")]
    [Arguments("##any", "##local", "##any")]
    [Arguments("##any", "##targetNamespace", "##any")]
    [Arguments("##any", "urn:a urn:b", "##any")]
    [Arguments("##any", "urn:t ##local", "##any")]
    [Arguments("##other", "##other", "not(urn:t)")]
    [Arguments("##other", "##local", null)]
    [Arguments("##other", "##targetNamespace", "not(##local)")]
    [Arguments("##other", "urn:a urn:b", "not(urn:t)")]
    [Arguments("##other", "urn:t ##local", "##any")]
    [Arguments("##local", "##local", "{##local}")]
    [Arguments("##local", "##targetNamespace", "{##local,urn:t}")]
    [Arguments("##local", "urn:a urn:b", "{##local,urn:a,urn:b}")]
    [Arguments("##local", "urn:t ##local", "{##local,urn:t}")]
    [Arguments("##targetNamespace", "##targetNamespace", "{urn:t}")]
    [Arguments("##targetNamespace", "urn:a urn:b", "{urn:a,urn:b,urn:t}")]
    [Arguments("##targetNamespace", "urn:t ##local", "{##local,urn:t}")]
    [Arguments("urn:a urn:b", "urn:a urn:b", "{urn:a,urn:b}")]
    [Arguments("urn:a urn:b", "urn:t ##local", "{##local,urn:a,urn:b,urn:t}")]
    [Arguments("urn:t ##local", "urn:t ##local", "{##local,urn:t}")]
    public async Task Union_OfEveryPairOfForms_FollowsAttributeWildcardUnion(
        string left,
        string right,
        string? expected
    )
    {
        var l = Parse(left);
        var r = Parse(right);

        await Assert.That(l.Union(r)?.ToString()).IsEqualTo(expected);
        await Assert.That(r.Union(l)?.ToString()).IsEqualTo(expected);
    }

    [Test]
    public async Task Union_NegationsOfDifferentValues_IsNotAbsent()
    {
        var left = Parse("##other", "urn:t");
        var right = Parse("##other", "urn:o");

        await Assert.That(left.Union(right)!.ToString()).IsEqualTo("not(##local)");
    }

    [Test]
    [Arguments("urn:a", "not(##local)")]
    [Arguments("##local urn:a", "##any")]
    public async Task Union_NegatedAbsentWithASet_IsAnyOnlyWhenTheSetHoldsAbsent(string set, string expected)
    {
        var negatedAbsent = Parse("##other", "");

        await Assert.That(negatedAbsent.Union(Parse(set))!.ToString()).IsEqualTo(expected);
        await Assert.That(Parse(set).Union(negatedAbsent)!.ToString()).IsEqualTo(expected);
    }

    [Test]
    [Arguments("##any", "##any", "##any")]
    [Arguments("##any", "##other", "not(urn:t)")]
    [Arguments("##any", "##local", "{##local}")]
    [Arguments("##any", "##targetNamespace", "{urn:t}")]
    [Arguments("##any", "urn:a urn:b", "{urn:a,urn:b}")]
    [Arguments("##any", "urn:t ##local", "{##local,urn:t}")]
    [Arguments("##other", "##other", "not(urn:t)")]
    [Arguments("##other", "##local", "{}")]
    [Arguments("##other", "##targetNamespace", "{}")]
    [Arguments("##other", "urn:a urn:b", "{urn:a,urn:b}")]
    [Arguments("##other", "urn:t ##local", "{}")]
    [Arguments("##local", "##local", "{##local}")]
    [Arguments("##local", "##targetNamespace", "{}")]
    [Arguments("##local", "urn:a urn:b", "{}")]
    [Arguments("##local", "urn:t ##local", "{##local}")]
    [Arguments("##targetNamespace", "##targetNamespace", "{urn:t}")]
    [Arguments("##targetNamespace", "urn:a urn:b", "{}")]
    [Arguments("##targetNamespace", "urn:t ##local", "{urn:t}")]
    [Arguments("urn:a urn:b", "urn:a urn:b", "{urn:a,urn:b}")]
    [Arguments("urn:a urn:b", "urn:t ##local", "{}")]
    [Arguments("urn:t ##local", "urn:t ##local", "{##local,urn:t}")]
    public async Task Intersect_OfEveryPairOfForms_FollowsAttributeWildcardIntersection(
        string left,
        string right,
        string expected
    )
    {
        var l = Parse(left);
        var r = Parse(right);

        await Assert.That(l.Intersect(r)?.ToString()).IsEqualTo(expected);
        await Assert.That(r.Intersect(l)?.ToString()).IsEqualTo(expected);
    }

    [Test]
    public async Task Intersect_NegationsOfDifferentNamespaces_IsNotExpressible()
    {
        var left = Parse("##other", "urn:t");
        var right = Parse("##other", "urn:o");

        await Assert.That(left.Intersect(right)).IsNull();
        await Assert.That(right.Intersect(left)).IsNull();
    }

    [Test]
    public async Task Intersect_NegatedNamespaceWithNegatedAbsent_IsTheNegatedNamespace()
    {
        var named = Parse("##other", "urn:t");
        var absent = Parse("##other", "");

        await Assert.That(named.Intersect(absent)!.ToString()).IsEqualTo("not(urn:t)");
        await Assert.That(absent.Intersect(named)!.ToString()).IsEqualTo("not(urn:t)");
    }

    [Test]
    [Arguments("##any", "##any", true)]
    [Arguments("##any", "##other", false)]
    [Arguments("##any", "##local", false)]
    [Arguments("##any", "##targetNamespace", false)]
    [Arguments("##any", "urn:a urn:b", false)]
    [Arguments("##any", "urn:t ##local", false)]
    [Arguments("##other", "##any", true)]
    [Arguments("##other", "##other", true)]
    [Arguments("##other", "##local", false)]
    [Arguments("##other", "##targetNamespace", false)]
    [Arguments("##other", "urn:a urn:b", false)]
    [Arguments("##other", "urn:t ##local", false)]
    [Arguments("##local", "##any", true)]
    [Arguments("##local", "##other", false)]
    [Arguments("##local", "##local", true)]
    [Arguments("##local", "##targetNamespace", false)]
    [Arguments("##local", "urn:a urn:b", false)]
    [Arguments("##local", "urn:t ##local", true)]
    [Arguments("##targetNamespace", "##any", true)]
    [Arguments("##targetNamespace", "##other", false)]
    [Arguments("##targetNamespace", "##local", false)]
    [Arguments("##targetNamespace", "##targetNamespace", true)]
    [Arguments("##targetNamespace", "urn:a urn:b", false)]
    [Arguments("##targetNamespace", "urn:t ##local", true)]
    [Arguments("urn:a urn:b", "##any", true)]
    [Arguments("urn:a urn:b", "##other", true)]
    [Arguments("urn:a urn:b", "##local", false)]
    [Arguments("urn:a urn:b", "##targetNamespace", false)]
    [Arguments("urn:a urn:b", "urn:a urn:b", true)]
    [Arguments("urn:a urn:b", "urn:t ##local", false)]
    [Arguments("urn:t ##local", "##any", true)]
    [Arguments("urn:t ##local", "##other", false)]
    [Arguments("urn:t ##local", "##local", false)]
    [Arguments("urn:t ##local", "##targetNamespace", false)]
    [Arguments("urn:t ##local", "urn:a urn:b", false)]
    [Arguments("urn:t ##local", "urn:t ##local", true)]
    [Arguments("", "##other", true)]
    [Arguments("", "##local", true)]
    public async Task IsSubsetOf_OfEveryPairOfForms_FollowsWildcardSubset(string sub, string super, bool expected) =>
        await Assert.That(Parse(sub).IsSubsetOf(Parse(super))).IsEqualTo(expected);

    [Test]
    public async Task IsSubsetOf_NegationsAreSubsetsOnlyOfTheSameValue()
    {
        var named = Parse("##other", "urn:t");
        var absent = Parse("##other", "");

        // Wildcard Subset is intensional: not(urn:t) is not a subset of not(absent) although it allows fewer names.
        await Assert.That(named.IsSubsetOf(absent)).IsFalse();
        await Assert.That(absent.IsSubsetOf(named)).IsFalse();
        await Assert.That(absent.IsSubsetOf(Parse("##other", ""))).IsTrue();
    }
}
