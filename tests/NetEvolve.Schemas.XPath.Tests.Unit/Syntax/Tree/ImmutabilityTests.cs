namespace NetEvolve.Schemas.XPath.Tests.Unit.Syntax.Tree;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NetEvolve.Schemas.XPath.Syntax.Tree;

public class ImmutabilityTests
{
    private static List<Type> NodeTypesIncludingBases()
    {
        var types = VisitorTests.NodeKinds();
        foreach (var type in typeof(SyntaxNode).Assembly.GetTypes())
        {
            if (type.IsAbstract && typeof(SyntaxNode).IsAssignableFrom(type))
            {
                types.Add(type);
            }
        }

        return types;
    }

    [Test]
    public async Task NodeTypes_HaveNoPublicSetterAndNoPublicField()
    {
        var offenders = new List<string>();
        foreach (var type in NodeTypesIncludingBases())
        {
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                if (property.SetMethod is { IsPublic: true } || property.SetMethod is { IsFamily: true })
                {
                    offenders.Add(type.Name + "." + property.Name);
                }
            }

            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                if (!field.IsInitOnly && !field.IsLiteral)
                {
                    offenders.Add(type.Name + "." + field.Name);
                }
            }
        }

        await Assert.That(offenders).IsEmpty();
    }

    [Test]
    public async Task NodeTypes_ExposeNoMutableCollectionType()
    {
        var offenders = new List<string>();
        foreach (var type in NodeTypesIncludingBases())
        {
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var propertyType = property.PropertyType;
                if (propertyType.IsArray || IsMutableCollection(propertyType))
                {
                    offenders.Add(type.Name + "." + property.Name);
                }
            }
        }

        await Assert.That(offenders).IsEmpty();
    }

    [Test]
    public async Task ListProperties_AreReadOnlyAtRuntimeAndCopyTheirSource()
    {
        var factory = new TreeFactory();
        var items = new List<Expression> { factory.Int("1"), factory.Int("2") };
        var sequence = factory.Make(s => new SequenceExpression(s, items));

        items.Add(factory.Int("3"));

        await Assert.That(sequence.Items).HasCount(2);
        await Assert.That(((IList<Expression>)sequence.Items).IsReadOnly).IsTrue();
        await Assert.That(((IList)sequence.Children).IsReadOnly).IsTrue();
    }

    private static bool IsMutableCollection(Type type)
    {
        if (!type.IsGenericType)
        {
            return typeof(IList).IsAssignableFrom(type) && !type.IsInterface;
        }

        var definition = type.GetGenericTypeDefinition();
        return definition == typeof(List<>)
            || definition == typeof(ICollection<>)
            || definition == typeof(IList<>)
            || definition == typeof(IDictionary<,>)
            || definition == typeof(Dictionary<,>)
            || definition == typeof(ISet<>)
            || definition == typeof(HashSet<>);
    }
}
