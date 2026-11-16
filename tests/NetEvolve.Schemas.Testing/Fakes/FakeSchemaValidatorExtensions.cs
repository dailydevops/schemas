namespace NetEvolve.Schemas.Testing.Fakes;

/// <summary>Test-only entry point in the shape every format package uses: an extension member on <see cref="SchemaValidator"/>.</summary>
public static class FakeSchemaValidatorExtensions
{
    extension(SchemaValidator)
    {
        /// <summary>Creates a <see cref="FakeBuilder"/>.</summary>
        /// <returns>A new fake builder.</returns>
        public static FakeBuilder ForFake() => new();
    }
}
