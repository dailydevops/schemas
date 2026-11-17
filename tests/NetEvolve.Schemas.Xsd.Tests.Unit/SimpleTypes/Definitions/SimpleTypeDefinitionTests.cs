namespace NetEvolve.Schemas.Xsd.Tests.Unit.SimpleTypes.Definitions;

using System;
using System.Collections.Generic;
using System.Reflection;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;

public class SimpleTypeDefinitionTests
{
    private static readonly DocumentLocation Where = new("file:///a.xsd", 5, 3);
    private static readonly XPathQName Named = new("urn:test", "t");
    private static readonly IFacet[] NoFacets = [];

    private readonly ValidationIssueCollector _collector = new(ValidationLimits.Default);

    private XsdIssueReporter Reporter => new(_collector);

    private static SimpleTypeDefinition Builtin(string name, DatatypeVariety variety, params IFacet[] facets) =>
        SimpleTypeDefinition.FromDatatype(
            new XPathQName("http://www.w3.org/2001/XMLSchema", name),
            Where,
            new FakeDatatype(name, variety, facets)
        );

    [Test]
    public async Task FromDatatype_TakesVarietyAndFacetsFromTheDatatype()
    {
        var facet = new FakeFacet(FacetKind.Length);

        var type = Builtin("string", DatatypeVariety.Atomic, facet);

        await Assert.That(type.Kind).IsEqualTo(XsdComponentKind.SimpleType);
        await Assert.That(type.Variety).IsEqualTo(DatatypeVariety.Atomic);
        await Assert.That(type.Facets).Count().IsEqualTo(1);
        await Assert.That(type.Facets[0]).IsSameReferenceAs(facet);
        await Assert.That(type.BaseType).IsNull();
        await Assert.That(type.Name.LocalName).IsEqualTo("string");
        await Assert.That(type.Location.Line).IsEqualTo(5);
    }

