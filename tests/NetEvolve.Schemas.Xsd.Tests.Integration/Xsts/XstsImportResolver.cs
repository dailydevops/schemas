namespace NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

using System.Text;
using NetEvolve.Schemas.Resources;

/// <summary>
/// Supplies the XLink and the XML namespace schema documents that <c>common/xsts.xsd</c> imports through absolute <c>http</c>
/// locations, and leaves every other request to the inner resolver (the sandbox of the suite root).
/// </summary>
/// <remarks>
/// The suite does not ship the two documents. The runner never reaches the network, so it serves the declarations that the suite
/// schemas use from memory: the XLink attributes and the <c>xml:lang</c>, <c>xml:space</c>, <c>xml:base</c> and <c>xml:id</c> attributes.
/// </remarks>
internal sealed class XstsImportResolver : IResourceResolver
{
    private const string XlinkLocation = "http://www.w3.org/XML/2008/06/xlink.xsd";
    private const string XmlLocation = "http://www.w3.org/2001/xml.xsd";

    private const string Xlink = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" targetNamespace="http://www.w3.org/1999/xlink" xmlns:xlink="http://www.w3.org/1999/xlink">
          <xs:attribute name="type">
            <xs:simpleType>
              <xs:restriction base="xs:token">
                <xs:enumeration value="simple"/>
                <xs:enumeration value="extended"/>
                <xs:enumeration value="title"/>
                <xs:enumeration value="resource"/>
                <xs:enumeration value="locator"/>
                <xs:enumeration value="arc"/>
              </xs:restriction>
            </xs:simpleType>
          </xs:attribute>
          <xs:attribute name="href" type="xs:anyURI"/>
          <xs:attribute name="role" type="xs:anyURI"/>
          <xs:attribute name="arcrole" type="xs:anyURI"/>
          <xs:attribute name="title" type="xs:string"/>
          <xs:attribute name="label" type="xs:NCName"/>
          <xs:attribute name="from" type="xs:NCName"/>
          <xs:attribute name="to" type="xs:NCName"/>
        </xs:schema>
        """;

    private const string Xml = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" targetNamespace="http://www.w3.org/XML/1998/namespace">
          <xs:attribute name="lang">
            <xs:simpleType>
              <xs:union memberTypes="xs:language">
                <xs:simpleType>
                  <xs:restriction base="xs:string">
                    <xs:enumeration value=""/>
                  </xs:restriction>
                </xs:simpleType>
              </xs:union>
            </xs:simpleType>
          </xs:attribute>
          <xs:attribute name="space">
            <xs:simpleType>
              <xs:restriction base="xs:NCName">
                <xs:enumeration value="default"/>
                <xs:enumeration value="preserve"/>
              </xs:restriction>
            </xs:simpleType>
          </xs:attribute>
          <xs:attribute name="base" type="xs:anyURI"/>
          <xs:attribute name="id" type="xs:ID"/>
        </xs:schema>
        """;

    private readonly IResourceResolver _inner;

    public XstsImportResolver(IResourceResolver inner)
    {
        ArgumentNullException.ThrowIfNull(inner);

        _inner = inner;
    }

    public ResourceResult Resolve(in ResourceRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var address = request.ResolvedUri ?? request.Reference;
        var text = address switch
        {
            XlinkLocation => Xlink,
            XmlLocation => Xml,
            _ => null,
        };

        return text is null
            ? _inner.Resolve(in request, cancellationToken)
            : ResourceResult.Found(new MemoryStream(Encoding.UTF8.GetBytes(text)), address);
    }
}
