namespace NetEvolve.Schemas.Schematron.Svrl;

using NetEvolve.Schemas.Xml.Contracts;

/// <summary>The SVRL element and attribute names, interned once per output into the name table of its writer.</summary>
internal sealed class SvrlNames
{
    internal const string Namespace = "http://purl.oclc.org/dsdl/svrl";
    internal const string Prefix = "svrl";

    internal SvrlNames(XmlNameTable table)
    {
        PrefixSymbol = table.Intern(Prefix);
        NamespaceSymbol = table.Intern(Namespace);

        Output = Element(table, "schematron-output");
        NamespaceReport = Element(table, "ns-prefix-in-attribute-values");
        ActivePattern = Element(table, "active-pattern");
        ActiveGroup = Element(table, "active-group");
        FiredRule = Element(table, "fired-rule");
        FailedAssert = Element(table, "failed-assert");
        SuccessfulReport = Element(table, "successful-report");
        DiagnosticReference = Element(table, "diagnostic-reference");
        PropertyReference = Element(table, "property-reference");
        Error = Element(table, "error");
        Text = Element(table, "text");

        Title = Attribute(table, "title");
        Phase = Attribute(table, "phase");
        SchemaVersion = Attribute(table, "schemaVersion");
        SchematronEdition = Attribute(table, "schematronEdition");
        PrefixAttribute = Attribute(table, "prefix");
        Uri = Attribute(table, "uri");
        Id = Attribute(table, "id");
        Documents = Attribute(table, "documents");
        Name = Attribute(table, "name");
        Role = Attribute(table, "role");
        Scheme = Attribute(table, "scheme");
        Fpi = Attribute(table, "fpi");
        Icon = Attribute(table, "icon");
        See = Attribute(table, "see");
        Context = Attribute(table, "context");
        Flag = Attribute(table, "flag");
        Document = Attribute(table, "document");
        Location = Attribute(table, "location");
        Test = Attribute(table, "test");
        Severity = Attribute(table, "severity");
        Diagnostic = Attribute(table, "diagnostic");
        Property = Attribute(table, "property");
        Code = Attribute(table, "code");
        Lang = table.Intern("xml", "lang", "http://www.w3.org/XML/1998/namespace");
    }

    internal XmlSymbol PrefixSymbol { get; }

    internal XmlSymbol NamespaceSymbol { get; }

    internal XmlName Output { get; }

    internal XmlName NamespaceReport { get; }

    internal XmlName ActivePattern { get; }

    internal XmlName ActiveGroup { get; }

    internal XmlName FiredRule { get; }

    internal XmlName FailedAssert { get; }

    internal XmlName SuccessfulReport { get; }

    internal XmlName DiagnosticReference { get; }

    internal XmlName PropertyReference { get; }

    internal XmlName Error { get; }

    internal XmlName Text { get; }

    internal XmlName Title { get; }

    internal XmlName Phase { get; }

    internal XmlName SchemaVersion { get; }

    internal XmlName SchematronEdition { get; }

    internal XmlName PrefixAttribute { get; }

    internal XmlName Uri { get; }

    internal XmlName Id { get; }

    internal XmlName Documents { get; }

    internal XmlName Name { get; }

    internal XmlName Role { get; }

    internal XmlName Scheme { get; }

    internal XmlName Fpi { get; }

    internal XmlName Icon { get; }

    internal XmlName See { get; }

    internal XmlName Context { get; }

    internal XmlName Flag { get; }

    internal XmlName Document { get; }

    internal XmlName Location { get; }

    internal XmlName Test { get; }

    internal XmlName Severity { get; }

    internal XmlName Diagnostic { get; }

    internal XmlName Property { get; }

    internal XmlName Code { get; }

    internal XmlName Lang { get; }

    private static XmlName Element(XmlNameTable table, string local) => table.Intern(Prefix, local, Namespace);

    private static XmlName Attribute(XmlNameTable table, string local) =>
        table.Intern(string.Empty, local, string.Empty);
}
