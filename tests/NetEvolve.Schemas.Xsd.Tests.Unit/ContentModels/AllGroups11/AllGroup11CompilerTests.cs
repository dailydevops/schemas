namespace NetEvolve.Schemas.Xsd.Tests.Unit.ContentModels.AllGroups11;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.ContentModels.AllGroups11;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.Tests.Unit.Elements;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;

public class AllGroup11CompilerTests
{
    private static readonly XPathQName GroupName = new("urn:t", "g");

    private sealed class Compiled
    {
        public Dictionary<XPathQName, int> Ids { get; } = [];

        public List<IXsdComponent> Terms { get; } = [];

        public AllGroup11? Group { get; set; }

        public int IdOf(XPathQName name)
        {
            if (!Ids.TryGetValue(name, out var id))
            {
                id = Ids.Count + 1;
                Ids.Add(name, id);
            }

            return id;
        }

        public bool Accepts(params string[] names)
        {
            var run = Group!.CreateRun();
            foreach (var name in names)
            {
                var qName = name.Contains(':', StringComparison.Ordinal)
                    ? new XPathQName("urn:w", name[2..])
                    : new XPathQName(string.Empty, name);
                if (!run.TryStep(Ids.TryGetValue(qName, out var id) ? id : -1, qName, out _))
                {
                    return false;
                }
            }

            return run.CanEnd();
        }
    }

    private static Compiled Compile(Particle content)
    {
        var compiled = new Compiled();
        compiled.Group = AllGroup11Compiler.Compile(content, compiled.IdOf, compiled.Terms);
        return compiled;
    }

    private static Particle El(string name, long min = 1, long max = 1) => SchemaFixture.Part(name, null, min, max);

    private static Particle Any(string namespaces, long min = 1, long max = 1) =>
        SchemaFixture.Part(SchemaFixture.Wild(namespaces, XsdProcessContents.Lax), min, max);

    private static Particle AllParticle(long min, params Particle[] particles) =>
        SchemaFixture.Group(ModelGroupCompositor.All, min, 1, particles);

    private static Particle Ref(string local, long min, long max, params Particle[] particles) =>
        SchemaFixture.Part(
            ModelGroupDefinition.Create(
                new XPathQName("urn:t", local),
                SchemaFixture.Where,
                ModelGroup.Create(ModelGroupCompositor.All, SchemaFixture.Where, particles)
            ),
            min,
            max
        );

    private static readonly string[][] Samples =
    [
        [],
        ["a"],
        ["a", "a"],
        ["a", "a", "a"],
        ["a", "a", "a", "a"],
        ["b"],
        ["a", "b"],
        ["b", "a", "b", "a"],
        ["w:x"],
        ["a", "w:x"],
        ["w:x", "a", "w:y", "a", "a"],
        ["c"],
        ["a", "b", "w:x"],
        ["a", "a", "b", "b", "w:x", "w:y"],
    ];

    [Test]
    public async Task GroupReference_ToAnAllGroup_BehavesLikeTheInlineGroup()
    {
        Particle[] members = [El("a", 1, 3), El("b", 0, Particle.Unbounded), Any("urn:w", 0, 2)];
        var inline = Compile(AllParticle(1, members));
        var referenced = Compile(Ref("g", 1, 1, members));

        await Assert.That(referenced.Group).IsNotNull();
        await Assert.That(referenced.Group!.MemberCount).IsEqualTo(inline.Group!.MemberCount);
        await Assert.That(referenced.Terms.Count).IsEqualTo(inline.Terms.Count);
        foreach (var sample in Samples)
        {
            await Assert.That(referenced.Accepts(sample)).IsEqualTo(inline.Accepts(sample));
        }

        await Assert.That(referenced.Accepts("a", "b", "w:x")).IsTrue();
        await Assert.That(referenced.Accepts("a", "a", "a", "a")).IsFalse();
    }

    [Test]
    public async Task GroupReference_WithMinOccursZero_MakesTheGroupOptional()
    {
        var referenced = Compile(Ref("g", 0, 1, El("a", 1, 2), Any("urn:w")));

        await Assert.That(referenced.Group!.IsOptional).IsTrue();
        await Assert.That(referenced.Accepts()).IsTrue();
        await Assert.That(referenced.Accepts("a")).IsFalse();
        await Assert.That(referenced.Accepts("a", "w:x")).IsTrue();
    }

