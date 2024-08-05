#nullable enable
using System.Collections.Generic;
using JetBrains.Application.Parts;
using JetBrains.Application.Threading;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Common.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Serialization;
using JetBrains.Util;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Shaders.HlslSupport.ShaderContexts
{
    [SolutionComponent(InstantiationEx.LegacyDefault)]
    public class ShaderContextDataPresentationCache(Lifetime lifetime, IShellLocks shellLocks, IPersistentIndexManager persistentIndexManager)
        : SimplePsiSourceFileCacheWithLocalCache<List<ShaderContextDataPresentationCache.CacheItem>, ShaderContextDataPresentationCache.ShaderFileInfo>(lifetime, shellLocks, persistentIndexManager,
            UnsafeMarshallers.GetCollectionMarshaller(Read, Write, c => new List<CacheItem>(c)), "Unity::Shaders::ShaderContextDataPresentationCacheUpdated")
    {
        private static CacheItem Read(UnsafeReader reader) => new(new TextRange(reader.ReadInt32(), reader.ReadInt32()), new ShaderFileEntryInfo(reader.ReadInt32(), reader.ReadInt32(), reader.ReadString()));

        private static void Write(UnsafeWriter writer, CacheItem value)
        {
            writer.WriteInt32(value.TextRange.StartOffset);
            writer.WriteInt32(value.TextRange.EndOffset);
            writer.WriteInt32(value.Info.StartLine);
            writer.WriteInt32(value.Info.EndLine);
            writer.WriteString(value.Info.Hint);
        }

        protected override bool IsApplicable(IPsiSourceFile sf) => sf.LanguageType.Is<ShaderLabProjectFileType>();

        public override object? Build(IPsiSourceFile sourceFile, bool isStartup) => Build(sourceFile);

        public object? Build(IPsiSourceFile key)
        {
            if (key.GetDominantPsiFile<ShaderLabLanguage>() is not { } file)
                return null;

            var entries = new List<CacheItem>();
            foreach (var cgContent in file.Descendants<ICgContent>())
            {
                var hint = cgContent.GetContainingNode<ITexturePassDef>()?.GetEntityNameToken()?.GetUnquotedText();
                var range = cgContent.GetDocumentRange();
                var entryInfo = new ShaderFileEntryInfo((int)range.StartOffset.ToDocumentCoords().Line, (int)range.EndOffset.ToDocumentCoords().Line, hint);
                entries.Add(new CacheItem(range.TextRange, entryInfo));
            }
            return entries;
        }

        protected override ShaderFileInfo BuildLocal(IPsiSourceFile sourceFile, List<CacheItem> cacheItem)
        {
            var result = new Dictionary<TextRange, ShaderFileEntryInfo>();
            foreach (var info in cacheItem)
                result.Add(info.TextRange, info.Info);
            return new ShaderFileInfo(result);
        }

        public ShaderFileEntryInfo? GetShaderProgramPresentationInfo(IPsiSourceFile sourceFile, TextRange textRange) 
            => TryGetLocalCacheValue(sourceFile, out var fileInfo) && fileInfo.Mapping.TryGetValue(textRange, out var range) ? range : null;
        
        public readonly struct CacheItem(TextRange textRange, ShaderFileEntryInfo info)
        {
            public readonly TextRange TextRange = textRange;
            public readonly ShaderFileEntryInfo Info = info;
        }

        public readonly struct ShaderFileInfo(Dictionary<TextRange, ShaderFileEntryInfo> mapping)
        {
            public Dictionary<TextRange, ShaderFileEntryInfo> Mapping => mapping;
        }

        public readonly record struct ShaderFileEntryInfo(int StartLine, int EndLine, string? Hint = null);
    }
}