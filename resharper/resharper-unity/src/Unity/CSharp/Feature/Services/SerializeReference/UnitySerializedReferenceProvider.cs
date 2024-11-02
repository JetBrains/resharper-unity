#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Application.Parts;
using JetBrains.Application.PersistentMap;
using JetBrains.Application.Progress;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Utils;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Caches;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Daemon.UsageChecking.SwaExtension;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Impl.Reflection2;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;
using JetBrains.Util.Caches;
using JetBrains.Util.Logging;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.SerializeReference
{
    public interface IUnitySerializedReferenceProvider
    {
        void DumpFull(TextWriter writer, ISolution solution);
        SerializedFieldStatus GetSerializableStatus(ITypeElement? type, bool useSwea);
    }

    [SolutionComponent(Instantiation.DemandAnyThreadUnsafe)]
    public class UnitySerializedReferenceProvider : SwaExtensionProviderBase, IAssemblyCache,
        IUnitySerializedReferenceProvider
    {
        private const string NAME = "UnitySerializedReferenceProvider";
        private readonly UnitySerializedReferenceInfoIndex myIndex;

        private readonly object myLockObject = new();
        private static readonly ILogger ourLogger = Logger.GetLogger<UnitySerializedReferenceProvider>();
        private readonly IUnityElementIdProvider myProvider;
        private readonly IPsiAssemblyFileLoader myPsiAssemblyFileLoader;
        private readonly IPsiModules myPsiModules;
        private readonly UnitySolutionTracker myUnitySolutionTracker;


        private readonly OptimizedPersistentSortedMap<FullAssemblyId, UnitySerializationReferenceElementInfo> myShellDataMap;
        private readonly OptimizedPersistentSortedMap<FullAssemblyId, long> myShellTimestampMap;

        private readonly OptimizedPersistentSortedMap<FullAssemblyId, UnitySerializationReferenceElementInfo> mySolutionDataMap;
        private readonly OptimizedPersistentSortedMap<FullAssemblyId, long> mySolutionTimestampMap;

        public UnitySerializedReferenceProvider(Lifetime lifetime,
            IUnityElementIdProvider provider,
            IPsiAssemblyFileLoader psiAssemblyFileLoader,
            IPsiModules psiModules, UnitySolutionTracker unitySolutionTracker,
            SolutionAnalysisConfiguration solutionAnalysisConfiguration,
            ShellCaches shellCaches,
            ISolutionCaches solutionCaches)
            : base(NAME, false)
        {
            myProvider = provider;
            myPsiAssemblyFileLoader = psiAssemblyFileLoader;
            myPsiModules = psiModules;
            myUnitySolutionTracker = unitySolutionTracker;
            myIndex = new UnitySerializedReferenceInfoIndex(solutionAnalysisConfiguration, ourLogger);
            GetCaches(lifetime, shellCaches.Db, out myShellDataMap, out myShellTimestampMap);
            GetCaches(lifetime, solutionCaches.Db, out mySolutionDataMap, out mySolutionTimestampMap);
            
            Enabled.Value = unitySolutionTracker.IsUnityProject.HasTrueValue();
            unitySolutionTracker.HasUnityReference.Advise(lifetime, b => Enabled.Value |= b);
        }

        private const string PersistentId = "AssemblySerializeReferenceCache";

        protected virtual bool IsUnitySolution =>
            myUnitySolutionTracker.IsUnityProject.Value; //virtual - to overload for testValue.ClassNames

        private void GetCaches(Lifetime lifetime, IKeyValueDb db,
            out OptimizedPersistentSortedMap<FullAssemblyId, UnitySerializationReferenceElementInfo> dataMap,
            out OptimizedPersistentSortedMap<FullAssemblyId, long> timestampMap)
        {
            var map = db.GetMap(PersistentId, FullAssemblyId.AssemblyIdMarshaller, UnitySerializationReferenceElementInfo.SerializeRefInfoMarshaller);
            dataMap = new OptimizedPersistentSortedMap<FullAssemblyId, UnitySerializationReferenceElementInfo>(lifetime, map)
            {
                Cache = new UnlimitedCache<FullAssemblyId, UnitySerializationReferenceElementInfo>(),
                UseCachingEnumerator =  true
            };

            var persistentSortedMap = db.GetMap(PersistentId + "Timestamp", FullAssemblyId.AssemblyIdMarshaller, UnsafeMarshallers.LongMarshaller);
            timestampMap = new OptimizedPersistentSortedMap<FullAssemblyId, long>(lifetime, persistentSortedMap)
            {
                Cache = new UnlimitedCache<FullAssemblyId, long>(),
                UseCachingEnumerator =  true
            };
        }

        object? ICache.Load(IProgressIndicator progress, bool enablePersistence) => null;

        void ICache.MergeLoaded(object data) { }

        void ICache.Save(IProgressIndicator progress, bool enablePersistence)
        {
            //TODO
        }

        record AssemblyCache(
            OptimizedPersistentSortedMap<FullAssemblyId, UnitySerializationReferenceElementInfo> DataMap,
            OptimizedPersistentSortedMap<FullAssemblyId, long> TimestampMap);

        private AssemblyCache ShellCache => new AssemblyCache(myShellDataMap, myShellTimestampMap);
        private AssemblyCache SolutionCache =>  new AssemblyCache(mySolutionDataMap, mySolutionTimestampMap);
        private AssemblyCache GetAssemblyCache(IPsiAssembly assembly) => assembly.IsFrameworkAssembly ? ShellCache : SolutionCache;
        object? IAssemblyCache.Build(IPsiAssembly assembly)
        {
            if (!IsUnitySolution)
                return null;
            if (!IsValidAssembly(assembly))
                return null;

            if (myPsiModules.HasSourceProject(assembly))
                return null;

            UnitySerializationReferenceElementInfo? result = null;

            myPsiAssemblyFileLoader.GetOrLoadAssembly(assembly, true,
                (_, assemblyFile, metadataAssembly) =>
                {
                    if (assemblyFile is not AssemblyPsiFile)
                        result = null;
                    else if (metadataAssembly.AssemblyName == null)
                        result = null;
                    else
                    {
                        var currentTimestamp = assemblyFile.Timestamp.Ticks;

                        var fullAssemblyId = new FullAssemblyId(assembly);
                        var (dataMap, timestampMap) = GetAssemblyCache(assembly);
                        if (timestampMap.TryGetValue(fullAssemblyId, out var cachedTimestamp) &&
                            cachedTimestamp == currentTimestamp)
                        {
                            dataMap.TryGetValue(fullAssemblyId, out result);
                        }

                        if(result == null)
                        {
                            result = ProcessAssemblyFile(assemblyFile, metadataAssembly);
                            dataMap[fullAssemblyId] = result;
                            timestampMap[fullAssemblyId] = currentTimestamp;
                        }
                    }
                });

            return result; //result == null in case if assembly removed
        }

        void IAssemblyCache.Merge(IPsiAssembly assembly, object part, Func<bool> checkForTermination)
        {
            if (!IsUnitySolution) return;
            if (part is not UnitySerializationReferenceElementInfo referenceElementInfo)
                return;

            if (referenceElementInfo.IsEmpty())
            {
                return;
            }

            lock (myLockObject)
            {
                myIndex.MergeAssemblyInfo(referenceElementInfo, assembly);
            }
        }

        void IAssemblyCache.Drop(IEnumerable<IPsiAssembly> assemblies)
        {
            if (!IsUnitySolution) return;
            lock (myLockObject)
            {
                myIndex.DropAssembliesTypeInfo(assemblies);
                foreach (var assembly in assemblies)
                {
                    if(assembly.IsFrameworkAssembly)
                        continue;  // Do not drop framework assembly - it can be used by other solutions

                    var (dataMap, timestampMap) = GetAssemblyCache(assembly);
                    var fullAssemblyId = new FullAssemblyId(assembly);
                    dataMap.Remove(fullAssemblyId);
                    timestampMap.Remove(fullAssemblyId);
                }
            }
        }

        void IUnitySerializedReferenceProvider.DumpFull(TextWriter writer, ISolution solution)
        {
            if (!IsUnitySolution) return;
            lock (myLockObject)
            {
                myIndex.DumpFull(writer, solution);
            }
        }

        public SerializedFieldStatus GetSerializableStatus(ITypeElement? type, bool useSwea)
        {
            var elementId = myProvider.GetElementId(type);
            if (elementId == null)
                return SerializedFieldStatus.NonSerializedField; //can't get information about the type, multidimensional array handles here too (TODO? why?)

            lock (myLockObject)
                return myIndex.IsTypeSerializable(elementId.Value, useSwea);
        }

        protected virtual bool IsValidAssembly(IPsiAssembly assembly)
        {
            return true;
        }

        public override ISwaExtensionData CreateUsageDataElement(UsageData owner)
        {
            return new UnitySerializationReferenceElementData(myProvider);
        }

        public override void Merge(ISwaExtensionInfo oldData, ISwaExtensionInfo newData)
        {
            if (!IsUnitySolution) return;
            lock (myLockObject)
            {
                myIndex.Merge(oldData, newData);
            }
        }

        public override void Clear()
        {
            if (!IsUnitySolution) return;
            lock (myLockObject)
            {
                myIndex.ClearFilesIndex();
            }
        }

        public override bool IsApplicable(IPsiSourceFile psiSourceFile)
        {
            return IsUnitySolution && psiSourceFile.PrimaryPsiLanguage.Is<CSharpLanguage>();
        }

        private UnitySerializationReferenceElementInfo ProcessAssemblyFile(IPsiAssemblyFile assemblyFile,
            IMetadataAssembly metadataAssembly)
        {
            var result = new UnitySerializationReferenceElementInfo();

            foreach (var metadataTypeInfo in metadataAssembly.GetTypes())
            {
                if (metadataTypeInfo.IsModuleType())
                    continue;

                var classInfoAdapter = metadataTypeInfo.ToAdapter(assemblyFile, myProvider);
                SerializeReferenceTypesUtils.CollectClassData(classInfoAdapter, result.TypeToInterfaces,
                    result.TypeParameterResolves);
            }

            return result;
        }
    }
}
