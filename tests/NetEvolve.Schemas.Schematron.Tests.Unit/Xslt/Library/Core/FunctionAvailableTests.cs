namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Library.Core;

using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Xslt.Library.Core;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Library.Core.CoreFixture;

/// <summary>
/// <c>function-available()</c> (XSLT 2.0, 18.1.1): the two-argument form is true when a function of that name and arity is in scope,
/// the one-argument form when one of some arity is; an unprefixed name is in the function namespace; XTDE1400 for a name that is not
/// a lexical QName or whose prefix is not bound. A function is in scope when the XPath catalog under the active host flags, the
/// core library, or the stylesheet function table has it.
/// </summary>
public class FunctionAvailableTests
{
    private static XsltFunctionScope WithUser() => Scope(user: Table("gln/1", "mod/2", "wide/40"));

    private static string Boolean(bool value) => value ? "boolean:true" : "boolean:false";

    [Test]
    [Arguments("string-length", 1, true)]
    [Arguments("string-length", 0, true)]
    [Arguments("string-length", 2, false)]
    [Arguments("concat", 2, true)]
    [Arguments("concat", 9, true)]
    [Arguments("concat", 1, false)]
    [Arguments("insert-before", 3, true)]
    [Arguments("insert-before", 2, false)]
    [Arguments("fn:count", 1, true)]
    [Arguments("xs:integer", 1, true)]
    [Arguments("xs:integer", 2, false)]
    public async Task FunctionAvailable_ForABuiltInFunction_IsTrueOnlyForAMatchingArity(
        string name,
        int arity,
        bool expected
    ) => await Assert.That(Run(WithUser(), "function-available", S(name), I(arity))).IsEqualTo(Boolean(expected));

    [Test]
    [Arguments("f:gln", 1, true)]
    [Arguments("f:gln", 2, false)]
    [Arguments("f:gln", 0, false)]
    [Arguments("f:mod", 2, true)]
    [Arguments("f:mod", 1, false)]
    [Arguments("f:wide", 40, true)]
    public async Task FunctionAvailable_ForAUserFunction_IsTrueOnlyForAMatchingArity(
        string name,
        int arity,
        bool expected
    ) => await Assert.That(Run(WithUser(), "function-available", S(name), I(arity))).IsEqualTo(Boolean(expected));

    [Test]
    [Arguments("f:unknown", 1)]
    [Arguments("unknown", 1)]
    [Arguments("unknown", 0)]
    [Arguments("fn:unknown", 2)]
    [Arguments("gln", 1)]
    [Arguments("string-length", -1)]
    [Arguments("string-length", 4294967297)]
    public async Task FunctionAvailable_ForAnUnknownFunction_IsFalse(string name, long arity) =>
        await Assert.That(Run(WithUser(), "function-available", S(name), I(arity))).IsEqualTo("boolean:false");

    [Test]
    [Arguments("current", 0)]
    [Arguments("system-property", 1)]
    [Arguments("function-available", 1)]
    [Arguments("function-available", 2)]
    [Arguments("generate-id", 0)]
    [Arguments("generate-id", 1)]
    public async Task FunctionAvailable_ForTheXsltFunctions_IsTrueUnderAnXsltBinding(string name, int arity) =>
        await Assert.That(Run(Scope("xslt2"), "function-available", S(name), I(arity))).IsEqualTo("boolean:true");

    [Test]
    [Arguments("string-length", true)]
    [Arguments("f:gln", true)]
    [Arguments("f:wide", true)]
    [Arguments("concat", true)]
    [Arguments("insert-before", true)]
    [Arguments("current", true)]
    [Arguments("generate-id", true)]
    [Arguments("f:unknown", false)]
    [Arguments("unknown", false)]
    public async Task FunctionAvailable_OneArgumentForm_IsTrueWhenSomeArityMatches(string name, bool expected) =>
        await Assert.That(Run(WithUser(), "function-available", S(name))).IsEqualTo(Boolean(expected));

    [Test]
    [Arguments("")]
    [Arguments("1x")]
    [Arguments("a:b:c")]
    [Arguments("zz:name")]
    [Arguments(" string-length")]
    public async Task FunctionAvailable_ForAnInvalidNameOrAnUnboundPrefix_IsXtde1400(string name)
    {
        await Assert.That(Run(WithUser(), "function-available", S(name), I(1))).IsEqualTo("error:XTDE1400");
        await Assert.That(Run(WithUser(), "function-available", S(name))).IsEqualTo("error:XTDE1400");
    }

    [Test]
    public async Task FunctionAvailable_IsNotFoundUnderAnXpathBinding() =>
        await Assert.That(Run(Scope("xpath2"), "function-available", S("string-length"), I(1))).IsEqualTo("notfound");

    [Test]
    public async Task FunctionAvailable_UnderAnXpathBinding_DoesNotSeeTheXsltFunctions()
    {
        var scope = Scope("xpath2");

        await Assert.That(scope.IsAvailable(Fn("current"), 0)).IsFalse();
        await Assert.That(scope.IsAvailable(Fn("generate-id"), 1)).IsFalse();
        await Assert.That(scope.IsAvailable(Fn("string-length"), 1)).IsTrue();
    }
}
