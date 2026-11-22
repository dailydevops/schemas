namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Attributes;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Dtd.Validation.Attributes;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Attributes.AttributeStepModel;

/// <summary>The lexical helpers of the attribute step: guards and the token walk.</summary>
public class AttributeValuesTests
{
    [Test]
    public async Task Matches_WithoutADefinition_Throws() =>
        await Assert.That(() => DtdAttributeValues.Matches(null!, "x".AsSpan())).Throws<ArgumentNullException>();

    [Test]
    public async Task Contains_WithoutValues_Throws() =>
        await Assert.That(() => DtdAttributeValues.Contains(null!, "x".AsSpan())).Throws<ArgumentNullException>();

    [Test]
    public async Task TryNormalizeDefault_WithoutALiteral_Throws()
    {
        var declarations = new AttributeStepModel().Build();

        await Assert
            .That(() =>
                DtdAttributeValues.TryNormalizeDefault(
                    null!,
                    DtdAttributeType.Cdata,
                    declarations,
                    XmlReaderLimits.Default,
                    out _
                )
            )
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task TryNormalizeDefault_WithoutDeclarations_Throws() =>
        await Assert
            .That(() =>
                DtdAttributeValues.TryNormalizeDefault(
                    "x",
                    DtdAttributeType.Cdata,
                    null!,
                    XmlReaderLimits.Default,
                    out _
                )
            )
            .Throws<ArgumentNullException>();

    [Test]
    public async Task TryNormalizeDefault_WithoutLimits_Throws()
    {
        var declarations = new AttributeStepModel().Build();

        await Assert
            .That(() => DtdAttributeValues.TryNormalizeDefault("x", DtdAttributeType.Cdata, declarations, null!, out _))
            .Throws<ArgumentNullException>();
    }

    [Test]
    [Arguments(DtdAttributeType.Cdata, "  a  b  ", "  a  b  ")]
    [Arguments(DtdAttributeType.NmTokens, "  a  b  ", "a b")]
    [Arguments(DtdAttributeType.Cdata, "a&#x20;&#x20;b", "a  b")]
    [Arguments(DtdAttributeType.Cdata, "a\r\nb\rc\nd\te", "a b c d e")]
    public async Task TryNormalizeDefault_ExpandsReferencesAndNormalizesByTheType(
        DtdAttributeType type,
        string literal,
        string expected
    )
    {
        var declarations = new AttributeStepModel().Build();

        var ok = DtdAttributeValues.TryNormalizeDefault(
            literal,
            type,
            declarations,
            XmlReaderLimits.Default,
            out var value
        );

        await Assert.That(ok).IsTrue();
        await Assert.That(value).IsEqualTo(expected);
    }

    [Test]
    public async Task TryNormalizeDefault_WhenItFails_ReturnsAnEmptyValue()
    {
        var declarations = new AttributeStepModel().Build();

        var ok = DtdAttributeValues.TryNormalizeDefault(
            "&ghost;",
            DtdAttributeType.Cdata,
            declarations,
            XmlReaderLimits.Default,
            out var value
        );

        await Assert.That(ok).IsFalse();
        await Assert.That(value).IsEmpty();
    }
}
