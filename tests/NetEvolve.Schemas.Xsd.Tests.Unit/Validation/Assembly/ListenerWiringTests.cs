namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Assembly;

using System.Linq;
using System.Threading.Tasks;

/// <summary>The identity constraint, ID, and entity listeners are wired behind the builder (design #146 DP-12).</summary>
public class ListenerWiringTests
{
    private static readonly string Constraints = AssemblyFixture.WrapSchema(
        """
        <xs:element name="root">
          <xs:complexType>
            <xs:sequence>
              <xs:element name="item" maxOccurs="unbounded">
                <xs:complexType><xs:attribute name="id" type="xs:string" use="required"/></xs:complexType>
              </xs:element>
              <xs:element name="sec" maxOccurs="unbounded">
                <xs:complexType>
                  <xs:sequence>
                    <xs:element name="entry" maxOccurs="unbounded">
                      <xs:complexType><xs:attribute name="n" type="xs:string"/></xs:complexType>
                    </xs:element>
                  </xs:sequence>
                </xs:complexType>
                <xs:unique name="uSec"><xs:selector xpath="t:entry"/><xs:field xpath="@n"/></xs:unique>
              </xs:element>
            </xs:sequence>
          </xs:complexType>
          <xs:key name="kItem"><xs:selector xpath="t:item"/><xs:field xpath="@id"/></xs:key>
          <xs:keyref name="rItem" refer="t:kItem"><xs:selector xpath="t:sec/t:entry"/><xs:field xpath="@n"/></xs:keyref>
        </xs:element>
        """
    );

    private static readonly string Identifiers = AssemblyFixture.WrapSchema(
        """
        <xs:element name="root">
          <xs:complexType>
            <xs:choice maxOccurs="unbounded">
              <xs:element name="e"><xs:complexType><xs:attribute name="id" type="xs:ID"/></xs:complexType></xs:element>
              <xs:element name="r"><xs:complexType><xs:attribute name="ref" type="xs:IDREF"/></xs:complexType></xs:element>
            </xs:choice>
          </xs:complexType>
        </xs:element>
        """
    );

    private static readonly string Entities = AssemblyFixture.WrapSchema(
        """
        <xs:element name="root">
          <xs:complexType><xs:attribute name="ent" type="xs:ENTITY"/></xs:complexType>
        </xs:element>
        """
    );

    private const string Declarations =
        "<!DOCTYPE root [<!NOTATION gif SYSTEM \"image/gif\"><!ENTITY pic SYSTEM \"pic.gif\" NDATA gif>]>";

    [Test]
    public async Task IdentityConstraints_ValidDocument_IsValid()
    {
        var result = Run(
            Constraints,
            "<root xmlns=\"urn:t\"><item id=\"a\"/><item id=\"b\"/><sec><entry n=\"a\"/><entry n=\"b\"/></sec><sec><entry n=\"a\"/></sec></root>"
        );

        await Assert.That(result.Issues).IsEmpty();
    }

    [Test]
    public async Task IdentityConstraints_DuplicateKeyOfTheGlobalElement_IsReported()
    {
        var result = Run(
            Constraints,
            "<root xmlns=\"urn:t\"><item id=\"a\"/><item id=\"a\"/><sec><entry n=\"a\"/></sec></root>"
        );

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Issues.Count(i => i.Code == XsdRuleCodes.CvcIdentityConstraint)).IsEqualTo(1);
    }

    [Test]
    public async Task IdentityConstraints_DuplicateUniqueValueInsideALocalElement_IsReported()
    {
        var result = Run(
            Constraints,
            "<root xmlns=\"urn:t\"><item id=\"a\"/><sec><entry n=\"a\"/><entry n=\"a\"/></sec></root>"
        );

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Issues.Any(i => i.Code == XsdRuleCodes.CvcIdentityConstraint)).IsTrue();
    }

    [Test]
    public async Task IdentityConstraints_DanglingKeyref_IsReported()
    {
        var result = Run(Constraints, "<root xmlns=\"urn:t\"><item id=\"a\"/><sec><entry n=\"z\"/></sec></root>");

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Issues.Any(i => i.Code == XsdRuleCodes.CvcIdentityConstraint)).IsTrue();
    }

    [Test]
    public async Task IdentityConstraints_TwoRunsOfOneValidator_DoNotShareState()
    {
        var validator = AssemblyFixture.Build(Constraints);
        var duplicate = "<root xmlns=\"urn:t\"><item id=\"a\"/><item id=\"a\"/><sec><entry n=\"a\"/></sec></root>";
        var valid = "<root xmlns=\"urn:t\"><item id=\"a\"/><sec><entry n=\"a\"/></sec></root>";

        var first = AssemblyFixture.Run(validator, duplicate);
        var second = AssemblyFixture.Run(validator, valid);
        var third = AssemblyFixture.Run(validator, duplicate);

        await Assert.That(first.IsValid).IsFalse();
        await Assert.That(second.IsValid).IsTrue();
        await Assert.That(AssemblyFixture.Same(third, first)).IsTrue();
    }

    [Test]
    public async Task Ids_DuplicateIdAndDanglingIdref_AreReported()
    {
        var valid = Run(Identifiers, "<root xmlns=\"urn:t\"><e id=\"a\"/><r ref=\"a\"/></root>");
        var duplicate = Run(Identifiers, "<root xmlns=\"urn:t\"><e id=\"a\"/><e id=\"a\"/></root>");
        var dangling = Run(Identifiers, "<root xmlns=\"urn:t\"><e id=\"a\"/><r ref=\"b\"/></root>");

        await Assert.That(valid.IsValid).IsTrue();
        await Assert.That(duplicate.Issues.Any(i => i.Code == XsdRuleCodes.CvcId)).IsTrue();
        await Assert.That(dangling.Issues.Any(i => i.Code == XsdRuleCodes.CvcId)).IsTrue();
    }

    [Test]
    public async Task Entities_AgainstTheUnparsedEntitiesOfTheDoctype()
    {
        var declared = Run(Entities, Declarations + "<root xmlns=\"urn:t\" ent=\"pic\"/>", dtd: true);
        var undeclared = Run(Entities, Declarations + "<root xmlns=\"urn:t\" ent=\"nope\"/>", dtd: true);
        var noDeclarations = Run(Entities, "<root xmlns=\"urn:t\" ent=\"pic\"/>", dtd: true);

        await Assert.That(declared.IsValid).IsTrue();
        await Assert.That(undeclared.IsValid).IsFalse();
        await Assert.That(noDeclarations.IsValid).IsFalse();
    }

    private static ValidationResult Run(string schema, string instance, bool dtd = false) =>
        AssemblyFixture.Run(schema, instance, dtd);
}
