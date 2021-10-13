using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.Utils;
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
        private AsmDefVersionDefine(string symbol, string packageId, string expression)
            : this(symbol, packageId, expression, JetSemanticVersionRange.Parse(expression))
        {
        }

        private AsmDefVersionDefine(string symbol, string packageId, string expression,
                                    JetSemanticVersionRange versionRange)
        {
            Symbol = symbol;
            PackageId = packageId;
            Expression = expression;
            VersionRange = versionRange;
        }

        public static AsmDefVersionDefine? Create(string symbol, string packageId, string expression)
        {
            if (!JetSemanticVersionRange.TryParse(expression, out var versionRange))
                return null;
            return new AsmDefVersionDefine(symbol, packageId, expression, versionRange);
        }

        public string Symbol { get; }
        public string PackageId { get; }
        public string Expression { get; }
        public JetSemanticVersionRange VersionRange { get; }

        public static AsmDefVersionDefine Read(UnsafeReader reader)
        {
            var symbol = reader.ReadString()!;
            var packageId = reader.ReadString()!;
            var expression = reader.ReadString()!;
            return new AsmDefVersionDefine(symbol, packageId, expression);
        }

        public static void Write(UnsafeWriter writer, AsmDefVersionDefine versionDefine)
        {
            writer.Write(versionDefine.Symbol);
            writer.Write(versionDefine.PackageId);
            writer.Write(versionDefine.Expression);
        }
    }
}