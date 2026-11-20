namespace NetEvolve.Schemas.Xsd.Tests.Unit.IdentityConstraints.Evaluation;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.IdentityConstraints.Evaluation;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;
using NetEvolve.Schemas.Xsd.Validation.Core;

public class KeyTableTests
{
    private static readonly IXsdComponent Decimal = IdcFixture.BuiltIn("decimal");

    private static readonly ValueSpace Space = new(new DescriptorValueAssessor(XsdVersion.V11), XsdVersion.V11);

    private static KeySequence Sequence(params string[] values)
    {
        var types = new IXsdComponent[values.Length];
        var hash = 17;
        for (var i = 0; i < values.Length; i++)
        {
            types[i] = Decimal;
            hash = unchecked((hash * 31) + Space.Hash(Decimal, values[i]));
        }

        return new KeySequence(values, types, hash);
    }

    [Test]
    public async Task Contains_AfterAdd_FindsTheEqualSequenceInTheValueSpace()
    {
        var table = new KeyTable();
        table.Add(Sequence("1", "2"));

        await Assert.That(table.Count).IsEqualTo(1);
        await Assert.That(table.Contains(Sequence("1.0", "2.00"), Space)).IsTrue();
        await Assert.That(table.Contains(Sequence("1", "3"), Space)).IsFalse();
        await Assert.That(table.Contains(Sequence("9"), Space)).IsFalse();
    }

    [Test]
    public async Task Contains_SequenceOfAnotherLengthWithTheSameHash_IsNotEqual()
    {
        var table = new KeyTable();
        table.Add(new KeySequence(["1", "2"], [Decimal, Decimal], 5));

        await Assert.That(table.Contains(new KeySequence(["1"], [Decimal], 5), Space)).IsFalse();
    }

    [Test]
    public async Task Add_SequencesWithTheSameHash_AreKeptApart()
    {
        var table = new KeyTable();
        table.Add(new KeySequence(["1"], [Decimal], 5));
        table.Add(new KeySequence(["2"], [Decimal], 5));

        await Assert.That(table.Count).IsEqualTo(2);
        await Assert.That(table.Contains(new KeySequence(["2"], [Decimal], 5), Space)).IsTrue();
        await Assert.That(table.Contains(new KeySequence(["3"], [Decimal], 5), Space)).IsFalse();
    }

    [Test]
    public async Task Guards_NullArguments_Throw()
    {
        var table = new KeyTable();

        await Assert.That(() => table.Contains(null!, Space)).Throws<ArgumentNullException>();
        await Assert.That(() => table.Contains(Sequence("1"), null!)).Throws<ArgumentNullException>();
        await Assert.That(() => table.Add(null!)).Throws<ArgumentNullException>();
    }
}
