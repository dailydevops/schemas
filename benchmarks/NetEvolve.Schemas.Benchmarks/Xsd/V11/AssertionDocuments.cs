namespace NetEvolve.Schemas.Benchmarks.Xsd.V11;

using System.Globalization;
using System.Text;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>
/// The schemas with complex type assertions (<c>xs:assert</c>, Structures 3.13) and their generated instances. Both schemas are inline
/// and the instances are built in memory, so the benchmarks are deterministic and need no network.
/// </summary>
internal static class AssertionDocuments
{
    /// <summary>
    /// A document with ten asserted lines (a subtree of three children each) followed by a list of plain entries. Only the entries
    /// grow, so the number and the size of the asserted subtrees do not depend on the document size.
    /// </summary>
    public const string LineSchema = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" elementFormDefault="qualified">
          <xs:element name="order">
            <xs:complexType>
              <xs:sequence>
                <xs:element name="line" type="Line" maxOccurs="unbounded"/>
                <xs:element name="entry" type="xs:int" minOccurs="0" maxOccurs="unbounded"/>
              </xs:sequence>
            </xs:complexType>
          </xs:element>
          <xs:complexType name="Line">
            <xs:sequence>
              <xs:element name="quantity" type="xs:integer"/>
              <xs:element name="price" type="xs:decimal"/>
              <xs:element name="total" type="xs:decimal"/>
            </xs:sequence>
            <xs:assert test="total = quantity * price"/>
          </xs:complexType>
        </xs:schema>
        """;

    /// <summary>One asserted element whose subtree is the whole list of items, so the buffered subtree grows with the item count.</summary>
    public const string BundleSchema = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" elementFormDefault="qualified">
          <xs:element name="bundle">
            <xs:complexType>
              <xs:sequence>
                <xs:element name="item" type="xs:integer" maxOccurs="unbounded"/>
              </xs:sequence>
              <xs:assert test="count(item) gt 0 and sum(item) ge 0"/>
            </xs:complexType>
          </xs:element>
        </xs:schema>
        """;

    /// <summary>The number of asserted lines of every order.</summary>
    public const int Lines = 10;

    /// <summary>Creates an order of ten valid lines and <paramref name="entries"/> plain entries.</summary>
    /// <param name="entries">The number of plain entries.</param>
    /// <returns>The instance, UTF-8.</returns>
    public static byte[] Order(int entries) => Order(entries, brokenLine: -1);

    /// <summary>Creates an order in which the line at <paramref name="brokenLine"/> violates the assertion.</summary>
    /// <param name="entries">The number of plain entries.</param>
    /// <param name="brokenLine">The zero-based index of the violating line, or -1 for none.</param>
    /// <returns>The instance, UTF-8.</returns>
    public static byte[] Order(int entries, int brokenLine)
    {
        var sb = new StringBuilder("<order>");
        for (var i = 0; i < Lines; i++)
        {
            var quantity = i + 1;
            var price = (i * 7) + 1;
            var total = (quantity * price) + (i == brokenLine ? 1 : 0);
            _ = sb.Append(
                CultureInfo.InvariantCulture,
                $"<line><quantity>{quantity}</quantity><price>{price}</price><total>{total}</total></line>"
            );
        }

        for (var i = 0; i < entries; i++)
        {
            _ = sb.Append(CultureInfo.InvariantCulture, $"<entry>{i}</entry>");
        }

        return Encoding.UTF8.GetBytes(sb.Append("</order>").ToString());
    }

    /// <summary>Creates a bundle of <paramref name="items"/> non-negative items.</summary>
    /// <param name="items">The number of items.</param>
    /// <returns>The instance, UTF-8.</returns>
    public static byte[] Bundle(int items)
    {
        var sb = new StringBuilder("<bundle>");
        for (var i = 0; i < items; i++)
        {
            _ = sb.Append(CultureInfo.InvariantCulture, $"<item>{i % 100}</item>");
        }

        return Encoding.UTF8.GetBytes(sb.Append("</bundle>").ToString());
    }

    /// <summary>A bundle whose sum is negative, so the assertion of <see cref="BundleSchema"/> fails.</summary>
    public static byte[] BrokenBundle { get; } = Encoding.UTF8.GetBytes("<bundle><item>-5</item></bundle>");

    /// <summary>Compiles the schema and proves that the assertions run: the valid instance passes and the broken one fails.</summary>
    /// <param name="schema">The schema text.</param>
    /// <param name="valid">A valid instance.</param>
    /// <param name="invalid">An instance that violates the assertion.</param>
    /// <returns>The validator.</returns>
    public static IXsdValidator Prove(string schema, byte[] valid, byte[] invalid)
    {
        var validator = Xsd11Validators.Inline(schema, XsdVersion.V11);
        var accepted = validator.Validate(valid);
        if (!accepted.IsValid)
        {
            throw new InvalidOperationException(
                "The valid instance of the benchmark is rejected: " + accepted.Issues[0].Message
            );
        }

        if (validator.Validate(invalid).IsValid)
        {
            throw new InvalidOperationException(
                "The assertion of the benchmark did not run: the broken instance is valid."
            );
        }

        return validator;
    }
}
