namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Contracts.Fakes;

using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;

/// <summary>Builds the pieces a validation step needs without a parser: a hand-built declaration model and a context.</summary>
internal static class DtdContractFixture
{
    /// <summary>Creates a declaration model with a single element declaration, built by the shared builder.</summary>
    internal static DtdDeclarations Declarations()
    {
        var builder = new DtdDeclarationBuilder("root", publicId: null, systemId: null);
        builder.AddElement(new DtdElementDeclaration("root", DtdContentKind.Empty, content: null));

        return builder.Build();
    }

    /// <summary>Creates a context over a hand-built model; the collector receives every reported issue.</summary>
    internal static DtdValidationContext Context(
        ValidationIssueCollector issues,
        DtdValidationOptions? options = null,
        bool isStandalone = false,
        string? systemId = "doc.xml"
    ) => new(Declarations(), options ?? DtdValidationOptions.Default, issues, isStandalone, systemId);
}
