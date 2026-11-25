namespace NetEvolve.Schemas.Xsd.Tests.Unit.DependencyInjection;

using System.Text;

internal static class XsdRegistrationTestSupport
{
    /// <summary>Declares <c>root</c> with integer <c>item</c> children.</summary>
    internal const string RootSchema = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" targetNamespace="urn:root" elementFormDefault="qualified">
          <xs:element name="root">
            <xs:complexType>
              <xs:sequence>
                <xs:element name="item" type="xs:int" maxOccurs="unbounded"/>
              </xs:sequence>
            </xs:complexType>
          </xs:element>
        </xs:schema>
        """;

    /// <summary>Declares <c>other</c> as a boolean.</summary>
    internal const string OtherSchema = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" targetNamespace="urn:other" elementFormDefault="qualified">
          <xs:element name="other" type="xs:boolean"/>
        </xs:schema>
        """;

    /// <summary>Declares <c>root</c> through an include of <c>inc.xsd</c>, which only a resolver can supply.</summary>
    internal const string IncludingSchema = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" targetNamespace="urn:root" elementFormDefault="qualified">
          <xs:include schemaLocation="inc.xsd"/>
        </xs:schema>
        """;

    internal const string IncludedSchema = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" targetNamespace="urn:root" elementFormDefault="qualified">
          <xs:element name="root" type="xs:int"/>
        </xs:schema>
        """;

    internal const string IncludingSystemId = "file:///schemas/main.xsd";

    internal const string IncludedSystemId = "file:///schemas/inc.xsd";

    /// <summary>An XSD 1.1 construct (<c>xs:assert</c>) that strict 1.0 rejects as a schema error.</summary>
    internal const string Xsd11Schema = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" targetNamespace="urn:root" elementFormDefault="qualified">
          <xs:complexType name="c"><xs:sequence/><xs:assert test="true()"/></xs:complexType>
          <xs:element name="root" type="c"/>
        </xs:schema>
        """;

    internal const string Strict10AssertCode = "NES4020";

    internal const string ValidRoot = "<root xmlns=\"urn:root\"><item>1</item><item>2</item></root>";

    internal const string InvalidRoot = "<root xmlns=\"urn:root\"><item>x</item></root>";

    internal const string ValidIncludedRoot = "<root xmlns=\"urn:root\">5</root>";

    internal const string ValidOther = "<other xmlns=\"urn:other\">true</other>";

    internal const string InvalidOther = "<other xmlns=\"urn:other\">maybe</other>";

    internal static bool IsValid(ISchemaValidator validator, string instance) =>
        validator.Validate(instance.AsSpan()).IsValid;

    internal static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);

    internal static DisposeTrackingStream Open(string text) => new(Bytes(text));
}
