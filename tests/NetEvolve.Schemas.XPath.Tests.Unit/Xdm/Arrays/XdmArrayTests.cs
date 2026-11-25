namespace NetEvolve.Schemas.XPath.Tests.Unit.Xdm.Arrays;

using System;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Arrays;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The array item of the data model: immutable members, updates by copy, and slices of the source.</summary>
public class XdmArrayTests
{
    private static XdmSequence Member(long value) => XdmSequence.Create(XdmItem.FromInteger(value));

    private static XdmArray Numbers(params long[] values)
    {
        var members = new XdmSequence[values.Length];
        for (var i = 0; i < members.Length; i++)
        {
            members[i] = Member(values[i]);
        }

        return XdmArray.Create(members);
    }

    private static long At(XdmArray array, int index) => array[index][0].GetSaturatedInteger();

    [Test]
    public async Task Create_WithoutMembers_IsTheEmptyArray(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(XdmArray.Create([])).IsSameReferenceAs(XdmArray.Empty);
        await Assert.That(XdmArray.Empty.Size).IsEqualTo(0);
    }

    [Test]
    public async Task Slice_ReadsTheRange_AndLeavesTheSourceUnchanged(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var source = Numbers(1, 2, 3, 4);

        var slice = source.Slice(1, 2);

        await Assert.That(slice.Size).IsEqualTo(2);
        await Assert.That(At(slice, 0)).IsEqualTo(2);
        await Assert.That(At(slice, 1)).IsEqualTo(3);
        await Assert.That(source.Slice(0, 4)).IsSameReferenceAs(source);
        await Assert.That(source.Slice(2, 0)).IsSameReferenceAs(XdmArray.Empty);
        await Assert.That(source.Size).IsEqualTo(4);
    }

    [Test]
    public async Task Put_AndAppend_OnASlice_CopyTheRange(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var source = Numbers(1, 2, 3, 4);
        var slice = source.Slice(1, 2);

        var put = slice.Put(0, Member(9));
        var appended = slice.Append(Member(7));

        await Assert.That(At(put, 0)).IsEqualTo(9);
        await Assert.That(At(put, 1)).IsEqualTo(3);
        await Assert.That(appended.Size).IsEqualTo(3);
        await Assert.That(At(appended, 2)).IsEqualTo(7);
        await Assert.That(At(slice, 0)).IsEqualTo(2);
        await Assert.That(At(source, 1)).IsEqualTo(2);
    }

    [Test]
    public async Task CopyTo_WritesTheMembersAtTheStart(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var target = new XdmSequence[4];

        Numbers(1, 2, 3).Slice(1, 2).CopyTo(target, 1);

        await Assert.That(target[0].IsEmpty).IsTrue();
        await Assert.That(target[1][0].GetSaturatedInteger()).IsEqualTo(2);
        await Assert.That(target[2][0].GetSaturatedInteger()).IsEqualTo(3);
    }

    [Test]
    public async Task OutOfRangeAccess_ThrowsArgumentOutOfRange(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var array = Numbers(1, 2);

        _ = await Assert.That(() => array[2]).Throws<ArgumentOutOfRangeException>();
        _ = await Assert.That(() => array[-1]).Throws<ArgumentOutOfRangeException>();
        _ = await Assert.That(() => array.Slice(1, 2)).Throws<ArgumentOutOfRangeException>();
        _ = await Assert.That(() => array.Put(2, Member(1))).Throws<ArgumentOutOfRangeException>();
        _ = await Assert.That(() => XdmArray.Create(null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task TryGet_FindsAnArrayItem_AndNothingElse(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var array = Numbers(1);

        await Assert.That(XdmArray.TryGet(array.ToItem(), out var found)).IsTrue();
        await Assert.That(found).IsSameReferenceAs(array);
        await Assert.That(array.ToItem().Kind).IsEqualTo(XdmItemKind.Function);
        await Assert.That(XdmArray.TryGet(XdmItem.FromInteger(1), out _)).IsFalse();
        await Assert.That(array.Arity).IsEqualTo(1);
        await Assert.That(array.Name).IsNull();
    }

    [Test]
    public async Task Invoke_ReturnsTheMember_OrRaisesFOAY0001(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var array = Numbers(10, 20);
        using var context = new XPathEvaluationContext();

        var member = array.Invoke(context, [Member(2)]);

        await Assert.That(member[0].GetSaturatedInteger()).IsEqualTo(20);
        var error = Assert.Throws<XPathException>(() => array.Invoke(context, [Member(3)]));
        await Assert.That(error.Diagnostic.QName.LocalName).IsEqualTo("FOAY0001");
    }
}
