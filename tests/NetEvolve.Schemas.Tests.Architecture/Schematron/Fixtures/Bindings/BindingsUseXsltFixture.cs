namespace NetEvolve.Schemas.Tests.Architecture.Schematron.Fixtures.Bindings;

/// <summary>Violating fixture: a query binding that depends on the XSLT engine.</summary>
internal static class BindingsUseXsltFixture
{
    public static object Create() => new Xslt.XsltFixture();
}
