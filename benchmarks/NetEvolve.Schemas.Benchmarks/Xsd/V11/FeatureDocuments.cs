namespace NetEvolve.Schemas.Benchmarks.Xsd.V11;

using System.Globalization;
using System.Text;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>
/// The inline schemas of the assertion facet (Structures 3.13), open content (Structures 3.4.2.3), and type alternatives (Structures 3.12)
/// and their generated instances. The instances are built in memory, so the benchmarks are deterministic and need no network.
/// </summary>
internal static class FeatureDocuments
{
    /// <summary>The number of repeated children of every generated instance.</summary>
    public const int Count = 1000;

    /// <summary>A simple type with an assertion facet that every <c>n</c> element is checked against.</summary>
    public const string FacetSchema = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" elementFormDefault="qualified">
          <xs:simpleType name="Even">
            <xs:restriction base="xs:integer">
              <xs:assertion test="$value mod 2 = 0"/>
            </xs:restriction>
          </xs:simpleType>
          <xs:element name="list">
            <xs:complexType>
              <xs:sequence>
                <xs:element name="n" type="Even" maxOccurs="unbounded"/>
              </xs:sequence>
            </xs:complexType>
          </xs:element>
        </xs:schema>
        """;

    /// <summary>A sequence with interleave open content that takes foreign elements between and after the members.</summary>
    public const string OpenContentSchema = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" elementFormDefault="qualified">
          <xs:element name="doc">
            <xs:complexType>
              <xs:openContent mode="interleave">
                <xs:any namespace="##other" processContents="lax"/>
              </xs:openContent>
              <xs:sequence>
                <xs:element name="head" type="xs:string"/>
                <xs:element name="body" type="xs:string" maxOccurs="unbounded"/>
              </xs:sequence>
            </xs:complexType>
          </xs:element>
        </xs:schema>
        """;

    /// <summary>An element whose type is selected per instance from its <c>kind</c> attribute by three type alternatives; any other kind keeps the declared type.</summary>
    public const string AlternativeSchema = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" elementFormDefault="qualified">
          <xs:complexType name="Value">
            <xs:simpleContent>
              <xs:extension base="xs:string">
                <xs:attribute name="kind" type="xs:string"/>
              </xs:extension>
            </xs:simpleContent>
          </xs:complexType>
          <xs:complexType name="IntValue">
            <xs:simpleContent>
              <xs:restriction base="Value">
                <xs:pattern value="[0-9]+"/>
              </xs:restriction>
            </xs:simpleContent>
          </xs:complexType>
          <xs:complexType name="DateValue">
            <xs:simpleContent>
              <xs:restriction base="Value">
                <xs:pattern value="[0-9]{4}-[0-9]{2}-[0-9]{2}"/>
              </xs:restriction>
            </xs:simpleContent>
          </xs:complexType>
          <xs:complexType name="FlagValue">
            <xs:simpleContent>
              <xs:restriction base="Value">
                <xs:enumeration value="true"/>
                <xs:enumeration value="false"/>
              </xs:restriction>
            </xs:simpleContent>
          </xs:complexType>
          <xs:element name="list">
            <xs:complexType>
              <xs:sequence>
                <xs:element name="v" type="Value" maxOccurs="unbounded">
                  <xs:alternative test="@kind = 'int'" type="IntValue"/>
                  <xs:alternative test="@kind = 'date'" type="DateValue"/>
                  <xs:alternative test="@kind = 'flag'" type="FlagValue"/>
                </xs:element>
              </xs:sequence>
            </xs:complexType>
          </xs:element>
        </xs:schema>
        """;

    /// <summary>Creates a list of even numbers; with <paramref name="broken"/> the last one is odd.</summary>
    /// <param name="broken">Whether the last number violates the assertion facet.</param>
    /// <returns>The instance, UTF-8.</returns>
    public static byte[] Facet(bool broken = false) =>
        Build("list", Count, i => $"<n>{((i == Count - 1 && broken) ? i * 2 + 1 : i * 2)}</n>");

    /// <summary>Creates a document with a head, <see cref="Count"/> bodies, and a foreign element before every body and at the end.</summary>
    /// <param name="broken">Whether the head is missing, so the content model fails.</param>
    /// <returns>The instance, UTF-8.</returns>
    public static byte[] OpenContent(bool broken = false) =>
        Build(
            "doc xmlns:o=\"urn:benchmark:other\"",
            Count,
            i => (i == 0 && !broken ? "<head>h</head>" : "") + "<o:x>1</o:x><body>b</body>",
            "<o:x>1</o:x>"
        );

    /// <summary>Creates a list of typed values; with <paramref name="broken"/> the last integer value is not a number.</summary>
    /// <param name="broken">Whether the last value violates its selected type.</param>
    /// <returns>The instance, UTF-8.</returns>
    public static byte[] Alternatives(bool broken = false) =>
        Build("list", Count, i =>
            (i % 4) switch
            {
                0 =>
                    $"<v kind=\"int\">{(i == Count - 4 && broken ? "x" : i.ToString(CultureInfo.InvariantCulture))}</v>",
                1 => "<v kind=\"date\">2024-02-29</v>",
                2 => "<v kind=\"flag\">true</v>",
                _ => "<v>text</v>",
            }
        );

    private static byte[] Build(string root, int count, Func<int, string> child, string tail = "")
    {
        var sb = new StringBuilder("<").Append(root).Append('>');
        for (var i = 0; i < count; i++)
        {
            _ = sb.Append(child(i));
        }

        var name = root.Split(' ')[0];
        return Encoding.UTF8.GetBytes(sb.Append(tail).Append("</").Append(name).Append('>').ToString());
    }

    /// <summary>Compiles the schema in 1.1 mode and proves that the feature runs: the valid instance passes and the broken one fails.</summary>
    /// <param name="schema">The schema text.</param>
    /// <param name="valid">A valid instance.</param>
    /// <param name="invalid">An instance that violates the feature.</param>
    /// <returns>The validator.</returns>
    public static IXsdValidator Prove(string schema, byte[] valid, byte[] invalid) =>
        AssertionDocuments.Prove(schema, valid, invalid);
}
