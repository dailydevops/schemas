namespace NetEvolve.Schemas.XPath.Tests.Unit.Casting.Other;

using System;
using NetEvolve.Schemas.Xml.Datatypes.QNames;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Casting.Other;
using NetEvolve.Schemas.XPath.Casting.Temporal;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Tests.Unit.Casting.Core;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The seam that hands the namespaces of the static context to a conversion: a row that needs them, the overload of the table that takes
/// them, the namespace context over the static context, and the guards of the registration files.
/// </summary>
public class QNameSeamTests
{
    private static readonly XdmTypeId QNameType = XdmTypes.Get("QName");

    private sealed class Bindings(string prefix, string namespaceName) : INamespaceContext
    {
        public bool TryResolvePrefix(ReadOnlySpan<char> name, out string? resolved)
        {
            var match = name.SequenceEqual(prefix.AsSpan());
            resolved = match ? namespaceName : null;
            return match;
        }
    }

    [Test]
    public async Task Table_Overload_PassesTheNamespacesToARowThatNeedsThem()
    {
        var table = CastingCatalog.Default.Table;

        var status = table.TryCast(
            XdmItem.FromString("b:local"),
            QNameType,
            new Bindings("b", "urn:b"),
            out var result
        );

        await Assert.That(status).IsEqualTo(CastStatus.Ok);
        _ = result.TryGetQName(out var name);
        await Assert.That(name!.Name.NamespaceName).IsEqualTo("urn:b");
        await Assert.That(name.Prefix).IsEqualTo("b");
    }

    [Test]
    public async Task Table_Overload_WithoutNamespaces_RefusesALexicalForm_ButNotAQName()
    {
        var table = CastingCatalog.Default.Table;

        await Assert
            .That(table.TryCast(XdmItem.FromString("local"), QNameType, null, out _))
            .IsEqualTo(CastStatus.NotAllowed);
        await Assert
            .That(table.TryCast(XdmItem.FromUntypedAtomic("local"), QNameType, null, out _))
            .IsEqualTo(CastStatus.NotAllowed);
        await Assert
            .That(
                table.TryCast(
                    XdmItem.FromQName(new XdmQName(XsdQNameOf("urn:b", "local"), "b")),
                    QNameType,
                    null,
                    out var kept
                )
            )
            .IsEqualTo(CastStatus.Ok);
        await Assert.That(kept.Kind).IsEqualTo(XdmItemKind.QName);
        await Assert
            .That(table.TryCast(XdmItem.FromString("b:local"), XdmTypeId.XsString, null, out var text))
            .IsEqualTo(CastStatus.Ok);
        await Assert.That(CastFixture.Render(text)).IsEqualTo("string:b:local");
    }

    [Test]
    public async Task Table_Overload_UnboundPrefix_IsAStatus_NotAnException()
    {
        var table = CastingCatalog.Default.Table;

        await Assert
            .That(table.TryCast(XdmItem.FromString("z:local"), QNameType, new Bindings("b", "urn:b"), out _))
            .IsEqualTo(CastStatus.UnboundPrefix);
        await Assert
            .That(table.TryCast(XdmItem.FromString("1z"), QNameType, new Bindings("b", "urn:b"), out _))
            .IsEqualTo(CastStatus.InvalidLexical);
    }

    [Test]
    public async Task Table_ContextFreeOverload_ResolvesAgainstNoNamespaces()
    {
        var table = CastingCatalog.Default.Table;

        await Assert.That(table.TryCast(XdmItem.FromString("local"), QNameType, out _)).IsEqualTo(CastStatus.Ok);
        await Assert
            .That(table.TryCast(XdmItem.FromString("b:local"), QNameType, out _))
            .IsEqualTo(CastStatus.UnboundPrefix);
    }

    [Test]
    public async Task Table_IsNamespaceSensitive_ForTheQNameTargetOnly()
    {
        var table = CastingCatalog.Default.Table;

        await Assert.That(table.IsNamespaceSensitive(QNameType)).IsTrue();
        await Assert.That(table.IsNamespaceSensitive(XdmTypeId.XsString)).IsFalse();
        await Assert.That(table.IsNamespaceSensitive(XdmTypes.Get("date"))).IsFalse();
    }

