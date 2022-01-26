using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Collections;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Serialization;
using JetBrains.Util;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.HlslSupport
{
    [SolutionComponent]
    public class ShaderContextDataPresentationCache : SimpleICache<List<ShaderContextDataPresentationCache.ShaderProgramInfo>>, IBuildMergeParticipant<IPsiSourceFile>
    {
        private readonly ISolution mySolution;

        private ConcurrentDictionary<IPsiSourceFile, ConcurrentDictionary<TextRange, (int, int)>> myFileAndRangeToLine 
            = new ConcurrentDictionary<IPsiSourceFile, ConcurrentDictionary<TextRange, (int, int)>> ();
        
        public ShaderContextDataPresentationCache(Lifetime lifetime, IShellLocks shellLocks, ISolution solution, IPersistentIndexManager persistentIndexManager) : 
            base(lifetime, shellLocks, persistentIndexManager, UnsafeMarshallers.GetCollectionMarshaller(Read, Write, c => new List<ShaderProgramInfo>(c)))
        {
            mySolution = solution;
        }

        private static ShaderProgramInfo Read(UnsafeReader reader)
        {
            return new ShaderProgramInfo(new TextRange(reader.ReadInt(), reader.ReadInt()), reader.ReadInt(), reader.ReadInt());
        }

        private static void Write(UnsafeWriter writer, ShaderProgramInfo value)
        {
            writer.Write(value.TextRange.StartOffset);
            writer.Write(value.TextRange.EndOffset);
            writer.Write(value.LineStart);
            writer.Write(value.LineEnd);
        }

        protected override bool IsApplicable(IPsiSourceFile sf)
        {
            return sf.LanguageType.Is<ShaderLabProjectFileType>();
        }

        public override object Build(IPsiSourceFile sourceFile, bool isStartup)
        {
            return Build(sourceFile);
        }

        public object Build(IPsiSourceFile key)
        {
            var file = key.GetDominantPsiFile<ShaderLabLanguage>();
            if (file == null)
                return null;

            var document = key.Document;
            return file.Descendants<ICgContent>().Collect().Select(t =>
            {
                var range = t.GetDocumentRange();

                return new ShaderProgramInfo(range.TextRange, (int)range.StartOffset.ToDocumentCoords().Line, (int)range.EndOffset.ToDocumentCoords().Line  );
            }).ToList();
        }
        
        public override void Merge(IPsiSourceFile sourceFile, object builtPart)
        {

            RemoveFromLocalCache(sourceFile);
            AddToLocalCache(sourceFile, builtPart as List<ShaderProgramInfo>);
            base.Merge(sourceFile, builtPart);
        }

        public override void MergeLoaded(object data)
        {
            base.MergeLoaded(data);
            PopulateLocalCache();
        }

        public override void Drop(IPsiSourceFile sourceFile)
        {
            RemoveFromLocalCache(sourceFile);
            base.Drop(sourceFile);
        }

        private void PopulateLocalCache()
        {
            foreach (var (psiSourceFile, cacheItem) in Map)
                AddToLocalCache(psiSourceFile, cacheItem);
        }

        private void AddToLocalCache(IPsiSourceFile sourceFile, [CanBeNull] List<ShaderProgramInfo> cacheItem)
        {
            if (cacheItem == null)
                return;

            var result = new ConcurrentDictionary<TextRange, (int, int)>();

            foreach (var info in cacheItem)
            {
                result[info.TextRange] = (info.LineStart, info.LineEnd);
            }

            myFileAndRangeToLine[sourceFile] = result;
        }

        private void RemoveFromLocalCache(IPsiSourceFile sourceFile)
        {
            myFileAndRangeToLine.TryRemove(sourceFile, out _);
        }

        public (int startLine, int endLine)? GetRangeForShaderProgram(IPsiSourceFile sourceFile, TextRange textRange)
        {
            mySolution.Locks.AssertReadAccessAllowed();
            
            if (myFileAndRangeToLine.TryGetValue(sourceFile, out var data) &&
                data.TryGetValue(textRange, out var range))
                return range;
            return null;
        }
        
        public struct ShaderProgramInfo
        {
            public TextRange TextRange;
            public int LineStart;
            public int LineEnd;

            public ShaderProgramInfo(TextRange textRange, int start, int end)
            {
                TextRange = textRange;
                LineStart = start;
                LineEnd = end;
            }
        }
    }
}