namespace NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Types;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Derivation.Types;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;

public class TypeDerivationCheckTests
{
    private const XsdDerivationMethod Ext = XsdDerivationMethod.Extension;
    private const XsdDerivationMethod Res = XsdDerivationMethod.Restriction;

    private readonly TypeFixture _f = new();

    [Test]
    public async Task ReportAll_ReportsEveryInvalidComplexType_AndSkipsTheOthers()
    {
        var parent = _f.Root("a", TypeFixture.Sequence(TypeFixture.Element("x")), final: Ext | Res);
        var badExtension = _f.Complex("b", Ext, parent, TypeFixture.Sequence(TypeFixture.Element("y")));
        var badRestriction = _f.Complex("c", Res, parent, TypeFixture.Sequence(TypeFixture.Element("x")));
        var fine = _f.Root("d");
        var simple = _f.Restrict("e", TypeFixture.Xs("int"));
        IXsdComponent[] components =
        [
            parent,
            badExtension,
            badRestriction,
            fine,
            simple,
            TypeFixture.Registered("anyType"),
            TypeFixture.Registered("int"),
            ComplexTypeDefinition.AnyType,
        ];

        TypeFixture.Checker().ReportAll(components, _f.Reporter);

        await Assert.That(_f.Issues).Count().IsEqualTo(2);
        await Assert.That(_f.Issues[0].Constraint).IsEqualTo("cos-ct-extends.1.1");
        await Assert.That(_f.Issues[1].Constraint).IsEqualTo("derivation-ok-restriction.1");
    }

    [Test]
    public async Task ReportAll_WithoutViolations_ReportsNothing()
    {
        var parent = _f.Root("a", TypeFixture.Sequence(TypeFixture.Element("x")));
        IXsdComponent[] components =
        [
            parent,
            _f.Complex("b", Ext, parent, TypeFixture.Sequence(TypeFixture.Element("y"))),
        ];

        TypeFixture.Checker().ReportAll(components, _f.Reporter);

        await Assert.That(_f.Issues).Count().IsEqualTo(0);
    }

    [Test]
    public async Task Report_TheUrType_HasNoBaseAndReportsNothing()
    {
        TypeFixture.Checker().Report(ComplexTypeDefinition.AnyType, _f.Reporter);

        await Assert.That(_f.Issues).Count().IsEqualTo(0);
    }

    [Test]
    public async Task Check_EveryMethodCombination_IsAccepted()
    {
        var a = _f.Root("a");
        var check = TypeFixture.Checker();

        for (var method = 0; method <= 31; method++)
        {
            for (var block = 0; block <= 31; block++)
            {
                _ = check.Check(a, a, (XsdDerivationMethod)method, (XsdDerivationMethod)block);
            }
        }

        await Assert.That(check.Check(a, a, Ext, Res).IsValid).IsTrue();
    }

    [Test]
    public async Task Constructor_UndefinedVersion_Throws() =>
        await Assert.That(() => new TypeDerivationCheck((XsdVersion)5)).Throws<ArgumentOutOfRangeException>();

    [Test]
    public async Task Constructor_NegativeVersion_Throws() =>
        await Assert.That(() => new TypeDerivationCheck((XsdVersion)(-1))).Throws<ArgumentOutOfRangeException>();

    [Test]
    public async Task Check_NullDerived_Throws() =>
        await Assert
            .That(() => TypeFixture.Checker().Check(null!, _f.Root("a"), Ext, Ext))
            .Throws<ArgumentNullException>();

    [Test]
    public async Task Check_NullBase_Throws() =>
        await Assert
            .That(() => TypeFixture.Checker().Check(_f.Root("a"), null!, Ext, Ext))
            .Throws<ArgumentNullException>();

    [Test]
    public async Task Check_UndefinedMethod_Throws() =>
        await Assert
            .That(() => TypeFixture.Checker().Check(_f.Root("a"), _f.Root("b"), (XsdDerivationMethod)32, Ext))
            .Throws<ArgumentOutOfRangeException>();

    [Test]
    public async Task Check_NegativeMethod_Throws() =>
        await Assert
            .That(() => TypeFixture.Checker().Check(_f.Root("a"), _f.Root("b"), (XsdDerivationMethod)(-1), Ext))
            .Throws<ArgumentOutOfRangeException>();

    [Test]
    public async Task Check_UndefinedBlockSet_Throws() =>
        await Assert
            .That(() => TypeFixture.Checker().Check(_f.Root("a"), _f.Root("b"), Ext, (XsdDerivationMethod)32))
            .Throws<ArgumentOutOfRangeException>();

    [Test]
    public async Task Report_NullType_Throws() =>
        await Assert.That(() => TypeFixture.Checker().Report(null!, _f.Reporter)).Throws<ArgumentNullException>();

    [Test]
    public async Task Report_NullReporter_Throws() =>
        await Assert.That(() => TypeFixture.Checker().Report(_f.Root("a"), null!)).Throws<ArgumentNullException>();

    [Test]
    public async Task Report_NullBase_Throws() =>
        await Assert
            .That(() => TypeFixture.Checker().Report(_f.Root("a"), (IXsdComponent)null!, _f.Reporter))
            .Throws<ArgumentNullException>();

    [Test]
    public async Task Report_WithABase_NullReporter_Throws() =>
        await Assert
            .That(() => TypeFixture.Checker().Report(_f.Root("a"), _f.Root("b"), null!))
            .Throws<ArgumentNullException>();

    [Test]
    public async Task ReportAll_NullComponents_Throws() =>
        await Assert.That(() => TypeFixture.Checker().ReportAll(null!, _f.Reporter)).Throws<ArgumentNullException>();

    [Test]
    public async Task ReportAll_NullReporter_Throws() =>
        await Assert
            .That(() => TypeFixture.Checker().ReportAll(new List<IXsdComponent>(), null!))
            .Throws<ArgumentNullException>();

    [Test]
    public async Task ReportAll_NullElement_Throws() =>
        await Assert
            .That(() => TypeFixture.Checker().ReportAll(new IXsdComponent[] { null! }, _f.Reporter))
            .Throws<ArgumentNullException>();
}
