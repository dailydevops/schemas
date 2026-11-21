namespace NetEvolve.Schemas.Tests.Architecture.Xsd.Fixtures.SchemaDocuments;

/// <summary>Violating fixture: the syntax model that depends on the component registry.</summary>
internal static class SchemaDocumentsUseComponentsFixture
{
    public static object Create() => new Components.ComponentFixture();
}
