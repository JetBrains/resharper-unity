#nullable enable
using System.Collections.Generic;
using System.Collections.Immutable;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Injections;
using JetBrains.Serialization;
using JetBrains.Util;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Caches
{
    public class ShaderProgramInfo
    {
        public static readonly IUnsafeMarshaller<ShaderProgramInfo> Marshaller = new UniversalMarshaller<ShaderProgramInfo>(Read, Write);

        public InjectedHlslProgramType InjectedProgramType { get; }
        public ShaderType ShaderType { get; }
        public int ShaderTarget { get; }
        public ImmutableArray<ShaderFeature> ShaderFeatures { get; }
        public Dictionary<string, string> DefinedMacros { get; }
        private readonly OneToListMap<string, ShaderFeature> myKeywordToFeatures; 
        
        public ShaderProgramInfo(InjectedHlslProgramType injectedProgramType, ShaderType shaderType, int shaderTarget, ImmutableArray<ShaderFeature> shaderFeatures, Dictionary<string, string> definedMacros)
        {
            InjectedProgramType = injectedProgramType;
            ShaderTarget = shaderTarget;
            ShaderType = shaderType;
            ShaderFeatures = shaderFeatures;
            DefinedMacros = definedMacros;

            var keywordToFeatures = new OneToListMap<string, ShaderFeature>();
            foreach (var shaderFeature in shaderFeatures)
            foreach (var entry in shaderFeature.Entries)
                keywordToFeatures.Add(entry.Keyword, shaderFeature);
            myKeywordToFeatures = keywordToFeatures;
        }
        
        public ICollection<string> Keywords => myKeywordToFeatures.Keys;
        public OneToListMap<string, ShaderFeature>.ValueCollection GetShaderFeatures(string keyword) => myKeywordToFeatures[keyword];
        public bool HasKeyword(string keyword) => myKeywordToFeatures.ContainsKey(keyword);

        private static ShaderProgramInfo Read(UnsafeReader reader)
        {
            var injectedProgramType = (InjectedHlslProgramType)reader.ReadByte();
            var shaderType = (ShaderType)reader.ReadByte();
            var shaderTarget = reader.ReadInt32();
            var shaderVariants = reader.ReadImmutableArray(ShaderFeature.ReadDelegate);
            var definedMacros = reader.ReadDictionary<string, string, Dictionary<string, string>>(UnsafeReader.StringDelegate!, UnsafeReader.StringDelegate!, count => new Dictionary<string, string>(count))!;
            return new ShaderProgramInfo(injectedProgramType, shaderType, shaderTarget, shaderVariants, definedMacros);
        }

        private static void Write(UnsafeWriter writer, ShaderProgramInfo item)
        {
            writer.WriteByte((byte)item.InjectedProgramType);
            writer.WriteByte((byte)item.ShaderType);
            writer.WriteInt32(item.ShaderTarget);
            writer.WriteCollection(ShaderFeature.WriteDelegate, item.ShaderFeatures);
            writer.Write(UnsafeWriter.StringDelegate, UnsafeWriter.StringDelegate, item.DefinedMacros);
        }
    }
}