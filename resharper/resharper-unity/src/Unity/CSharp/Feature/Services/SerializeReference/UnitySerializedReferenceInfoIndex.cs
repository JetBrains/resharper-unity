#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Collections;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Daemon.UsageChecking.SwaExtension;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.Serialization;
using JetBrains.Util;
using JetBrains.Util.Collections;
using JetBrains.Util.Dotnet.TargetFrameworkIds;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.SerializeReference
{
    internal class UnitySerializedReferenceInfoIndex
    {
        private readonly SolutionAnalysisConfiguration mySolutionAnalysisConfiguration;
        private readonly ILogger myLogger;

        public UnitySerializedReferenceInfoIndex(SolutionAnalysisConfiguration solutionAnalysisConfiguration,
            ILogger logger)
        {
            mySolutionAnalysisConfiguration = solutionAnalysisConfiguration;
            myLogger = logger;
        }

        public IndexClassInfoDictionary ClassInfoDictionary { get; } = new();

        private Dictionary<FullAssemblyId, UnitySerializationReferenceElementInfo> AssemblyDictionary { get; } =
            new();

        internal void Merge(ISwaExtensionInfo oldData, ISwaExtensionInfo newData)
        {
            var oldInfo = oldData as UnitySerializationReferenceElementInfo;
            var newInfo = newData as UnitySerializationReferenceElementInfo;

            var infoDiff =
                SerializeReferenceProviderDiffUtils.CalculateDiff(oldInfo?.TypeToInterfaces, newInfo?.TypeToInterfaces);

            SerializeReferenceProviderDiffUtils.ApplyDiff(ClassInfoDictionary, infoDiff);


            var resolvesDiff =
                SerializeReferenceProviderDiffUtils.CalculateDiff(oldInfo?.TypeParameterResolves,
                    newInfo?.TypeParameterResolves,
                    (id, type, count) => new TypeParameterResolvesDiff(id, type, count));

            SerializeReferenceProviderDiffUtils.ApplyDiff(ClassInfoDictionary, resolvesDiff);
        }

        public void ClearFilesIndex()
        {
            ClassInfoDictionary.Clear();
            foreach (var (_, elementInfo) in AssemblyDictionary)
                Merge(null!, elementInfo);
        }

        public void DumpFull(TextWriter writer, ISolution solution)
        {
            void DumpCountingSet(CountingSet<ElementId> countingSet, string name)
            {
                Write(name + ": " + countingSet.Count);
                foreach (var (to, count) in countingSet.Select(kvp => kvp).OrderBy(kvp => kvp.Key.Value))
                {
                    ClassInfoDictionary.TryGetValue(to, out var info);
                    Write($"      {to}:{info?.ClassName} ({count})");
                }

                writer.WriteLine();
            }

            void Write(string s)
            {
                writer.WriteLine("       " + s);
            }

            Write($"{nameof(ClassInfoDictionary)}: {ClassInfoDictionary.Count}");

            foreach (var (elementId, indexClassInfo) in ClassInfoDictionary.Select(kvp => kvp)
                         .OrderBy(kvp => kvp.Value.ClassName))
            {
                Write(
                    $"{elementId} | {indexClassInfo.ClassName} | {nameof(indexClassInfo.IsTypeParameter)}:{indexClassInfo.IsTypeParameter}");

                DumpCountingSet(indexClassInfo.SuperClasses, nameof(indexClassInfo.SuperClasses));
                DumpCountingSet(indexClassInfo.Inheritors, nameof(indexClassInfo.Inheritors));
                DumpCountingSet(indexClassInfo.SerializeReferenceHolders,
                    nameof(indexClassInfo.SerializeReferenceHolders));
                Write("--------------------------------------------------------------------\n");
            }
        }

        internal void MergeAssemblyInfo(UnitySerializationReferenceElementInfo elementInfo,
            IPsiAssembly psiAssembly)
        {
            var fullAssemblyId = new FullAssemblyId(psiAssembly);
            var isAssemblyRegistered = AssemblyDictionary.TryGetValue(fullAssemblyId, out _);

            if (!isAssemblyRegistered)
            {
                myLogger.Info(
                    $"Adding {nameof(psiAssembly)}:{psiAssembly.Id.Mvid}:{fullAssemblyId}|{fullAssemblyId}");
                AssemblyDictionary.Add(fullAssemblyId, elementInfo);
                Merge(null!, elementInfo);
            }
            else
            {
                myLogger.Warn(
                    $"Already added: {nameof(psiAssembly)}:{psiAssembly.Id.Mvid}:{fullAssemblyId}|{fullAssemblyId}");
            }
        }

        public void DropAssembliesTypeInfo(IEnumerable<IPsiAssembly> assemblies)
        {
            foreach (var assembly in assemblies)
            {
                var fullAssemblyId = new FullAssemblyId(assembly);
                var isAssemblyRegistered = AssemblyDictionary.TryGetValue(fullAssemblyId, out var assemblyTypeInfo);

                if (isAssemblyRegistered)
                {
                    myLogger.Info(
                        $"Dropping assembly info: {assembly.Id.Mvid}:{fullAssemblyId}|{nameof(isAssemblyRegistered)}:{isAssemblyRegistered}");
                    AssemblyDictionary.Remove(fullAssemblyId);
                    Merge(assemblyTypeInfo!, null!);
                }
                else
                {
                    myLogger.Warn($"Can't drop assembly info, assembly is not registered: {fullAssemblyId}");
                }
            }
        }

        public SerializedFieldStatus IsTypeSerializable(ElementId elementId, bool useSwea, HashSet<ElementId> visitedNodes = null)
        {
            if(!elementId.IsCompiledElementId)
            {
                if(!useSwea || !mySolutionAnalysisConfiguration.Completed.Value)
                    return SerializedFieldStatus.Unknown; //if swea is not completed
            }
            
            if (visitedNodes != null && visitedNodes.Contains(elementId))
                return SerializedFieldStatus.NonSerializedField;
            
            if (ClassInfoDictionary.TryGetValue(elementId, out var info))
            {
                visitedNodes ??= new HashSet<ElementId>();

                if (info.SerializeReferenceHolders.Count > 0)
                    return SerializedFieldStatus.SerializedField;

                visitedNodes.Add(elementId);
                
                foreach (var (id, _) in info.SuperClasses)
                {
                    if (IsTypeSerializable(id, useSwea, visitedNodes) == SerializedFieldStatus.SerializedField)
                        return SerializedFieldStatus.SerializedField;
                }

                foreach (var (id, _) in info.Inheritors)
                {
                    if (IsTypeSerializable(id, useSwea, visitedNodes) == SerializedFieldStatus.SerializedField)
                        return SerializedFieldStatus.SerializedField;
                }

                return SerializedFieldStatus.NonSerializedField;
            }

            return SerializedFieldStatus.Unknown;
        }
    }

    internal readonly struct FullAssemblyId
    {
        private readonly string myPSIAssemblyLocation;
        private readonly TargetFrameworkId myPSIModuleTargetFrameworkId;
        private readonly AssemblyId myPSIAssemblyId;


        private FullAssemblyId(string psiAssemblyLocation, TargetFrameworkId psiModuleTargetFrameworkId, AssemblyId psiAssemblyId)
        {
            myPSIAssemblyLocation = psiAssemblyLocation;
            myPSIModuleTargetFrameworkId = psiModuleTargetFrameworkId;
            myPSIAssemblyId = psiAssemblyId;
        }

        public FullAssemblyId(IPsiAssembly assembly)
        {
            myPSIAssemblyLocation = assembly.Location!.Name;
            myPSIAssemblyId = assembly.Id;
            myPSIModuleTargetFrameworkId = assembly.PsiModule.TargetFrameworkId;
        }

        public override string ToString()
        {
            return
                $"{nameof(myPSIAssemblyLocation)}: {myPSIAssemblyLocation}, {nameof(myPSIModuleTargetFrameworkId)}: {myPSIModuleTargetFrameworkId}, {nameof(myPSIAssemblyId)}: {myPSIAssemblyId}";
        }

        public bool Equals(FullAssemblyId other)
        {
            return myPSIAssemblyLocation.Equals(other.myPSIAssemblyLocation) &&
                   myPSIModuleTargetFrameworkId.Equals(other.myPSIModuleTargetFrameworkId) &&
                   myPSIAssemblyId.Equals(other.myPSIAssemblyId);
        }

        public override bool Equals(object? obj)
        {
            return obj is FullAssemblyId other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = myPSIAssemblyLocation.GetHashCode();
                hashCode = (hashCode * 397) ^ myPSIModuleTargetFrameworkId.GetHashCode();
                hashCode = (hashCode * 397) ^ myPSIAssemblyId.GetHashCode();
                return hashCode;
            }
        }
        
        public static readonly IUnsafeMarshaller<FullAssemblyId> AssemblyIdMarshaller = new UniversalMarshaller<FullAssemblyId>(Read, Write);

        private static FullAssemblyId Read(UnsafeReader reader)
        {
            var locationName = reader.ReadString();
            var targetFrameworkId = TargetFrameworkId.Read(reader);
            var assemblyId = AssemblyId.AssemblyIdMarshaller.Unmarshal(reader);
            return new FullAssemblyId(locationName, targetFrameworkId, assemblyId);
        }

        private static void Write(UnsafeWriter writer, FullAssemblyId id)
        {
            writer.Write(id.myPSIAssemblyLocation);
            id.myPSIModuleTargetFrameworkId.Write(writer);
            AssemblyId.AssemblyIdMarshaller.Marshal(writer, id.myPSIAssemblyId);
        }
    }
}