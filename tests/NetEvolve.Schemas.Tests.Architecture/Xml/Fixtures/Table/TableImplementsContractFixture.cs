namespace NetEvolve.Schemas.Tests.Architecture.Xml.Fixtures.Table;

/// <summary>Allowed fixture: depends only on the contract namespace, like the name table.</summary>
internal static class TableImplementsContractFixture
{
    public static object Create() => new Contracts.ContractFixture();
}
