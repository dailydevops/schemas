namespace NetEvolve.Schemas.Xsd.Tests.Unit.Assertions.Facet;

using System;
using System.Collections.Generic;
using System.Reflection;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.Xsd.Assertions.Facet;

/// <summary>The facet is evaluated on the value alone: no tree is built, so the check stays streaming.</summary>
public class AssertionFacetStreamingTests
{
    [Test]
    [Arguments("true()", "3")]
    [Arguments("false()", "3")]
    [Arguments("$value > 1", "3")]
    [Arguments("exists($value/child::a)", "3")]
    [Arguments(". = 1", "3")]
    [Arguments("xs:integer($value) = 1", "abc")]
    public async Task EvaluatingAnAssertion_LoadsNoDocument_AndHoldsNoNode(string test, string text)
    {
        using var fixture = new FacetAssertionFixture();
        var holding = fixture.Base("string", test);
        var noNode = fixture.Base("list:integer", "not($value instance of node()+)");

        _ = fixture.Check(holding, text);

        await Assert.That(fixture.Check(noNode, "1 2 3")).IsTrue();
        await Assert.That(fixture.ResolverCalls).IsEqualTo(0);
    }

    [Test]
    public async Task TheTypesOfTheFacetFolder_ReferenceNoTreeBuilderAndNoNode()
    {
        var banned = new[] { typeof(XmlTreeBuilder), typeof(XmlDocumentTree), typeof(IXmlSink), typeof(XPathNode) };
        var found = 0;
        foreach (var type in typeof(FacetAssertion).Assembly.GetTypes())
        {
            if (!string.Equals(type.Namespace, typeof(FacetAssertion).Namespace, StringComparison.Ordinal))
            {
                continue;
            }

            found++;
            foreach (var used in SignatureTypes(type))
            {
                await Assert.That(Array.IndexOf(banned, used)).IsEqualTo(-1);
            }
        }

        await Assert.That(found).IsGreaterThanOrEqualTo(2);
    }

    private static List<Type> SignatureTypes(Type type)
    {
        const BindingFlags all =
            BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Instance
            | BindingFlags.Static
            | BindingFlags.DeclaredOnly;
        var used = new List<Type>();
        foreach (var field in type.GetFields(all))
        {
            used.Add(field.FieldType);
        }

        foreach (var property in type.GetProperties(all))
        {
            used.Add(property.PropertyType);
        }

        foreach (var method in type.GetMethods(all))
        {
            used.Add(method.ReturnType);
            foreach (var parameter in method.GetParameters())
            {
                used.Add(parameter.ParameterType);
            }
        }

        return used;
    }
}