    [Test]
    public async Task SchemaDocument_WithRepeatingMembersWildcardAndReference_CompilesLikeTheInlineGroup()
    {
        var fixture = ElementFixture
            .Build(
                """
                <xs:group name="g">
                  <xs:all>
                    <xs:element name="a" maxOccurs="3"/>
                    <xs:element name="b" minOccurs="0" maxOccurs="unbounded"/>
                    <xs:any namespace="urn:w" minOccurs="0" maxOccurs="2"/>
                  </xs:all>
                </xs:group>
                """
            )
            .Resolve();
        var definition = fixture.Group("g");

        var referenced = Compile(SchemaFixture.Part(definition));
        var inline = Compile(SchemaFixture.Part(definition.ModelGroup));

        await Assert.That(fixture.Issues).IsEmpty();
        await Assert.That(referenced.Group!.MemberCount).IsEqualTo(3);
        foreach (var sample in Samples)
        {
            await Assert.That(referenced.Accepts(sample)).IsEqualTo(inline.Accepts(sample));
        }

        await Assert.That(referenced.Accepts("b", "a", "b", "a", "w:x")).IsTrue();
        await Assert.That(referenced.Accepts("a", "w:x", "w:y", "w:z")).IsFalse();
    }

    [Test]
    public async Task NestedAllGroup_WithExactlyOneOccurrence_IsFlattenedIntoTheEnclosingGroup()
    {
        var content = AllParticle(
            1,
            El("a"),
            Ref("n", 1, 1, El("b", 1, 2), Any("urn:w", 0, 1)),
            SchemaFixture.Group(ModelGroupCompositor.All, 1, 1, El("c"))
        );

        var compiled = Compile(content);

        await Assert.That(compiled.Group!.MemberCount).IsEqualTo(4);
        await Assert.That(compiled.Terms.Count).IsEqualTo(4);
        await Assert.That(((ElementDeclaration)compiled.Terms[0]).Name.LocalName).IsEqualTo("a");
        await Assert.That(((ElementDeclaration)compiled.Terms[1]).Name.LocalName).IsEqualTo("b");
        await Assert.That(compiled.Terms[2] is IWildcard).IsTrue();
        await Assert.That(((ElementDeclaration)compiled.Terms[3]).Name.LocalName).IsEqualTo("c");
        await Assert.That(compiled.Accepts("c", "b", "a", "b", "w:x")).IsTrue();
        await Assert.That(compiled.Accepts("c", "a")).IsFalse();
    }

    [Test]
    public async Task NestedAllGroup_WithAnotherOccurrence_IsLeftOut_AsItBreaksAllGroupLimited()
    {
        // Limitation: All Group Limited (Structures 3.8.6.2, clause 1.3) is a schema error (XSD-60); here the member is skipped.
        var content = AllParticle(1, El("a"), Ref("n", 0, 1, El("b")), Ref("m", 1, 2, El("c")));

        var compiled = Compile(content);

        await Assert.That(compiled.Group!.MemberCount).IsEqualTo(1);
        await Assert.That(compiled.Terms.Count).IsEqualTo(1);
    }

    [Test]
    public async Task MembersThatCannotOccurOrAreNotElementsOrWildcards_AreLeftOut()
    {
        var unresolved = Particle.CreateReference(
            SchemaFixture.Where,
            1,
            1,
            XsdSymbolSpace.Elements,
            new XPathQName("urn:t", "missing")
        );
        var content = AllParticle(
            1,
            El("a"),
            El("removed", 0, 0),
            unresolved,
            SchemaFixture.Seq(El("s")),
            Any("##any", 0, 0),
            El("b", 0, 2)
        );

        var compiled = Compile(content);

        await Assert.That(compiled.Group!.MemberCount).IsEqualTo(2);
        await Assert.That(compiled.Terms.Count).IsEqualTo(2);
        await Assert.That(((ElementDeclaration)compiled.Terms[1]).Name.LocalName).IsEqualTo("b");
        await Assert.That(compiled.Accepts("b", "a", "b")).IsTrue();
    }

    [Test]
    public async Task NonAllContent_ReturnsNull()
    {
        await Assert.That(Compile(SchemaFixture.Seq(El("a"))).Group).IsNull();
        await Assert.That(Compile(SchemaFixture.Choice(El("a"))).Group).IsNull();
        await Assert
            .That(Compile(SchemaFixture.Part(SchemaFixture.Wild("##any", XsdProcessContents.Lax))).Group)
            .IsNull();
        await Assert.That(Compile(SchemaFixture.Group(ModelGroupCompositor.All, 1, 0, El("a"))).Group).IsNull();
    }

    [Test]
    public async Task NestingBeyondTheCeiling_ReturnsNull_ButARealisticDepthCompiles()
    {
        static Particle Nest(int depth)
        {
            var particle = AllParticle(1, El("a"));
            for (var i = 0; i < depth; i++)
            {
                particle = AllParticle(1, particle);
            }

            return particle;
        }

        await Assert.That(Compile(Nest(30)).Group!.MemberCount).IsEqualTo(1);
        await Assert.That(Compile(Nest(200)).Group).IsNull();
    }

