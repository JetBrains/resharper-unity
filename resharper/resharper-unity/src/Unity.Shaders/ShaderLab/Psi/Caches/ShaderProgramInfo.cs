#nullable enable
using System.Collections.Generic;
using JetBrains.Serialization;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Caches
{
    public class ShaderProgramInfo
    {
        public static readonly IUnsafeMarshaller<ShaderProgramInfo> Marshaller = new UniversalMarshaller<ShaderProgramInfo>(Read, Write);

        public Dictionary<string, string> DefinedMacros { get; }
        public int ShaderTarget { get; }
        public bool IsSurface { get; }
        public string[]? ShaderVariants { get; }

        public ShaderProgramInfo(Dictionary<string, string> definedMacros, int shaderTarget, bool isSurface, string[]? shaderVariants)
        {
            DefinedMacros = definedMacros;
            ShaderTarget = shaderTarget;
            IsSurface = isSurface;
            ShaderVariants = shaderVariants;
        }

        private static ShaderProgramInfo Read(UnsafeReader reader)
        {
            var definedMacros = reader.ReadDictionary<string, string, Dictionary<string, string>>(UnsafeReader.StringDelegate!, UnsafeReader.StringDelegate!, count => new Dictionary<string, string>(count))!;
            var shaderTarget = reader.ReadInt32();
            var isSurface = reader.ReadBoolean();
            var shaderVariants = (string[]?)reader.ReadArray(UnsafeReader.StringDelegate)!;
            return new ShaderProgramInfo(definedMacros, shaderTarget, isSurface, shaderVariants);
        }

        private static void Write(UnsafeWriter writer, ShaderProgramInfo item)
        {
            writer.Write(UnsafeWriter.StringDelegate, UnsafeWriter.StringDelegate, item.DefinedMacros);
            writer.WriteInt32(item.ShaderTarget);
            writer.WriteBoolean(item.IsSurface);
            writer.WriteCollection(UnsafeWriter.StringDelegate, item.ShaderVariants);
        }
    }
}