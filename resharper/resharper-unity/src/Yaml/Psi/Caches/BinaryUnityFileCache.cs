using System;
using System.Collections.Generic;
using JetBrains.Application.changes;
using JetBrains.Application.Progress;
using JetBrains.Application.Threading;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Serialization;
using JetBrains.Threading;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches
{
    // We need to avoid binary "YAML" files - they're useless to us and bloat and case false positives in the trigram
    // index. Ideally, we'd filter all binary files out before they could be processed by the PSI caches (and therefore
    // the trigram index). The PSI module processor will filter out some, based on file name and by sniffing for the
    // `%YAML` header. For performance reasons, we only sniff the header for large files, so some binary files will
    // still be added (and added to the trigram index).
    // This cache recognises binary YAML files, and the file's IPsiSourceFileProperties.ShouldBuildPsi will return false
    // if the cache says it's binary. When the cache sees a binary file, it notifies the change manager that the PSI
    // file has been modified (technically, the properties have been modified). Now that ShouldBuildPsi returns false,
    // the file is dropped from all caches, including the trigram index, and the caches are no longer called for this
    // file.
    // When a binary file is modified, the PSI module processor will sniff the header and invalidate the file in the
    // cache if it's now a YAML file.
    [PsiComponent]
    public class BinaryUnityFileCache : SimpleICache<BinaryUnityFileCache.BinaryFileCacheItem>
    {
        private readonly GroupingEvent myGroupingEvent;
        private readonly JetHashSet<IPsiSourceFile> myChangedFiles;

        public BinaryUnityFileCache(Lifetime lifetime, ISolution solution,
                                    IPersistentIndexManager persistentIndexManager, IShellLocks locks,
                                    ChangeManager changeManager)
            : base(lifetime, persistentIndexManager, BinaryFileCacheItem.Marshaller)
        {
            myGroupingEvent = solution.Locks.GroupingEvents.CreateEvent(lifetime, "UnityRefresherOnSaveEvent",
                TimeSpan.FromMilliseconds(500), Rgc.Guarded, () =>
                {
                    var changedFiles = new JetHashSet<IPsiSourceFile>(myChangedFiles);
                    myChangedFiles.Clear();

                    if (changedFiles.Count > 0)
                    {
                        locks.ExecuteWithWriteLock(() => changeManager.ExecuteAfterChange(() =>
                        {
                            var builder = new PsiModuleChangeBuilder();
                            foreach (var file in changedFiles)
                            {
                                if (file.IsValid())
                                    builder.AddFileChange(file, PsiModuleChange.ChangeType.Modified);
                            }

                            changeManager.OnProviderChanged(solution, builder.Result, SimpleTaskExecutor.Instance);
                        }));
                    }
                });
            myChangedFiles = new JetHashSet<IPsiSourceFile>();
        }

        public bool IsBinaryFile(IPsiSourceFile sourceFile)
        {
            return Map.TryGetValue(sourceFile, out var value) && value.IsBinary;
        }

        public void Invalidate(IPsiSourceFile sourceFile)
        {
            base.Drop(sourceFile);
        }

        protected override bool IsApplicable(IPsiSourceFile sf)
        {
            return sf.IsLanguageSupported<UnityYamlLanguage>();
        }

        public override object Build(IPsiSourceFile sourceFile, bool isStartup)
        {
            if (!IsApplicable(sourceFile))
                return null;

            if (!(sourceFile.GetDominantPsiFile<UnityYamlLanguage>() is IYamlFile yamlFile))
                return null;

            // Handle empty files
            var tokenBuffer = yamlFile.CachingLexer.TokenBuffer;
            if (tokenBuffer.CachedTokens.Count == 0)
                return null;

            var isBinary = tokenBuffer[0].Type == YamlTokenType.NON_PRINTABLE;
            return new BinaryFileCacheItem(isBinary);
        }

        public override void Merge(IPsiSourceFile sourceFile, object builtPart)
        {
            if (!Map.TryGetValue(sourceFile, out var oldValue))
                oldValue = new BinaryFileCacheItem(false);

            var newValue = builtPart as BinaryFileCacheItem ?? new BinaryFileCacheItem(false);
            if (oldValue.IsBinary != newValue.IsBinary)
            {
                myChangedFiles.Add(sourceFile);
                myGroupingEvent.FireIncoming();
            }

            base.Merge(sourceFile, builtPart);
        }

        public override void Drop(IPsiSourceFile sourceFile)
        {
            // Ignore drop of valid files so that old value is retained in the persistent Map
            if (sourceFile.IsValid())
            {
                RemoveFromDirty(sourceFile);
                return;
            }

            base.Drop(sourceFile);
        }

        public class BinaryFileCacheItem
        {
            public static readonly IUnsafeMarshaller<BinaryFileCacheItem> Marshaller =
                new UniversalMarshaller<BinaryFileCacheItem>(Read, Write);

            public readonly bool IsBinary;

            public BinaryFileCacheItem(bool isBinary)
            {
                IsBinary = isBinary;
            }

            private static BinaryFileCacheItem Read(UnsafeReader reader)
            {
                var flag = reader.ReadBool();
                return new BinaryFileCacheItem(flag);
            }

            private static void Write(UnsafeWriter writer, BinaryFileCacheItem value)
            {
                writer.Write(value.IsBinary);
            }

            public override string ToString() => $"IsBinary: {IsBinary}";
        }
    }
}