    [Test]
    public async Task Extend_AllGroupWithAllGroup_YieldsOneMergedAllGroup()
    {
        var baseParticle = AllParticle(1, El("a", 1, 2), El("b", 0, 1));
        var own = AllParticle(1, El("c"), Any("urn:w", 0, 2));

        var merged = AllGroup11Compiler.Extend(baseParticle, own);

        await Assert.That(merged).IsNotNull();
        await Assert.That(merged!.MinOccurs).IsEqualTo(1L);
        await Assert.That(merged.MaxOccurs).IsEqualTo(1L);
        var group = (ModelGroup)merged.Term!;
        await Assert.That(group.Compositor).IsEqualTo(ModelGroupCompositor.All);
        await Assert.That(group.Particles.Count).IsEqualTo(4);
        var compiled = Compile(merged);
        await Assert.That(compiled.Group!.MemberCount).IsEqualTo(4);
        await Assert.That(compiled.Terms.Count).IsEqualTo(4);
        await Assert.That(((ElementDeclaration)compiled.Terms[0]).Name.LocalName).IsEqualTo("a");
        await Assert.That(((ElementDeclaration)compiled.Terms[2]).Name.LocalName).IsEqualTo("c");
        await Assert.That(compiled.Accepts("c", "a", "a")).IsTrue();
        await Assert.That(compiled.Accepts("w:x", "b", "c", "a", "w:y")).IsTrue();
        await Assert.That(compiled.Accepts("a")).IsFalse();
        await Assert.That(compiled.Accepts("c")).IsFalse();
    }

    [Test]
    public async Task Extend_TakesTheOrderOfBaseThenOwn_AndTheMinOccursOfTheOwnContent()
    {
        var baseParticle = AllParticle(1, El("a"));
        var own = AllParticle(0, El("b"));

        var merged = AllGroup11Compiler.Extend(baseParticle, own)!;
        var compiled = Compile(merged);

        var particles = ((ModelGroup)merged.Term!).Particles;
        await Assert.That(particles[0]).IsSameReferenceAs(((ModelGroup)baseParticle.Term!).Particles[0]);
        await Assert.That(particles[1]).IsSameReferenceAs(((ModelGroup)own.Term!).Particles[0]);
        await Assert.That(merged.MinOccurs).IsEqualTo(0L);
        await Assert.That(compiled.Group!.IsOptional).IsTrue();
        await Assert.That(compiled.Accepts()).IsTrue();
        await Assert.That(compiled.Accepts("a")).IsFalse();
        await Assert.That(compiled.Accepts("b", "a")).IsTrue();
    }

    [Test]
    public async Task Extend_ThroughGroupReferences_UnwrapsTheDefinitions()
    {
        var baseParticle = Ref("base", 1, 1, El("a"));
        var own = Ref("own", 1, 1, El("b", 0, 3), Any("urn:w", 0, 1));

        var merged = AllGroup11Compiler.Extend(baseParticle, own);
        var compiled = Compile(merged!);

        await Assert.That(compiled.Group!.MemberCount).IsEqualTo(3);
        await Assert.That(compiled.Accepts("b", "b", "a", "w:x")).IsTrue();
    }

    [Test]
    public async Task Extend_ChainedOverTheMergedParticle_KeepsOneAllGroup()
    {
        var first = AllGroup11Compiler.Extend(AllParticle(1, El("a")), AllParticle(1, El("b")))!;
        var second = AllGroup11Compiler.Extend(first, AllParticle(1, El("c", 1, 2)))!;

        var compiled = Compile(second);

        await Assert.That(compiled.Group!.MemberCount).IsEqualTo(3);
        await Assert.That(compiled.Accepts("c", "b", "c", "a")).IsTrue();
        await Assert.That(compiled.Accepts("c", "b", "c", "c", "a")).IsFalse();
    }

    [Test]
    public async Task Extend_WhenEitherContentIsNotAnAllGroup_ReturnsNull()
    {
        var all = AllParticle(1, El("a"));
        var sequence = SchemaFixture.Seq(El("b"));

        await Assert.That(AllGroup11Compiler.Extend(sequence, all)).IsNull();
        await Assert.That(AllGroup11Compiler.Extend(all, sequence)).IsNull();
        await Assert.That(AllGroup11Compiler.Extend(sequence, sequence)).IsNull();
        await Assert
            .That(
                AllGroup11Compiler.Extend(all, SchemaFixture.Part(SchemaFixture.Wild("##any", XsdProcessContents.Lax)))
            )
            .IsNull();
    }

    [Test]
    public async Task Methods_RejectNullArguments()
    {
        var all = AllParticle(1, El("a"));
        var terms = new List<IXsdComponent>();

        await Assert.That(() => AllGroup11Compiler.Compile(null!, _ => 0, terms)).Throws<ArgumentNullException>();
        await Assert.That(() => AllGroup11Compiler.Compile(all, null!, terms)).Throws<ArgumentNullException>();
        await Assert.That(() => AllGroup11Compiler.Compile(all, _ => 0, null!)).Throws<ArgumentNullException>();
        await Assert.That(() => AllGroup11Compiler.Extend(null!, all)).Throws<ArgumentNullException>();
        await Assert.That(() => AllGroup11Compiler.Extend(all, null!)).Throws<ArgumentNullException>();
    }
}
