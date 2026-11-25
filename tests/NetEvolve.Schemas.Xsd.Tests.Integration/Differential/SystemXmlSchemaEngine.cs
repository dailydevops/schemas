namespace NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

using System.Xml;
using System.Xml.Schema;

/// <summary>The baseline: System.Xml.Schema of the running runtime. Only local files are read.</summary>
internal sealed class SystemXmlSchemaEngine : IDifferentialEngine
{
    public Task<EngineResult> RunAsync(DifferentialCase differentialCase, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(differentialCase);
        return Task.FromResult(Run(differentialCase));
    }

    private static EngineResult Run(DifferentialCase differentialCase)
    {
        var issues = new List<IssuePosition>();
        var schemas = CompileSchemas(differentialCase.SchemaPaths, issues, out var rejected);
        if (rejected || issues.Count > 0)
        {
            return new EngineResult(Verdict.Invalid, Verdict.NotRun, issues);
        }

        if (differentialCase.InstancePath is null)
        {
            return new EngineResult(Verdict.Valid, Verdict.NotRun, issues);
        }

        ValidateInstance(differentialCase.InstancePath, schemas, issues);
        return new EngineResult(Verdict.Valid, issues.Count > 0 ? Verdict.Invalid : Verdict.Valid, issues);
    }

    private static XmlSchemaSet CompileSchemas(
        IReadOnlyList<string> paths,
        List<IssuePosition> issues,
        out bool rejected
    )
    {
        rejected = false;
        var schemas = new XmlSchemaSet { XmlResolver = new LocalFileResolver() };
        schemas.ValidationEventHandler += (_, e) => Collect(e, issues);
        try
        {
            foreach (var path in paths)
            {
                using var reader = XmlReader.Create(
                    path,
                    new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore }
                );
                _ = schemas.Add(null, reader);
            }

            schemas.Compile();
        }
        catch (XmlSchemaException ex)
        {
            issues.Add(new IssuePosition(ex.LineNumber, ex.LinePosition));
        }
        catch (XmlException ex)
        {
            issues.Add(new IssuePosition(ex.LineNumber, ex.LinePosition));
        }
        catch (ArgumentException)
        {
            // System.Xml.Schema throws this for some invalid schemas (an empty string for a name); it carries no position.
            rejected = true;
        }

        return schemas;
    }

    private static void ValidateInstance(string path, XmlSchemaSet schemas, List<IssuePosition> issues)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Ignore,
            Schemas = schemas,
            ValidationType = ValidationType.Schema,
        };
        settings.ValidationEventHandler += (_, e) => Collect(e, issues);
        try
        {
            using var reader = XmlReader.Create(path, settings);
            while (reader.Read())
            {
                // The validating reader reports every issue through the event handler.
            }
        }
        catch (XmlException ex)
        {
            issues.Add(new IssuePosition(ex.LineNumber, ex.LinePosition));
        }
    }

    private static void Collect(ValidationEventArgs e, List<IssuePosition> issues)
    {
        if (e.Severity == XmlSeverityType.Error)
        {
            issues.Add(new IssuePosition(e.Exception.LineNumber, e.Exception.LinePosition));
        }
    }
}