    [Test]
    public async Task Restrict_Atomic_RecordsBaseTypeAndKeepsTheVariety()
    {
        var baseType = Builtin("string", DatatypeVariety.Atomic);

        var type = SimpleTypeDefinition.Restrict(Named, Where, XsdDerivationMethod.None, baseType, NoFacets, Reporter);

        await Assert.That(type).IsNotNull();
        await Assert.That(type!.Variety).IsEqualTo(DatatypeVariety.Atomic);
        await Assert.That(type.BaseType).IsSameReferenceAs(baseType);
        await Assert.That(type.ItemType).IsNull();
        await Assert.That(type.MemberTypes).Count().IsEqualTo(0);
        await Assert.That(type.Name).IsEqualTo(Named);
        await Assert.That(type.IsAnonymous).IsFalse();
        await Assert.That(_collector.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Restrict_Anonymous_HasNoName()
    {
        var type = SimpleTypeDefinition.Restrict(
            default,
            Where,
            XsdDerivationMethod.None,
            Builtin("string", DatatypeVariety.Atomic),
            NoFacets,
            Reporter
        );

        await Assert.That(type!.IsAnonymous).IsTrue();
    }

    [Test]
    public async Task Restrict_List_KeepsItemTypeAndVariety()
    {
        var item = Builtin("int", DatatypeVariety.Atomic);
        var list = SimpleTypeDefinition.List(Named, Where, XsdDerivationMethod.None, item, NoFacets, Reporter)!;

        var type = SimpleTypeDefinition.Restrict(
            new XPathQName("urn:test", "r"),
            Where,
            XsdDerivationMethod.None,
            list,
            NoFacets,
            Reporter
        );

        await Assert.That(type!.Variety).IsEqualTo(DatatypeVariety.List);
        await Assert.That(type.ItemType).IsSameReferenceAs(item);
        await Assert.That(type.BaseType).IsSameReferenceAs(list);
    }

    [Test]
    public async Task Restrict_Union_KeepsMemberTypesAndVariety()
    {
        var first = Builtin("int", DatatypeVariety.Atomic);
        var second = Builtin("string", DatatypeVariety.Atomic);
        var union = SimpleTypeDefinition.Union(
            Named,
            Where,
            XsdDerivationMethod.None,
            [first, second],
            NoFacets,
            Reporter
        )!;

        var type = SimpleTypeDefinition.Restrict(
            new XPathQName("urn:test", "r"),
            Where,
            XsdDerivationMethod.None,
            union,
            NoFacets,
            Reporter
        );

        await Assert.That(type!.Variety).IsEqualTo(DatatypeVariety.Union);
        await Assert.That(type.MemberTypes).Count().IsEqualTo(2);
        await Assert.That(type.MemberTypes[0]).IsSameReferenceAs(first);
        await Assert.That(type.MemberTypes[1]).IsSameReferenceAs(second);
    }

    [Test]
    public async Task List_RecordsItemTypeAndHasVarietyList()
    {
        var item = Builtin("int", DatatypeVariety.Atomic);

        var type = SimpleTypeDefinition.List(Named, Where, XsdDerivationMethod.None, item, NoFacets, Reporter);

        await Assert.That(type!.Variety).IsEqualTo(DatatypeVariety.List);
        await Assert.That(type.ItemType).IsSameReferenceAs(item);
        await Assert.That(type.MemberTypes).Count().IsEqualTo(0);
        await Assert.That(_collector.Count).IsEqualTo(0);
    }

    [Test]
    public async Task List_OfUnionOfAtomicTypes_IsAllowed()
    {
        var union = SimpleTypeDefinition.Union(
            Named,
            Where,
            XsdDerivationMethod.None,
            [Builtin("int", DatatypeVariety.Atomic)],
            NoFacets,
            Reporter
        )!;

        var type = SimpleTypeDefinition.List(
            new XPathQName("urn:test", "l"),
            Where,
            XsdDerivationMethod.None,
            union,
            NoFacets,
            Reporter
        );

        await Assert.That(type).IsNotNull();
    }

    [Test]
    public async Task Union_RecordsMemberTypesInOrderAndHasVarietyUnion()
    {
        var first = Builtin("int", DatatypeVariety.Atomic);
        var second = Builtin("string", DatatypeVariety.Atomic);

        var type = SimpleTypeDefinition.Union(
            Named,
            Where,
            XsdDerivationMethod.None,
            [first, second],
            NoFacets,
            Reporter
        );

        await Assert.That(type!.Variety).IsEqualTo(DatatypeVariety.Union);
        await Assert.That(type.MemberTypes[0]).IsSameReferenceAs(first);
        await Assert.That(type.MemberTypes[1]).IsSameReferenceAs(second);
        await Assert.That(type.ItemType).IsNull();
    }

    [Test]
    public async Task List_OfList_IsALocatedSchemaError()
    {
        var inner = SimpleTypeDefinition.List(
            Named,
            Where,
            XsdDerivationMethod.None,
            Builtin("int", DatatypeVariety.Atomic),
            NoFacets,
            Reporter
        )!;

        var type = SimpleTypeDefinition.List(
            new XPathQName("urn:test", "outer"),
            new DocumentLocation("file:///a.xsd", 9, 1),
            XsdDerivationMethod.None,
            inner,
            NoFacets,
            Reporter
        );

        await Assert.That(type).IsNull();
        await AssertSingleError(XsdRuleCodes.CosStRestricts, 9);
    }

    [Test]
    public async Task List_OfUnionContainingAList_IsALocatedSchemaError()
    {
        var list = SimpleTypeDefinition.List(
            Named,
            Where,
            XsdDerivationMethod.None,
            Builtin("int", DatatypeVariety.Atomic),
            NoFacets,
            Reporter
        )!;
        var union = SimpleTypeDefinition.Union(
            new XPathQName("urn:test", "u"),
            Where,
            XsdDerivationMethod.None,
            [list],
            NoFacets,
            Reporter
        )!;

        var type = SimpleTypeDefinition.List(
            new XPathQName("urn:test", "outer"),
            Where,
            XsdDerivationMethod.None,
            union,
            NoFacets,
            Reporter
        );

        await Assert.That(type).IsNull();
        await AssertSingleError(XsdRuleCodes.CosStRestricts, 5);
    }

    [Test]
    public async Task List_OfNonSimpleType_IsALocatedSchemaError()
    {
        var complex = new FakeComponent(XsdComponentKind.ComplexType, "c");

        var type = SimpleTypeDefinition.List(Named, Where, XsdDerivationMethod.None, complex, NoFacets, Reporter);

        await Assert.That(type).IsNull();
        await AssertSingleError(XsdRuleCodes.CosStRestricts, 5);
    }

    [Test]
    public async Task Union_WithNonSimpleMember_IsALocatedSchemaError()
    {
        var complex = new FakeComponent(XsdComponentKind.ComplexType, "c");

        var type = SimpleTypeDefinition.Union(
            Named,
            Where,
            XsdDerivationMethod.None,
            [Builtin("int", DatatypeVariety.Atomic), complex],
            NoFacets,
            Reporter
        );

        await Assert.That(type).IsNull();
        await AssertSingleError(XsdRuleCodes.CosStRestricts, 5);
    }

    [Test]
    public async Task Restrict_NonSimpleBase_IsALocatedSchemaError()
    {
        var complex = new FakeComponent(XsdComponentKind.ComplexType, "c");

        var type = SimpleTypeDefinition.Restrict(Named, Where, XsdDerivationMethod.None, complex, NoFacets, Reporter);

        await Assert.That(type).IsNull();
        await AssertSingleError(XsdRuleCodes.CosStRestricts, 5);
    }

    [Test]
    public async Task Final_Restriction_BlocksDerivationByRestriction()
    {
        var baseType = FinalBase(XsdDerivationMethod.Restriction);

        var type = SimpleTypeDefinition.Restrict(
            new XPathQName("urn:test", "d"),
            new DocumentLocation("file:///a.xsd", 12, 1),
            XsdDerivationMethod.None,
            baseType,
            NoFacets,
            Reporter
        );

        await Assert.That(type).IsNull();
        await AssertSingleError(XsdRuleCodes.CosStRestricts, 12);
    }

    [Test]
    public async Task Final_List_BlocksDerivationByList()
    {
        var item = FinalBase(XsdDerivationMethod.List);

        var type = SimpleTypeDefinition.List(
            new XPathQName("urn:test", "d"),
            new DocumentLocation("file:///a.xsd", 13, 1),
            XsdDerivationMethod.None,
            item,
            NoFacets,
            Reporter
        );

        await Assert.That(type).IsNull();
        await AssertSingleError(XsdRuleCodes.CosStRestricts, 13);
    }

    [Test]
    public async Task Final_Union_BlocksDerivationByUnion()
    {
        var member = FinalBase(XsdDerivationMethod.Union);

        var type = SimpleTypeDefinition.Union(
            new XPathQName("urn:test", "d"),
            new DocumentLocation("file:///a.xsd", 14, 1),
            XsdDerivationMethod.None,
            [member],
            NoFacets,
            Reporter
        );

        await Assert.That(type).IsNull();
        await AssertSingleError(XsdRuleCodes.CosStRestricts, 14);
    }

    [Test]
    [Arguments(4)]
    [Arguments(8)]
    public async Task Final_OtherMethods_DoNotBlockRestriction(int finalMask)
    {
        var final = (XsdDerivationMethod)finalMask;
        var type = SimpleTypeDefinition.Restrict(
            Named,
            Where,
            XsdDerivationMethod.None,
            FinalBase(final),
            NoFacets,
            Reporter
        );

        await Assert.That(type).IsNotNull();
        await Assert.That(_collector.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Final_IsRecordedOnTheDefinition_LimitedToTheSimpleTypeMethods()
    {
        var type = SimpleTypeDefinition.Restrict(
            Named,
            Where,
            XsdDerivationMethod.Restriction | XsdDerivationMethod.Extension,
            Builtin("string", DatatypeVariety.Atomic),
            NoFacets,
            Reporter
        );

        await Assert.That(type!.Final).IsEqualTo(XsdDerivationMethod.Restriction);
    }

    [Test]
    public async Task Restrict_SeesTheInheritedFacetsOfTheBaseAndAddsItsOwn()
    {
        var inherited = new FakeFacet(FacetKind.MaxLength);
        var own = new FakeFacet(FacetKind.Pattern);
        var baseType = Builtin("string", DatatypeVariety.Atomic, inherited);

        var type = SimpleTypeDefinition.Restrict(Named, Where, XsdDerivationMethod.None, baseType, [own], Reporter);

        await Assert.That(type!.Facets).Count().IsEqualTo(2);
        await Assert.That(type.Facets[0]).IsSameReferenceAs(inherited);
        await Assert.That(type.Facets[1]).IsSameReferenceAs(own);
        await Assert.That(baseType.Facets).Count().IsEqualTo(1);
    }

    [Test]
    public async Task Restrict_FacetsInheritAcrossSeveralSteps()
    {
        var first = new FakeFacet(FacetKind.MinLength);
        var second = new FakeFacet(FacetKind.MaxLength);
        var root = Builtin("string", DatatypeVariety.Atomic, first);
        var middle = SimpleTypeDefinition.Restrict(Named, Where, XsdDerivationMethod.None, root, [second], Reporter)!;

        var leaf = SimpleTypeDefinition.Restrict(
            new XPathQName("urn:test", "leaf"),
            Where,
            XsdDerivationMethod.None,
            middle,
            NoFacets,
            Reporter
        )!;

        await Assert.That(leaf.Facets).Count().IsEqualTo(2);
        await Assert.That(leaf.Facets[0]).IsSameReferenceAs(first);
        await Assert.That(leaf.Facets[1]).IsSameReferenceAs(second);
    }

    [Test]
    public async Task List_CarriesOnlyItsOwnFacets()
    {
        var own = new FakeFacet(FacetKind.MaxLength);

        var list = SimpleTypeDefinition.List(
            Named,
            Where,
            XsdDerivationMethod.None,
            Builtin("int", DatatypeVariety.Atomic, new FakeFacet(FacetKind.MinInclusive)),
            [own],
            Reporter
        )!;

        await Assert.That(list.Facets).Count().IsEqualTo(1);
        await Assert.That(list.Facets[0]).IsSameReferenceAs(own);
    }

    [Test]
    public async Task Architecture_Folder_ReferencesDatatypesOnlyThroughTheXmlContract()
    {
        var xml = typeof(IDatatypeDescriptor).Assembly;
        var offenders = new List<string>();
        foreach (var type in typeof(SimpleTypeDefinition).Assembly.GetTypes())
        {
            if (!string.Equals(type.Namespace, typeof(SimpleTypeDefinition).Namespace, StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var used in UsedTypes(type))
            {
                if (
                    used.Assembly == xml
                    && !string.Equals(used.Namespace, typeof(IDatatypeDescriptor).Namespace, StringComparison.Ordinal)
                )
                {
                    offenders.Add($"{type.Name} uses {used.FullName}");
                }
            }
        }

        await Assert.That(offenders).IsEmpty();
    }

    private static SimpleTypeDefinition FinalBase(XsdDerivationMethod final) =>
        SimpleTypeDefinition.FromDatatype(
            new XPathQName("urn:test", "base"),
            Where,
            new FakeDatatype("base", DatatypeVariety.Atomic),
            final
        );

    private static IEnumerable<Type> UsedTypes(Type type)
    {
        const BindingFlags All =
            BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Instance
            | BindingFlags.Static
            | BindingFlags.DeclaredOnly;
        foreach (var field in type.GetFields(All))
        {
            yield return Element(field.FieldType);
        }

        foreach (var property in type.GetProperties(All))
        {
            yield return Element(property.PropertyType);
        }

        foreach (var method in type.GetMethods(All))
        {
            yield return Element(method.ReturnType);
            foreach (var parameter in method.GetParameters())
            {
                yield return Element(parameter.ParameterType);
            }
        }

        foreach (var constructor in type.GetConstructors(All))
        {
            foreach (var parameter in constructor.GetParameters())
            {
                yield return Element(parameter.ParameterType);
            }
        }
    }

    // Generic arguments (IReadOnlyList<IFacet>) are unwrapped one level so the facet type is judged too.
    private static Type Element(Type type)
    {
        var current = type.HasElementType ? type.GetElementType()! : type;
        return current.IsGenericType && current.GetGenericArguments().Length == 1
            ? current.GetGenericArguments()[0]
            : current;
    }

    private async Task AssertSingleError(DiagnosticCode code, int line)
    {
        var issues = _collector.ToResult().Issues;
        await Assert.That(issues).Count().IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(code);
        await Assert.That(issues[0].Location.Line).IsEqualTo(line);
    }
}
