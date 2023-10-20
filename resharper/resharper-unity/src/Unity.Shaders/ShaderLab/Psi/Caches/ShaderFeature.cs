#nullable enable
using System.Collections.Immutable;
using JetBrains.Serialization;
using JetBrains.Util;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Caches;

public readonly struct ShaderFeature
{
    public readonly ImmutableArray<Entry> Entries;
    public readonly bool AllowAllDisabled;

    public ShaderFeature(ImmutableArray<Entry> entries, bool allowAllDisabled)
    {
        Entries = entries;
        AllowAllDisabled = allowAllDisabled;
    }

    public static readonly UnsafeReader.ReadDelegate<ShaderFeature> ReadDelegate = reader => new ShaderFeature(
        reader.ReadImmutableArray(Entry.EntryReadDelegate),
        reader.ReadBoolean()
    );

    public static readonly UnsafeWriter.WriteDelegate<ShaderFeature> WriteDelegate = (writer, feature) =>
    {
        writer.WriteImmutableArray(Entry.EntryWriteDelegate, feature.Entries);
        writer.WriteBoolean(feature.AllowAllDisabled);
    };
    
    public readonly struct Entry
    {
        public readonly string Keyword;
        public readonly TextRange TextRange;

        public Entry(string keyword, TextRange textRange)
        {
            Keyword = keyword;
            TextRange = textRange;
        }

        public static readonly UnsafeReader.ReadDelegate<Entry> EntryReadDelegate = reader => new Entry(reader.ReadString()!, new TextRange(reader.ReadInt32(), reader.ReadInt32()));
        public static readonly UnsafeWriter.WriteDelegate<Entry> EntryWriteDelegate = (writer, entry) =>
        {
            writer.WriteString(entry.Keyword);
            writer.WriteInt32(entry.TextRange.StartOffset);
            writer.WriteInt32(entry.TextRange.Length);
        };
    }
}