namespace NetEvolve.Schemas.Xml.Tests.Unit.Input.Sequence;

using System.Text;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>One document in a given encoding: the sample of every sequence parity test.</summary>
public sealed record SequenceSample(string Name, string Xml, Encoding Encoding, bool Bom = false, bool Dtd = false)
{
    internal XmlReaderOptions Options =>
        Dtd ? XmlReaderOptions.Default.WithDtdProcessing(true) : XmlReaderOptions.Default;

    internal byte[] Bytes
    {
        get
        {
            var body = Encoding.GetBytes(Xml);
            if (!Bom)
            {
                return body;
            }

            var preamble = Encoding.GetPreamble();
            var all = new byte[preamble.Length + body.Length];
            preamble.CopyTo(all, 0);
            body.CopyTo(all, preamble.Length);
            return all;
        }
    }

    public override string ToString() => Name;
}
