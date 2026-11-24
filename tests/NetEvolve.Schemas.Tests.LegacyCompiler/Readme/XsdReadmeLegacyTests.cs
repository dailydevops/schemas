namespace NetEvolve.Schemas.Tests.LegacyCompiler.Readme;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>Compiled with C# 13: the region between the sample markers is a code block of src/NetEvolve.Schemas.Xsd/README.md.</summary>
public class XsdReadmeLegacyTests
{
    private const string Schema = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
          <xs:element name="order" type="xs:int"/>
        </xs:schema>
        """;

    [Test]
    public async Task NamedExtensionClassSample_CompilesWithoutExtensionMembers(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string schema = Schema;

        // <sample>
        var builder = XsdSchemaValidatorExtensions.ForXsd();
        // </sample>

        var validator = await builder.AddSchema(schema.AsMemory()).BuildAsync(cancellationToken);
        var result = await validator.ValidateAsync("<order>x</order>".AsMemory(), cancellationToken: cancellationToken);

        await Assert.That(builder).IsTypeOf<XsdValidatorBuilder>();
        await Assert.That(result.IsValid).IsFalse();
    }
}
