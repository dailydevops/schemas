namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Xsi;

using System;
using NetEvolve.Schemas.Xml.Text.NameTable;
using NetEvolve.Schemas.Xsd.Validation.Xsi;

public class XsiNamespaceScopeTests
{
    private static (XsiNamespaceScope Scope, SpanInterningNameTable Names) Create()
    {
        var names = new SpanInterningNameTable(1000);
        return (new XsiNamespaceScope(names), names);
    }

    private static string? Resolve(XsiNamespaceScope scope, string prefix) =>
        scope.TryResolvePrefix(prefix.AsSpan(), out var uri) ? uri : null;

    [Test]
    public async Task Prefix_ThatIsDeclared_ResolvesToItsNamespace_AndAnotherDoesNot()
    {
        var (scope, names) = Create();

        scope.Declare(names.Intern("p".AsSpan()), names.Intern("urn:p".AsSpan()));

        await Assert.That(Resolve(scope, "p")).IsEqualTo("urn:p");
        await Assert.That(Resolve(scope, "q")).IsNull();
        await Assert.That(Resolve(scope, string.Empty)).IsNull();
        await Assert.That(scope.Count).IsEqualTo(1);
    }

    [Test]
    public async Task LaterDeclaration_ShadowsAnEarlierOne_UntilItIsCutBack()
    {
        var (scope, names) = Create();
        scope.Declare(names.Intern("p".AsSpan()), names.Intern("urn:one".AsSpan()));
        var mark = scope.Count;

        scope.Declare(names.Intern("p".AsSpan()), names.Intern("urn:two".AsSpan()));
        var shadowed = Resolve(scope, "p");
        scope.Truncate(mark);

        await Assert.That(shadowed).IsEqualTo("urn:two");
        await Assert.That(Resolve(scope, "p")).IsEqualTo("urn:one");
    }

    [Test]
    public async Task DefaultNamespace_CanBeDeclaredAndUndeclared()
    {
        var (scope, names) = Create();
        scope.Declare(default, names.Intern("urn:d".AsSpan()));
        var declared = Resolve(scope, string.Empty);

        scope.Declare(default, default);

        await Assert.That(declared).IsEqualTo("urn:d");
        await Assert.That(Resolve(scope, string.Empty)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task ManyDeclarations_GrowTheStack()
    {
        var (scope, names) = Create();

        for (var i = 0; i < 40; i++)
        {
            scope.Declare(names.Intern(("p" + i).AsSpan()), names.Intern(("urn:" + i).AsSpan()));
        }

        await Assert.That(scope.Count).IsEqualTo(40);
        await Assert.That(Resolve(scope, "p0")).IsEqualTo("urn:0");
        await Assert.That(Resolve(scope, "p39")).IsEqualTo("urn:39");

        scope.Truncate(0);

        await Assert.That(Resolve(scope, "p0")).IsNull();
    }

    [Test]
    public async Task XmlPrefix_IsAlwaysBound()
    {
        var (scope, _) = Create();

        await Assert.That(Resolve(scope, "xml")).IsEqualTo("http://www.w3.org/XML/1998/namespace");
    }

    [Test]
    public async Task Scope_NullNames_Throws() =>
        await Assert.That(() => new XsiNamespaceScope(null!)).Throws<ArgumentNullException>();

    [Test]
    public async Task Truncate_BelowZero_Throws()
    {
        var (scope, _) = Create();

        await Assert.That(() => scope.Truncate(-1)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Truncate_PastTheCount_Throws()
    {
        var (scope, _) = Create();

        await Assert.That(() => scope.Truncate(1)).Throws<ArgumentOutOfRangeException>();
    }
}
