namespace NetEvolve.Schemas.Xsd.Tests.Unit.OpenContent;

using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.OpenContent;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;
using NetEvolve.Schemas.Xsd.Wildcards.Core;

/// <summary>Open content over an XSD 1.1 <c>all</c> group; the schema pipeline reads no <c>openContent</c> yet, so the types are built through the factories.</summary>
public class OpenContentAllGroup11Tests
{
    private const string Foreign = " xmlns:f=\"urn:foreign\"";

    private static SchemaFixture Schema(OpenContentMode mode)
    {
        var schema = new SchemaFixture();
        var content = SchemaFixture.All(
            SchemaFixture.Part("a", SchemaFixture.Simple("string"), 1, 2),
            SchemaFixture.Part("b", SchemaFixture.Simple("string"))
        );
        var open = OpenContentElement.Create(
            mode,
            SchemaFixture.Wild("##other", XsdProcessContents.Lax),
            SchemaFixture.Where,
            schema.Reporter
        );
        schema.Global(
            "r",
            ComplexTypeDefinition.Implicit(SchemaFixture.Header(), false, content, [], null, schema.Reporter, open)
        );
        return schema;
    }

    [Test]
    public async Task Interleave_TakesAForeignElementBetweenTheMembers()
    {
        var schema = Schema(OpenContentMode.Interleave);

        var valid = ValidationHarness.Run(schema, "<r" + Foreign + "><a/><f:z/><a/><b/></r>");
        var invalid = ValidationHarness.Run(schema, "<r" + Foreign + "><a/><f:z/></r>");

        await Assert.That(valid.Count).IsEqualTo(0);
        await Assert.That(invalid.Count).IsEqualTo(1);
        await Assert.That(invalid[0].Message).Contains("'b'");
    }

    [Test]
    public async Task Suffix_BeginsOnlyWhenTheAllGroupMayEnd()
    {
        var schema = Schema(OpenContentMode.Suffix);

        var valid = ValidationHarness.Run(schema, "<r" + Foreign + "><b/><a/><f:z/></r>");
        var early = ValidationHarness.Run(schema, "<r" + Foreign + "><b/><f:z/><a/></r>");
        var incomplete = ValidationHarness.Run(schema, "<r" + Foreign + "><b/><f:z/></r>");

        await Assert.That(valid.Count).IsEqualTo(0);
        await Assert.That(early.Count).IsEqualTo(0 + 1);
        await Assert.That(incomplete.Count).IsEqualTo(1);
    }
}
