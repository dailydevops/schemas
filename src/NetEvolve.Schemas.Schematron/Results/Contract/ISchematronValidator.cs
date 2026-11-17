#pragma warning disable NE0002 // Approved namespace NetEvolve.Schemas for the marker interface of a format (design #103 AP-30, #137 AP-7)

namespace NetEvolve.Schemas;

using System.Collections.Generic;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Schematron;
using NetEvolve.Schemas.Schematron.Model;

/// <summary>An immutable, thread-safe validator for a compiled Schematron schema. Not intended for implementation by consumers.</summary>
public interface ISchematronValidator : ISchemaValidator
{
    /// <summary>Gets the effective model: includes merged, abstract patterns expanded, all phases listed.</summary>
    SchematronSchema Schema { get; }

    /// <summary>Gets the edition of the schema.</summary>
    SchematronEdition Edition { get; }

    /// <summary>Gets the phase as requested, <c>#DEFAULT</c> when none was set.</summary>
    string Phase { get; }

    /// <summary>Gets the resolved phase id, or <c>#ALL</c>.</summary>
    string EffectivePhase { get; }

    /// <summary>Gets the external parameters the validator was built with.</summary>
    IReadOnlyDictionary<string, string> Parameters { get; }

    /// <summary>Gets the limits the validator was built with.</summary>
    ValidationLimits Limits { get; }

    /// <summary>Gets the resolver the validator was built with.</summary>
    IResourceResolver Resolver { get; }

    /// <summary>Returns a view of this validator that delivers the events of each run to a report sink.</summary>
    /// <param name="sink">The sink.</param>
    /// <returns>
    /// A lightweight view that shares the compiled schema. The view serves one validation at a time; the validator it was
    /// derived from stays thread-safe.
    /// </returns>
    ISchematronValidator WithReportSink(ISchematronReportSink sink);
}
