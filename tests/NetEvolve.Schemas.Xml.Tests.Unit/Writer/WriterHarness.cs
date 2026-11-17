namespace NetEvolve.Schemas.Xml.Tests.Unit.Writer;

using System;
using System.Buffers;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Text.NameTable;
using NetEvolve.Schemas.Xml.Writer;

/// <summary>Drives an <see cref="XmlWriter"/> with readable event scripts.</summary>
internal static class WriterHarness
{
    internal static SpanInterningNameTable NewTable() => new(10_000);

    internal static XmlName N(XmlNameTable t, string local, string prefix = "", string uri = "") =>
        t.Intern(prefix, local, uri);

    internal static string Chars(Action<XmlWriter, XmlNameTable> script, bool canonical = false)
    {
        var table = NewTable();
        var buffer = new ArrayBufferWriter<char>();
        using var writer = new XmlWriter(buffer, table, new XmlWriterOptions(canonical));
        script(writer, table);

        return buffer.WrittenSpan.ToString();
    }

    internal static byte[] Bytes(
        Action<XmlWriter, XmlNameTable> script,
        bool canonical = false,
        XmlWriterEncoding encoding = XmlWriterEncoding.Utf8
    )
    {
        var table = NewTable();
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new XmlWriter(buffer, table, new XmlWriterOptions(canonical, encoding));
        script(writer, table);

        return buffer.WrittenSpan.ToArray();
    }

    internal static void Start(
        this XmlWriter w,
        string local,
        string prefix = "",
        string uri = "",
        (string Prefix, string Uri)[]? decls = null,
        Att[]? attrs = null
    )
    {
        var t = w.Names;
        w.StartElement(N(t, local, prefix, uri), default);
        foreach (var (p, u) in decls ?? [])
        {
            w.NamespaceDeclaration(t.Intern(p), t.Intern(u));
        }

        foreach (var a in attrs ?? [])
        {
            w.Attribute(N(t, a.Local, a.Prefix, a.Uri), a.Value);
        }

        w.EndAttributes();
    }

    internal static void End(this XmlWriter w, string local, string prefix = "", string uri = "") =>
        w.EndElement(N(w.Names, local, prefix, uri));

    /// <summary>A fixed-capacity character writer that can be rewound, for allocation measurements.</summary>
    internal sealed class RewindableCharWriter : IBufferWriter<char>
    {
        private readonly char[] _array = new char[4096];

        public int Count { get; private set; }

        public void Rewind() => Count = 0;

        public void Advance(int count) => Count += count;

        public Memory<char> GetMemory(int sizeHint = 0) => _array.AsMemory(Count);

        public Span<char> GetSpan(int sizeHint = 0) => _array.AsSpan(Count);
    }
}
