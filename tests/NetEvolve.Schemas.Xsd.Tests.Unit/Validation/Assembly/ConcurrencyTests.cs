namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Assembly;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ConcurrencyTests
{
    [Test]
    public async Task Validate_OneCompiledValidatorFromManyThreads_GivesIdenticalResults()
    {
        var validator = AssemblyFixture.Build(AssemblyFixture.Schema);
        var documents = new[] { AssemblyFixture.ValidInstance, AssemblyFixture.InvalidInstance };
        var expected = documents
            .Select(d =>
                AssemblyFixture.Describe(validator.Validate(Encoding.UTF8.GetBytes(d), AssemblyFixture.InstanceId))
            )
            .ToArray();

        var tasks = Enumerable
            .Range(0, 64)
            .Select(n =>
                Task.Run(() =>
                {
                    var observed = new List<bool>();
                    for (var i = 0; i < 25; i++)
                    {
                        var index = (n + i) % documents.Length;
                        var bytes = Encoding.UTF8.GetBytes(documents[index]);
                        var result = validator.Validate(bytes, AssemblyFixture.InstanceId);
                        observed.Add(
                            string.Equals(AssemblyFixture.Describe(result), expected[index], StringComparison.Ordinal)
                        );
                    }

                    return observed;
                })
            )
            .ToArray();
        var all = (await Task.WhenAll(tasks)).SelectMany(o => o).ToArray();

        await Assert.That(all.Length).IsEqualTo(64 * 25);
        await Assert.That(all.All(same => same)).IsTrue();
        await Assert.That(expected[0]).IsNotEqualTo(expected[1]);
    }
}
