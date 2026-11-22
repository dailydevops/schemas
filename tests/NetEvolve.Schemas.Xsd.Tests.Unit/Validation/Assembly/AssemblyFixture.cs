namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Assembly;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using XsdVersion = NetEvolve.Schemas.Xml.Datatypes.Contracts.XsdVersion;

/// <summary>Schemas, instances, and builders for the tests of the validator assembly.</summary>
internal static class AssemblyFixture
{
    public const string Xs = "http://www.w3.org/2001/XMLSchema";

    public const string Tns = "urn:t";

    public const string SchemaId = "file:///s.xsd";

    public const string InstanceId = "file:///i.xml";

    /// <summary>A root with repeated integer items and an optional ID attribute; the non-ASCII character is two bytes in UTF-8.</summary>
    public static readonly string Schema = WrapSchema(
        """
        <xs:element name="root">
          <xs:complexType>
            <xs:sequence>
              <xs:element name="item" type="xs:int" maxOccurs="unbounded"/>
            </xs:sequence>
            <xs:attribute name="id" type="xs:ID"/>
          </xs:complexType>
        </xs:element>
        """
    );

    public const string ValidInstance = "<root xmlns=\"urn:t\" id=\"a\"><item>1</item><item>2</item></root>";

    /// <summary>Four problems: a bad value, an unknown element after it, a second bad value, and an undeclared attribute.</summary>
    public const string InvalidInstance =
        "<root xmlns=\"urn:t\" extra=\"ü\">\n  <item>x</item>\n  <item>ü</item>\n  <bad/>\n</root>";

    public static string WrapSchema(string body, string attributes = "") =>
        "<xs:schema xmlns:xs=\""
        + Xs
        + "\" xmlns:t=\""
        + Tns
        + "\" targetNamespace=\""
        + Tns
        + "\" elementFormDefault=\"qualified\" "
        + attributes
        + ">\n"
        + body
        + "\n</xs:schema>";

    public static IXsdValidator Build(
        string schema,
        XsdVersion version = XsdVersion.V11,
        bool dtd = false,
        ValidationLimits? limits = null
    )
    {
        var builder = SchemaValidator.ForXsd().WithVersion(version).WithDtdProcessing(dtd);
        if (limits is not null)
        {
            _ = builder.WithLimits(limits);
        }

        return builder.AddSchema(schema.AsMemory(), SchemaId).Build();
    }

    /// <summary>Runs a build that must fail and returns the compilation exception.</summary>
    public static SchemaCompilationException Failure(Func<IXsdValidator> build)
    {
        try
        {
            _ = build();
        }
        catch (SchemaCompilationException exception)
        {
            return exception;
        }

        throw new InvalidOperationException("The build succeeded.");
    }

    public static bool Same(ValidationResult left, ValidationResult right) =>
        string.Equals(Describe(left), Describe(right), StringComparison.Ordinal);

    public static ValidationResult Run(IXsdValidator validator, string instance) =>
        validator.Validate(instance.AsSpan(), InstanceId);

    public static ValidationResult Run(IXsdValidator validator, ReadOnlySequence<byte> instance) =>
        validator.Validate(instance, InstanceId);

    public static ValidationResult Run(IXsdValidator validator, byte[] instance) =>
        validator.Validate(instance.AsSpan(), InstanceId);

    public static ValidationResult Run(string schema, string instance, bool dtd = false) =>
        Run(Build(schema, dtd: dtd), instance);

    public static string Describe(ValidationResult result)
    {
        var text = new StringBuilder();
        foreach (var issue in result.Issues)
        {
            _ = text.Append(issue.Code)
                .Append('|')
                .Append(issue.Severity)
                .Append('|')
                .Append(issue.Constraint)
                .Append('|')
                .Append(issue.Message)
                .Append('|')
                .Append(issue.Location.Source)
                .Append('|')
                .Append(issue.Location.Line)
                .Append(':')
                .Append(issue.Location.Column)
                .Append('\n');
        }

        return text.Append(result.IsValid).Append(result.IsTruncated).ToString();
    }

    /// <summary>Splits the content into three segments, the first cut in the middle of the two byte character when there is one.</summary>
    public static ReadOnlySequence<T> Segmented<T>(T[] content, int firstCut)
    {
        var first = new Segment<T>(content.AsMemory(0, firstCut));
        var second = first.Append(content.AsMemory(firstCut, (content.Length - firstCut) / 2));
        var third = second.Append(content.AsMemory(firstCut + (content.Length - firstCut) / 2));
        return new ReadOnlySequence<T>(first, 0, third, third.Memory.Length);
    }

    private sealed class Segment<T> : ReadOnlySequenceSegment<T>
    {
        public Segment(ReadOnlyMemory<T> memory) => Memory = memory;

        public Segment<T> Append(ReadOnlyMemory<T> memory)
        {
            var next = new Segment<T>(memory) { RunningIndex = RunningIndex + Memory.Length };
            Next = next;
            return next;
        }
    }
}
