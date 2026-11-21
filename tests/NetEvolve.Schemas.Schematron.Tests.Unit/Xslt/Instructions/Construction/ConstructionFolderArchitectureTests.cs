namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Construction;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Tests.Unit.SuitePins;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.Xml.Model.Traversal;

/// <summary>
/// The Construction folder builds nodes only through the sink and the Xml pump (design #150 EP-8, #214): it creates no tree builder
/// of its own, reaches no <c>XmlDocumentTree</c> or System.Xml type, and writes no LINQ, no hand-written argument guard, and no
/// <c>System.Text.Json</c> (#253).
/// </summary>
public class ConstructionFolderArchitectureTests
{
    private const string Folder = "NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction";

    private const BindingFlags All =
        BindingFlags.Public
        | BindingFlags.NonPublic
        | BindingFlags.Instance
        | BindingFlags.Static
        | BindingFlags.DeclaredOnly;

    private static async Task<List<(string File, string Text)>> SourcesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var folder = Path.Combine(
            SuitePinFiles.RepositoryRoot,
            "src",
            "NetEvolve.Schemas.Schematron",
            "Xslt",
            "Instructions",
            "Construction"
        );
        var sources = new List<(string, string)>();
        foreach (var file in Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories))
        {
            sources.Add((Path.GetFileName(file), await File.ReadAllTextAsync(file, cancellationToken)));
        }

        return sources;
    }

    [Test]
    public async Task Construction_CreatesNoTreeBuilder_AndCopiesNodesThroughThePump()
    {
        var violations = new List<string>();
        var checkedTypes = 0;
        var usesPump = false;

        foreach (var type in typeof(Registration).Assembly.GetTypes())
        {
            if (!string.Equals(type.Namespace, Folder, StringComparison.Ordinal))
            {
                continue;
            }

            checkedTypes++;
            foreach (var field in type.GetFields(All))
            {
                Collect(field.FieldType, type, violations);
            }

            foreach (var method in type.GetMethods(All))
            {
                usesPump |= Scan(method, type, violations);
            }

            foreach (var ctor in type.GetConstructors(All))
            {
                usesPump |= Scan(ctor, type, violations);
            }
        }

        await Assert.That(checkedTypes).IsGreaterThan(10);
        await Assert.That(usesPump).IsTrue();
        await Assert.That(violations).IsEmpty();
    }

    [Test]
    public async Task Construction_ContainsNoLinq_NoSystemXml_NoTreeBuilderOfItsOwn_AndNoHandWrittenArgumentThrow(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sources = await SourcesAsync(cancellationToken);
        var offenders = new List<string>();
        foreach (var (file, text) in sources)
        {
            if (
                text.Contains("System.Linq", StringComparison.Ordinal)
                || text.Contains(".Select(", StringComparison.Ordinal)
                || text.Contains(".Where(", StringComparison.Ordinal)
                || text.Contains("using System.Xml", StringComparison.Ordinal)
                || text.Contains("System.Text.Json", StringComparison.Ordinal)
                || text.Contains("new XmlTreeBuilder", StringComparison.Ordinal)
                || text.Contains("is XmlDocumentTree", StringComparison.Ordinal)
                || text.Contains("throw new Argument", StringComparison.Ordinal)
                || text.Contains("ArgumentNullException.ThrowIfNull", StringComparison.Ordinal)
            )
            {
                offenders.Add(file);
            }
        }

        await Assert.That(sources.Count).IsGreaterThan(10);
        await Assert.That(offenders).IsEmpty();
    }

    [Test]
    public async Task Construction_UsesThePumpAndTheSinkOfTheXmlPackage(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sources = await SourcesAsync(cancellationToken);
        var joined = string.Join("\n", sources.ConvertAll(source => source.Text));

        await Assert.That(joined).Contains("XmlTreePump.Pump(");
        await Assert.That(joined).Contains("BeginTree()");
        await Assert.That(joined).Contains("EndTree(");
    }

    private static void Collect(Type used, Type owner, List<string> violations)
    {
        if (
            used == typeof(XmlDocumentTree)
            || used == typeof(XmlTreeBuilder)
            || (used.Namespace?.StartsWith("System.Xml", StringComparison.Ordinal) ?? false)
        )
        {
            violations.Add($"{owner.Name} references {used.FullName}");
        }
    }

    // Scans the IL for newobj/call tokens; a token that does not resolve is no operand and is skipped.
    private static bool Scan(MethodBase method, Type owner, List<string> violations)
    {
        var il = method.GetMethodBody()?.GetILAsByteArray();
        var pump = false;
        if (il is null)
        {
            return false;
        }

        for (var i = 0; i + 4 < il.Length; i++)
        {
            if (il[i] is not (0x73 or 0x28 or 0x6F))
            {
                continue;
            }

            MethodBase? target;
            try
            {
                target = method.Module.ResolveMethod(BitConverter.ToInt32(il, i + 1));
            }
            catch (Exception ex) when (ex is ArgumentException or BadImageFormatException)
            {
                continue;
            }

            var declaring = target?.DeclaringType;
            if (declaring is null)
            {
                continue;
            }

            pump |= declaring == typeof(XmlTreePump);
            Collect(declaring, owner, violations);
        }

        return pump;
    }
}
