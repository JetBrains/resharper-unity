using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.VersionUtils;
using JetBrains.Serialization;
using JetBrains.Util.PersistentMap;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.Caches
{
    public class AsmDefCacheItem
    {
        public static readonly IUnsafeMarshaller<AsmDefCacheItem> Marshaller =
            new UniversalMarshaller<AsmDefCacheItem>(Read, Write);

        private static readonly IUnsafeMarshaller<AsmDefVersionDefine[]> ourVersionDefinesMarshaller =
            UnsafeMarshallers.GetArrayMarshaller(AsmDefVersionDefine.Read, AsmDefVersionDefine.Write);

        private readonly string[] myReferences;
        private readonly AsmDefVersionDefine[] myVersionDefines;

        public AsmDefCacheItem(string name, int declarationOffset, string[] references,
                               AsmDefVersionDefine[] versionDefines)
        {
            Name = name;
            DeclarationOffset = declarationOffset;
            myReferences = references;
            myVersionDefines = versionDefines;
        }

        public string Name { get; }
        public int DeclarationOffset { get; }
        public IEnumerable<string> References => myReferences;
        public IEnumerable<AsmDefVersionDefine> VersionDefines => myVersionDefines;

        public override string ToString()
        {
            var references = string.Join(", ", myReferences);
            var versionDefines = string.Join(", ", myVersionDefines.Select(d => d.ToString()));
            return $"{Name}:{DeclarationOffset} references:[{references}] versionDefines:[{versionDefines}]";
        }

        private static AsmDefCacheItem Read(UnsafeReader reader)
        {
            var name = reader.ReadString()!;
            var declarationOffset = reader.ReadInt();
            var references = UnsafeMarshallers.StringArrayMarshaller.Unmarshal(reader);
            var versionDefines = ourVersionDefinesMarshaller.Unmarshal(reader);
            return new AsmDefCacheItem(name, declarationOffset, references, versionDefines);
        }

        private static void Write(UnsafeWriter writer, AsmDefCacheItem value)
        {
            writer.Write(value.Name);
            writer.Write(value.DeclarationOffset);
            UnsafeMarshallers.StringArrayMarshaller.Marshal(writer, value.myReferences);
            ourVersionDefinesMarshaller.Marshal(writer, value.myVersionDefines);
        }
    }

    public class AsmDefVersionDefine
    {
        // We know that expression is valid here, because this constructor is only used when deserialising
        // We might also get a Unity product version range (if the resource name is "Unity", then we compare against
        // the product version). We convert to a semver compatible version range.
        private AsmDefVersionDefine(string resourceName, string symbol, string expression)
            : this(resourceName, symbol, expression, UnitySemanticVersionRange.Parse(expression))
        {
        }

        private AsmDefVersionDefine(string resourceName, string symbol, string expression,
                                    UnitySemanticVersionRange versionRange)
        {
            ResourceName = resourceName;
            Symbol = symbol;
            Expression = expression;
            VersionRange = versionRange;
        }

        public static AsmDefVersionDefine? Create(string resourceName, string symbol, string expression)
        {
            return UnitySemanticVersionRange.TryParse(expression, out var versionRange)
                ? new AsmDefVersionDefine(resourceName, symbol, expression, versionRange)
                : null;
        }

        public string ResourceName { get; }
        public string Symbol { get; }
        public string Expression { get; }
        public UnitySemanticVersionRange VersionRange { get; }

        public override string ToString()
        {
            return $"{ResourceName}:{Expression}:{Symbol}";
        }

        public static AsmDefVersionDefine Read(UnsafeReader reader)
        {
            var resourceName = reader.ReadString()!;
            var symbol = reader.ReadString()!;
            var expression = reader.ReadString()!;  // Possible non-semver compatible Unity product version range
            return new AsmDefVersionDefine(resourceName, symbol, expression);
        }

        public static void Write(UnsafeWriter writer, AsmDefVersionDefine versionDefine)
        {
            writer.Write(versionDefine.ResourceName);
            writer.Write(versionDefine.Symbol);
            writer.Write(versionDefine.Expression);// Possible non-semver compatible Unity product version range
        }
    }
}