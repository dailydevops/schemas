namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Subordinate;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Schematron.Evaluation.Subordinate;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Parsing.Core;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Loading.Inclusion.InclusionFixture;

/// <summary>Parses a schema, runs the check of the subordinate document constructs, and keeps the problems.</summary>
internal static class SubordinateFixture
{
    internal static SchematronSchema Load(string xml)
    {
        var problems = new FakeProblemSink();
        return SchematronCoreParser.TryParse(Document(xml), RootId, problems, out var schema) && schema is not null
            ? schema
            : throw new InvalidOperationException("The test schema must parse; problems: " + problems.Problems.Count);
    }

    internal static (bool Ok, FakeProblemSink Problems) Check(
        IReadOnlyList<SchematronPatternBase> patterns,
        IResourceResolver resolver
    )
    {
        var problems = new FakeProblemSink();
        return (SubordinateConstructs.TryCheck(patterns, resolver, problems), problems);
    }

    internal static (bool Ok, FakeProblemSink Problems) Check(string xml, IResourceResolver resolver) =>
        Check(Load(xml).Patterns, resolver);

    /// <summary>A rule on <c>a</c> with one failing assert; <paramref name="attributes"/> are added to the rule.</summary>
    internal static string Failing(string attributes = "") =>
        El("rule", " context=\"a\"" + attributes, "<assert test=\"false()\">m</assert>");
}
