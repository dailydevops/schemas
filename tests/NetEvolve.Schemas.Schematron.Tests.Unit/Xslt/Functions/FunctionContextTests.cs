namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// The additive members of <see cref="XsltCompileContext"/> that the function instructions need (design #150 EP-9, EP-12): the
/// binding that compiles the expressions of an instruction, and a way to compile a child in a scope that the parent extended with
/// a parameter or a local variable.
/// </summary>
public class FunctionContextTests
{
    private static readonly QueryStaticScope Scope = new([], [], null, SchematronEdition.Iso2016);

    [Test]
    public async Task Context_WithoutTheAdditiveMembers_HasNoBindingAndCannotCompileInAScope()
    {
        var context = new XsltCompileContext(default, Scope, static _ => null, new FakeProblemSink());

        await Assert.That(context.Binding).IsNull();
        await Assert.That(() => context.CompileChildIn(default, Scope)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Context_CompileChildIn_PassesTheChildAndTheScopeToTheCallback()
    {
        var inner = new QueryStaticScope([], ["x"], null, SchematronEdition.Iso2016);
        QueryStaticScope? seen = null;
        var executable = new FakeXsltExecutable();
        var binding = new FakeQueryBinding();
        var context = new XsltCompileContext(
            default,
            Scope,
            static _ => null,
            new FakeProblemSink(),
            binding,
            (child, scope) =>
            {
                seen = scope;
                return executable;
            }
        );

        var compiled = context.CompileChildIn(default, inner);

        await Assert.That(compiled).IsSameReferenceAs(executable);
        await Assert.That(seen).IsSameReferenceAs(inner);
        await Assert.That(context.Binding).IsSameReferenceAs(binding);
    }

    [Test]
    public async Task Context_CompileChildIn_RejectsANullScope()
    {
        var context = new XsltCompileContext(default, Scope, static _ => null, new FakeProblemSink());

        await Assert.That(() => context.CompileChildIn(default, null!)).Throws<ArgumentNullException>();
    }
}
