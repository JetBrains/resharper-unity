#nullable enable

using System;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.Rider.Model.Unity.BackendUnity;
using JetBrains.Util.DataStructures.Collections;
using JetBrains.Util.DataStructures.Specialized;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.Profiler;

internal static class SamplesCacheUtils
{
    private static readonly ObjectPool<PooledStack<PooledSample>> ourSamplesStackPool = PooledStack<PooledSample>.CreatePool();

    private static readonly ObjectPool<PooledDictionary<int, string>> ourIdNameDictionaryPool = PooledDictionary<int, string>.CreatePool();

    [MustDisposeResource]
    public static PooledSamplesCache ConstructCache(UnityProfilerSnapshot? snapshot,
        IProgress<double>? cacheUpdatingProgress = null)
    {
        if(snapshot == null)
            return PooledSamplesCache.GetInstance();
        
        var samplesCache = PooledSamplesCache.GetInstance();
        samplesCache.SnapshotInfo = snapshot.Status;

        using var stack = ourSamplesStackPool.Allocate();
        using var markerIdToName = ourIdNameDictionaryPool.Allocate();
        foreach (var (id, name) in snapshot.MarkerIdToName) 
            markerIdToName.Add(id, name);

        var samplesCount = snapshot.Samples.Count;
        var batchSize = samplesCount / 100;
        for (var index = 0; index < samplesCount; index++)
        {
            if(index % batchSize == 0)
                cacheUpdatingProgress?.Report(index / (double) samplesCount);
            
            var sampleInfo = snapshot.Samples[index];
            if (!markerIdToName.TryGetValue(sampleInfo.MarkerId, out var sampleName))
                sampleName = string.Empty;

            var parsingResult = ParseSampleName(sampleName);
            var childrenCount = sampleInfo.ChildrenCount;

            var sample = PooledSample.GetInstance(parsingResult.QualifiedName, parsingResult.TypeName,
                parsingResult.AssemblyName, sampleInfo.Duration,
                sampleInfo.Duration / snapshot.FrameTimeMs, sampleInfo.MarkerId, childrenCount);

            //Add to cache collections
            samplesCache.RegisterSample(sample);

            //Process children
            if (stack.Count > 0)
            {
                //Link with parent
                var topSample = stack.Peek();
                topSample.AddChild(sample);
            }

            if (childrenCount > 0)
                stack.Push(sample);
            else
            {
                var sanityCheck = 1000000;
                while (stack.Count > 0 && --sanityCheck > 0)
                {
                    var topSample = stack.Peek();
                    var remainingChildren = topSample.ChildrenCount - topSample.Children.Count;

                    Assertion.Assert(remainingChildren >= 0);
                    //waiting for other children
                    if (remainingChildren >= 1)
                        break;

                    stack.Pop();
                }

                Assertion.Assert(sanityCheck > 0, "Possible infinite loop");
            }
        }

        return samplesCache;
    }

    private readonly struct SampleParsingResult(string assemblyName, string qualifiedName, string typeName)
    {
        public readonly string AssemblyName = assemblyName;
        public readonly string QualifiedName = qualifiedName;
        public readonly string TypeName = typeName;
    }
    private static SampleParsingResult ParseSampleName(string sampleName)
    {
        //AssemblyName.dll!NamespaceName::ClassName.MethodName() [Invoke]
        //real examples: 
        //  Assembly-CSharp.dll!MyNamespace2::HeavyScript2.get_GetName() [Invoke]
        //  Assembly-CSharp.dll!MyNamespace2::HeavyScript2.Update() 

        var parts = sampleName.Split('!');
        var dllName = parts[0];
        if (parts.Length == 1)
            dllName = string.Empty;

        var qualifiedName = parts[parts.Length - 1]; //get the last part

        qualifiedName = qualifiedName.StartsWith("::")
            ? qualifiedName.Substring(2, qualifiedName.Length - 2) //empty namespace
            : qualifiedName.Replace("::", "."); //replace :: with .


        var invoke = "[Invoke]";
        if (qualifiedName.EndsWith(invoke))
            qualifiedName = qualifiedName.Substring(0, qualifiedName.Length - invoke.Length).Trim();

        var brackets = "()";
        if (qualifiedName.EndsWith(brackets))
            qualifiedName = qualifiedName.Substring(0, qualifiedName.Length - brackets.Length);

        // #error wrong type parsing
        qualifiedName = qualifiedName.Trim();
        var typeName = qualifiedName.SubstringOrEmpty(0, qualifiedName.LastIndexOf('.'));
        return new SampleParsingResult(dllName, qualifiedName, typeName);
    }
}