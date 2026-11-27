namespace NetEvolve.Schemas.Xsd.Tests.Unit.Declarations11;

using System.Linq;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Assembly;
using XsdVersion = NetEvolve.Schemas.Xml.Datatypes.Contracts.XsdVersion;

/// <summary>The <see langword="ref"/> attribute of <c>key</c>, <c>unique</c>, and <c>keyref</c> is compiled and shares the referenced constraint (XSD-78, #1429).</summary>
public class IdentityConstraintRefWiringTests
{
    private const string Items =
        "<xs:complexType><xs:sequence><xs:element name=\"item\" maxOccurs=\"unbounded\"><xs:complexType><xs:attribute name=\"id\" type=\"xs:string\"/><xs:attribute name=\"to\" type=\"xs:string\"/></xs:complexType></xs:element></xs:sequence></xs:complexType>";

    private static string Schema(string extra = "") =>
        AssemblyFixture.WrapSchema(
            $"""
            <xs:element name="owner">{Items}
              <xs:key name="k"><xs:selector xpath="t:item"/><xs:field xpath="@id"/></xs:key>
              <xs:unique name="u"><xs:selector xpath="t:item"/><xs:field xpath="@to"/></xs:unique>
              <xs:keyref name="kr" refer="t:k"><xs:selector xpath="t:item"/><xs:field xpath="@to"/></xs:keyref>
            </xs:element>
            <xs:element name="user">{Items}
              <xs:key ref="t:k"/>
              <xs:unique ref="t:u"/>
              <xs:keyref ref="t:kr"/>
            </xs:element>
            {extra}
            """
        );

    private static ValidationResult Run(string instance) =>
        AssemblyFixture.Run(AssemblyFixture.Build(Schema()), instance);

    [Test]
    public async Task Validate_ElementThatReusesTheConstraints_AcceptsValuesThatSatisfyThem() =>
        await Assert
            .That(Run("<user xmlns=\"urn:t\"><item id=\"1\" to=\"1\"/><item id=\"2\" to=\"2\"/></user>").IsValid)
            .IsTrue();

    [Test]
    public async Task Validate_ElementThatReusesAUnique_RejectsARepeatedValue()
    {
        var result = Run("<user xmlns=\"urn:t\"><item id=\"1\" to=\"1\"/><item id=\"2\" to=\"1\"/></user>");

        await Assert.That(result.Issues.Any(i => i.Code == XsdRuleCodes.CvcIdentityConstraint)).IsTrue();
    }

    [Test]
    public async Task Validate_ElementThatReusesAKey_RejectsADuplicateKey()
    {
        var result = Run("<user xmlns=\"urn:t\"><item id=\"1\" to=\"1\"/><item id=\"1\" to=\"2\"/></user>");

        await Assert.That(result.Issues.Any(i => i.Code == XsdRuleCodes.CvcIdentityConstraint)).IsTrue();
    }

    [Test]
    public async Task Validate_ElementThatReusesAKeyref_RejectsADanglingReference()
    {
        var result = Run("<user xmlns=\"urn:t\"><item id=\"1\" to=\"9\"/></user>");

        await Assert.That(result.Issues.Any(i => i.Code == XsdRuleCodes.CvcIdentityConstraint)).IsTrue();
    }

    [Test]
    public async Task Validate_TheDeclaringElement_StillEnforcesItsOwnConstraints()
    {
        var result = Run("<owner xmlns=\"urn:t\"><item id=\"1\" to=\"1\"/><item id=\"1\" to=\"2\"/></owner>");

        await Assert.That(result.IsValid).IsFalse();
    }

    [Test]
    public async Task Build_ARefToAConstraintThatIsNotDeclared_IsALocatedSchemaError()
    {
        var exception = AssemblyFixture.Failure(() =>
            AssemblyFixture.Build(
                Schema("<xs:element name=\"e\" type=\"xs:string\"><xs:key ref=\"t:missing\"/></xs:element>")
            )
        );

        var issue = exception.Issues.Single(i => i.Code == XsdRuleCodes.SrcResolve);
        await Assert.That(issue.Location.Line).IsGreaterThan(0);
    }

    [Test]
    public async Task Build_ARefToAConstraintOfAnotherKind_IsALocatedSchemaError()
    {
        var exception = AssemblyFixture.Failure(() =>
            AssemblyFixture.Build(
                Schema("<xs:element name=\"e\" type=\"xs:string\"><xs:unique ref=\"t:k\"/></xs:element>")
            )
        );

        var issue = exception.Issues.Single(i => i.Code == XsdRuleCodes.SrcIdentityConstraint);
        await Assert.That(issue.Location.Line).IsGreaterThan(0);
    }

    [Test]
    public async Task Build_ARefTogetherWithASelectorAndAField_IsALocatedSchemaError()
    {
        var exception = AssemblyFixture.Failure(() =>
            AssemblyFixture.Build(
                Schema(
                    "<xs:element name=\"e\" type=\"xs:string\"><xs:key ref=\"t:k\"><xs:selector xpath=\".\"/><xs:field xpath=\".\"/></xs:key></xs:element>"
                )
            )
        );

        var issue = exception.Issues.Single(i => i.Code == XsdRuleCodes.SrcIdentityConstraint);
        await Assert.That(issue.Constraint).IsEqualTo("src-identity-constraint.4");
        await Assert.That(issue.Location.Line).IsGreaterThan(0);
    }

    [Test]
    public async Task Build_Version10_RejectsTheRefWithoutTheNotYetSupportedCode()
    {
        var exception = AssemblyFixture.Failure(() => AssemblyFixture.Build(Schema(), XsdVersion.V10));

        await Assert.That(exception.Issues.Count).IsGreaterThan(0);
        await Assert.That(exception.Issues.Any(i => i.Code.Number == 4074)).IsFalse();
    }
}
