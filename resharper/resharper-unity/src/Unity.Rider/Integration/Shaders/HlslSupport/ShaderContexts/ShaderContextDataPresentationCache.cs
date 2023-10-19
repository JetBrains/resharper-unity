#nullable enable
using System.Collections.Generic;
using System.Linq;
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
using JetBrains.Serialization;
using JetBrains.Util;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Shaders.HlslSupport.ShaderContexts
{
    [SolutionComponent]
    public class ShaderContextDataPresentationCache : SimplePsiSourceFileCacheWithLocalCache<List<ShaderContextDataPresentationCache.ShaderProgramInfo>, ShaderContextDataPresentationCache.ShaderFileInfo>, IBuildMergeParticipant<IPsiSourceFile>
    {
        public ShaderContextDataPresentationCache(Lifetime lifetime, IShellLocks shellLocks, IPersistentIndexManager persistentIndexManager) : 
            base(lifetime, shellLocks, persistentIndexManager, UnsafeMarshallers.GetCollectionMarshaller(Read, Write, c => new List<ShaderProgramInfo>(c)), "Unity::Shaders::ShaderContextDataPresentationCacheUpdated")
        {
        }

        private static ShaderProgramInfo Read(UnsafeReader reader) => new(new TextRange(reader.ReadInt(), reader.ReadInt()), reader.ReadInt(), reader.ReadInt());

        private static void Write(UnsafeWriter writer, ShaderProgramInfo value)
        {
            writer.Write(value.TextRange.StartOffset);
            writer.Write(value.TextRange.EndOffset);
            writer.Write(value.LineStart);
            writer.Write(value.LineEnd);
        }

        protected override bool IsApplicable(IPsiSourceFile sf) => sf.LanguageType.Is<ShaderLabProjectFileType>();

        public override object? Build(IPsiSourceFile sourceFile, bool isStartup) => Build(sourceFile);

        public object? Build(IPsiSourceFile key)
        {
            var file = key.GetDominantPsiFile<ShaderLabLanguage>();
            return file?.Descendants<ICgContent>().Collect().Select(t =>
            {
                var range = t.GetDocumentRange();

                return new ShaderProgramInfo(range.TextRange, (int)range.StartOffset.ToDocumentCoords().Line, (int)range.EndOffset.ToDocumentCoords().Line);
            }).ToList();
        }

        protected override ShaderFileInfo BuildLocal(IPsiSourceFile sourceFile, List<ShaderProgramInfo> cacheItem)
        {
            var result = new Dictionary<TextRange, (int, int)>();
            foreach (var info in cacheItem) 
                result.Add(info.TextRange, (info.LineStart, info.LineEnd));
            return new ShaderFileInfo(result);
        }

        public (int startLine, int endLine)? GetRangeForShaderProgram(IPsiSourceFile sourceFile, TextRange textRange) 
            => TryGetLocalCacheValue(sourceFile, out var fileInfo) && fileInfo.Mapping.TryGetValue(textRange, out var range) ? range : null;
        
        public readonly struct ShaderProgramInfo
        {
            public readonly TextRange TextRange;
            public readonly int LineStart;
            public readonly int LineEnd;

            public ShaderProgramInfo(TextRange textRange, int start, int end)
            {
                TextRange = textRange;
                LineStart = start;
                LineEnd = end;
            }
        }

        public readonly struct ShaderFileInfo
        {
            public readonly Dictionary<TextRange, (int StartLine, int EndLine)> Mapping;

            public ShaderFileInfo(Dictionary<TextRange, (int StartLine, int EndLine)> mapping)
            {
                Mapping = mapping;
            }
        }
    }
}