    [Test]
    public async Task Row_WithANamespaceConversion_KeepsBoth()
    {
        var row = new CastRow(
            XdmTypeId.XsString,
            QNameType,
            CastCell.ValueDependent,
            static (in XdmItem source, INamespaceContext? namespaces, out XdmItem result) =>
            {
                result = namespaces is null ? XdmItem.FromString("none") : XdmItem.FromString("some");
                return CastStatus.Ok;
            }
        );

        await Assert.That(row.NamespaceConversion).IsNotNull();
        await Assert.That(row.Conversion(XdmItem.FromString("x"), out var context)).IsEqualTo(CastStatus.Ok);
        await Assert.That(CastFixture.Render(context)).IsEqualTo("string:some");
        await Assert
            .That(row.NamespaceConversion!(XdmItem.FromString("x"), null, out var none))
            .IsEqualTo(CastStatus.Ok);
        await Assert.That(CastFixture.Render(none)).IsEqualTo("string:none");
        await Assert
            .That(new CastRow(XdmTypeId.XsString, XdmTypeId.XsString, CastCell.Always, Plain).NamespaceConversion)
            .IsNull();
    }

    [Test]
    public async Task Row_Guards_RejectNull()
    {
        await Assert
            .That(() => new CastRow(XdmTypeId.XsString, QNameType, CastCell.Always, (NamespaceCastConversion)null!))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() =>
                new CastRow(
                    XdmTypeId.XsString,
                    QNameType,
                    CastCell.NotAllowed,
                    static (in XdmItem _, INamespaceContext? _, out XdmItem result) =>
                    {
                        result = default;
                        return CastStatus.Ok;
                    }
                )
            )
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task StaticNamespaces_ResolveThePrefixesAndTheDefaultElementNamespaceOfTheContext()
    {
        var statics = QNameFixture
            .V20.ToBuilder()
            .WithNamespace("b", "urn:b")
            .WithDefaultElementNamespace("urn:default")
            .Build();
        var namespaces = new StaticNamespaceContext(statics);

        await Assert.That(namespaces.TryResolvePrefix("b".AsSpan(), out var bound)).IsTrue();
        await Assert.That(bound).IsEqualTo("urn:b");
        await Assert.That(namespaces.TryResolvePrefix("xml".AsSpan(), out var xml)).IsTrue();
        await Assert.That(xml).IsEqualTo("http://www.w3.org/XML/1998/namespace");
        await Assert.That(namespaces.TryResolvePrefix("zz".AsSpan(), out var unbound)).IsFalse();
        await Assert.That(unbound).IsNull();
        await Assert.That(namespaces.TryResolvePrefix(default, out var fallback)).IsTrue();
        await Assert.That(fallback).IsEqualTo("urn:default");
        await Assert
            .That(new StaticNamespaceContext(QNameFixture.V20).TryResolvePrefix(default, out var none))
            .IsFalse();
        await Assert.That(none).IsNull();
    }

    [Test]
    public async Task StaticNamespaces_Constructor_RejectsNull() =>
        await Assert.That(() => new StaticNamespaceContext(null!)).Throws<ArgumentNullException>();

    [Test]
    public async Task Registrations_Guards_RejectNull()
    {
        await Assert.That(() => TemporalRegistration.RegisterRows(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => OtherRegistration.RegisterRows(null!)).Throws<ArgumentNullException>();
    }

    [Test]
    [Arguments(6, "FODT0001")]
    [Arguments(7, "FODT0002")]
    [Arguments(8, "FONS0004")]
    [Arguments(2, "FORG0001")]
    [Arguments(1, "XPTY0004")]
    public async Task Status_MapsToItsErrorCode(int status, string code) =>
        await Assert.That(((CastStatus)status).ToErrorCode().LocalName).IsEqualTo(code);

    [Test]
    public async Task Status_BeyondTheLastMember_IsRejected() =>
        await Assert.That(() => ((CastStatus)9).ToErrorCode()).Throws<ArgumentOutOfRangeException>();

    private static CastStatus Plain(in XdmItem source, out XdmItem result)
    {
        result = source;
        return CastStatus.Ok;
    }

    private static XsdQName XsdQNameOf(string namespaceName, string localName)
    {
        var parsed = XsdQName.Parse(localName.AsSpan(), new Bindings(string.Empty, namespaceName));
        return parsed.Value;
    }
}
