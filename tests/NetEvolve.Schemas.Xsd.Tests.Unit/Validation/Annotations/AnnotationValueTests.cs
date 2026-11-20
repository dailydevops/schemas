namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Annotations;

using System;
using NetEvolve.Schemas.XPath.Xdm.Values;
using NetEvolve.Schemas.Xsd.Validation.Annotations;

public class AnnotationValueTests
{
    [Test]
    public async Task TypeName_Default_HasEmptyNames()
    {
        XsdTypeName name = default;

        await Assert.That(name.NamespaceName).IsEqualTo(string.Empty);
        await Assert.That(name.LocalName).IsEqualTo(string.Empty);
        await Assert.That(name.IsAnonymous).IsFalse();
    }

    [Test]
    public async Task TypeName_Anonymous_HasEmptyNames()
    {
        var name = XsdTypeName.Anonymous;

        await Assert.That(name.IsAnonymous).IsTrue();
        await Assert.That(name.NamespaceName).IsEqualTo(string.Empty);
        await Assert.That(name.LocalName).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task TypeName_Equality_ComparesNamespaceLocalNameAndAnonymity()
    {
        var name = new XsdTypeName("urn:a", "t", false);

        await Assert.That(name == new XsdTypeName("urn:a", "t", false)).IsTrue();
        await Assert.That(name != new XsdTypeName("urn:a", "t", false)).IsFalse();
        await Assert.That(name == new XsdTypeName("urn:b", "t", false)).IsFalse();
        await Assert.That(name == new XsdTypeName("urn:a", "u", false)).IsFalse();
        await Assert.That(name == new XsdTypeName("urn:a", "t", true)).IsFalse();
        await Assert.That(name.Equals((object)new XsdTypeName("urn:a", "t", false))).IsTrue();
        await Assert.That(name.Equals("urn:a")).IsFalse();
        await Assert.That(name.Equals(null)).IsFalse();
        await Assert.That(name.GetHashCode()).IsEqualTo(new XsdTypeName("urn:a", "t", false).GetHashCode());
        await Assert.That(name.GetHashCode()).IsNotEqualTo(new XsdTypeName("urn:a", "u", false).GetHashCode());
    }

    [Test]
    public async Task TypeName_NullNames_Throw()
    {
        await Assert.That(() => new XsdTypeName(null!, "t", false)).Throws<ArgumentNullException>();
        await Assert.That(() => new XsdTypeName("", null!, false)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Annotation_Default_IsNotKnown()
    {
        XsdAnnotation annotation = default;

        await Assert.That(annotation.Validity).IsEqualTo(XsdValidity.NotKnown);
        await Assert.That(annotation.TypeName).IsEqualTo(default(XsdTypeName));
        await Assert.That(annotation.IsNilled).IsFalse();
        await Assert.That(annotation.TypedValueKind).IsEqualTo(XsdTypedValueKind.None);
        await Assert.That(annotation.TypedValueCount).IsEqualTo(0);
        await Assert.That(annotation.CopyTypedValue(new XdmItem[1])).IsEqualTo(0);
        await Assert.That(annotation.GetHashCode()).IsEqualTo(0);
        await Assert.That(annotation == default).IsTrue();
    }

    [Test]
    public async Task Annotation_Equality_ComparesTheSourceAndTheNode()
    {
        var run = AnnotationFixture.Run("<r><i>1</i><i>2</i></r>");
        var other = AnnotationFixture.Run("<r><i>1</i><i>2</i></r>");
        var first = run.Tree.Document.FirstChild.FirstChild;

        var annotation = run.Annotation(first);

        await Assert.That(annotation == run.Annotation(first)).IsTrue();
        await Assert.That(annotation != run.Annotation(first)).IsFalse();
        await Assert.That(annotation.Equals((object)run.Annotation(first))).IsTrue();
        await Assert.That(annotation.Equals("x")).IsFalse();
        await Assert.That(annotation == run.Annotation(first.NextSibling)).IsFalse();
        await Assert.That(annotation == other.Annotation(other.Tree.Document.FirstChild.FirstChild)).IsFalse();
        await Assert.That(annotation == default).IsFalse();
        await Assert.That(annotation.GetHashCode()).IsEqualTo(run.Annotation(first).GetHashCode());
    }

    [Test]
    public async Task Types_AreInternal_UntilTheAssemblyOfTheValidatorExposesThem()
    {
        var names = new[]
        {
            typeof(XsdAnnotation),
            typeof(XsdTypeName),
            typeof(XsdValidity),
            typeof(XsdTypedValueKind),
            typeof(IAnnotationSink),
            typeof(AnnotationTable),
            typeof(AnnotationListener),
            typeof(AnnotationSource),
        };

        var visible = 0;
        foreach (var type in names)
        {
            visible += type.IsPublic ? 1 : 0;
        }

        await Assert.That(visible).IsEqualTo(0);
    }
}
