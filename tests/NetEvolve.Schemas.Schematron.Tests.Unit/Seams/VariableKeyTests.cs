namespace NetEvolve.Schemas.Schematron.Tests.Unit.Seams;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Seams;

public class VariableKeyTests
{
    [Test]
    public async Task Of_WithoutNamespace_ReturnsTheLocalName() =>
        await Assert.That(VariableKey.Of(string.Empty, "v")).IsEqualTo("v");

    [Test]
    public async Task Of_WithNamespace_ReturnsTheExpandedName() =>
        await Assert.That(VariableKey.Of("urn:f", "v")).IsEqualTo("{urn:f}v");

    [Test]
    [Arguments("v", "", "v")]
    [Arguments("{urn:f}v", "urn:f", "v")]
    [Arguments("{v", "", "{v")]
    [Arguments("{}v", "", "v")]
    public async Task ToQName_ReadsAKeyBack(string key, string namespaceName, string local)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(namespaceName);
        ArgumentNullException.ThrowIfNull(local);

        var name = VariableKey.ToQName(key);

        await Assert.That(name.NamespaceName).IsEqualTo(namespaceName);
        await Assert.That(name.LocalName).IsEqualTo(local);
    }

    [Test]
    public async Task ToQName_WithNull_Throws() =>
        await Assert.That(() => VariableKey.ToQName(null!)).Throws<ArgumentNullException>();
}
