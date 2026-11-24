namespace NetEvolve.Schemas.Benchmarks.Xml.Regex;

using System.Xml;
using System.Xml.Schema;

/// <summary>The baseline of the regex benchmarks: pattern facet validation through <c>System.Xml.Schema</c>.</summary>
internal sealed class SystemXmlPatternValidation
{
    private static readonly XmlQualifiedName StringType = new("string", XmlSchema.Namespace);
    private readonly NameTable _names = new();
    private readonly XmlSchemaDatatype[] _types;

    /// <summary>Initializes a new instance of the <see cref="SystemXmlPatternValidation"/> class and compiles the patterns.</summary>
    /// <param name="patterns">The patterns, in XSD syntax.</param>
    public SystemXmlPatternValidation(string[] patterns) => _types = Compile(patterns);

    /// <summary>Compiles the patterns as pattern facets of restrictions of <c>xs:string</c>.</summary>
    /// <param name="patterns">The patterns, in XSD syntax.</param>
    /// <returns>The data type of each pattern, in order.</returns>
    public static XmlSchemaDatatype[] Compile(string[] patterns)
    {
        var schema = new XmlSchema();
        for (var i = 0; i < patterns.Length; i++)
        {
            var restriction = new XmlSchemaSimpleTypeRestriction { BaseTypeName = StringType };
            _ = restriction.Facets.Add(new XmlSchemaPatternFacet { Value = patterns[i] });
            _ = schema.Items.Add(new XmlSchemaSimpleType { Name = "t" + i, Content = restriction });
        }

        var set = new XmlSchemaSet();
        _ = set.Add(schema);
        set.Compile();

        var types = new XmlSchemaDatatype[patterns.Length];
        for (var i = 0; i < types.Length; i++)
        {
            types[i] = ((XmlSchemaSimpleType)schema.SchemaTypes[new XmlQualifiedName("t" + i)]!).Datatype!;
        }

        return types;
    }

    /// <summary>Validates each input against its pattern; an input the pattern rejects throws.</summary>
    /// <param name="inputs">The inputs; index <c>i</c> belongs to pattern <c>i</c>.</param>
    /// <returns>The number of accepted inputs.</returns>
    public int Validate(string[] inputs)
    {
        var accepted = 0;
        for (var i = 0; i < inputs.Length; i++)
        {
            _ = _types[i].ParseValue(inputs[i], _names, null);
            accepted++;
        }

        return accepted;
    }
}
