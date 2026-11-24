namespace NetEvolve.Schemas.Schematron.Seams;

using NetEvolve.Schemas.Schematron.Model;

/// <summary>Stage Compile.</summary>
internal interface ISchemaCompiler
{
    /// <summary>Compiles the schema without XSLT modules; the same as the overload with <see cref="XsltModuleSet.Empty"/>.</summary>
    bool TryCompile(
        SchematronSchema effective,
        BuildSettings settings,
        IProblemSink problems,
        out CompiledSchema compiled
    );

    /// <summary>Compiles the schema with the XSLT modules that stage Resolve linked (design #150 EP-14, an additive change of AP-26).</summary>
    bool TryCompile(
        SchematronSchema effective,
        BuildSettings settings,
        XsltModuleSet modules,
        IProblemSink problems,
        out CompiledSchema compiled
    );
}
