namespace NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>Builds the smallest valid model and compiled schema for contract tests.</summary>
internal static class FakeModel
{
    internal static readonly DocumentLocation Origin = new("fake.sch", 1, 1);

    internal static SchematronSchema Schema(SchematronEdition edition = SchematronEdition.Iso2016) =>
        new(Origin, null, [], [], [], [], null, null, "fake", null, null, edition, null, null, [], []);

    internal static CompiledSchema Compiled(string effectivePhase = "#ALL") => new(Schema(), effectivePhase);

    internal static SchematronMessage Message(string text) => new(Origin, [new SchematronText(Origin, text)]);

    internal static IReadOnlyList<T> None<T>() => Array.Empty<T>();
}
