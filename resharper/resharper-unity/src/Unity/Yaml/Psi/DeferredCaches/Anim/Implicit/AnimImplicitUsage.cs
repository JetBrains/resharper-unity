using JetBrains.Annotations;
using JetBrains.Serialization;
using JetBrains.Util;
using JetBrains.Util.Maths;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Anim.Implicit
{
    public class AnimImplicitUsage
    {
        public AnimImplicitUsage(TextRange textRangeOwnerPsiPersistentIndex, 
            OWORD textRangeOwner, 
            [NotNull] string functionName)
        {
            TextRangeOwnerPsiPersistentIndex = textRangeOwnerPsiPersistentIndex;
            TextRangeOwner = textRangeOwner;
            FunctionName = functionName;
        }

        public TextRange TextRangeOwnerPsiPersistentIndex { get; }
        public OWORD TextRangeOwner { get; }
        
        [NotNull]
        public string FunctionName { get; }

        [CanBeNull]
        public static AnimImplicitUsage ReadFrom([NotNull] UnsafeReader reader)
        {
            var functionName = reader.ReadString();
            if (functionName is null) return null;
            return new AnimImplicitUsage(new TextRange(reader.ReadInt32(), reader.ReadInt32()), AssetUtils.ReadOWORD(reader), functionName);
        }

        public void WriteTo([NotNull] UnsafeWriter writer)
        {
            writer.Write(FunctionName);
            writer.Write(TextRangeOwnerPsiPersistentIndex.StartOffset);
            writer.Write(TextRangeOwnerPsiPersistentIndex.EndOffset);
            AssetUtils.WriteOWORD(TextRangeOwner, writer);
        }
    }
}