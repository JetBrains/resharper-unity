#nullable enable

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.Rider.Model.Unity.BackendUnity;
using JetBrains.Util;
using JetBrains.Util.DataStructures.Collections;
using JetBrains.Util.DataStructures.Specialized;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.Profiler;

internal static class SamplesCacheUtils
{
    private static readonly ObjectPool<PooledStack<PooledSample>> ourSamplesStackPool = PooledStack<PooledSample>.CreatePool();

    private static readonly ObjectPool<PooledDictionary<int, string>> ourIdNameDictionaryPool = PooledDictionary<int, string>.CreatePool();

    // Constants for string comparisons
    private const string InvokeConst = "[Invoke]";
    private const string BracketsConst = "()";

    // Helper methods to get spans for string constants to avoid allocations when comparing
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ReadOnlySpan<char> GetInvokeSpan() => InvokeConst.AsSpan();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ReadOnlySpan<char> GetBracketsSpan() => BracketsConst.AsSpan();

    /// <summary>
    /// Constructs a cache of profiler samples from a Unity profiler snapshot.
    /// This method is optimized for performance and memory usage.
    /// </summary>
    /// <param name="snapshot">The Unity profiler snapshot to process</param>
    /// <param name="cacheUpdatingProgress">Optional progress reporter</param>
    /// <returns>A pooled cache of samples</returns>
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

        // Pre-allocate capacity to avoid resizing
        // Note: EnsureCapacity is not available in .NET Framework 4.7.2
        // We'll rely on the Dictionary's automatic resizing

        foreach (var (id, name) in snapshot.MarkerIdToName) 
            markerIdToName.Add(id, name);

        var samplesCount = snapshot.Samples.Count;
        // Avoid division by zero
        var batchSize = samplesCount > 100 ? samplesCount / 100 : 1;

        // Cache frame time to avoid repeated division
        var frameTimeMs = snapshot.FrameTimeMs;

        // Cache samples collection to avoid repeated property access
        var samples = snapshot.Samples;

        // Process samples in batches for better cache locality
        for (var index = 0; index < samplesCount; index++)
        {
            if(index % batchSize == 0)
                cacheUpdatingProgress?.Report(index / (double) samplesCount);

            var sampleInfo = samples[index];

            // Use TryGetValue with out parameter to avoid extra lookup
            string sampleName;
            if (!markerIdToName.TryGetValue(sampleInfo.MarkerId, out sampleName))
                sampleName = EmptyString;

            var parsingResult = ParseSampleName(sampleName);
            var childrenCount = sampleInfo.ChildrenCount;

            var sample = PooledSample.GetInstance(parsingResult.QualifiedName, parsingResult.TypeName,
                parsingResult.AssemblyName, sampleInfo.Duration,
                sampleInfo.Duration / frameTimeMs, sampleInfo.MarkerId, childrenCount, sampleInfo.MemoryAllocation);

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
                //no children in the sample
                sample.ShareMemoryAllocationsWithParent();

                // Use a constant for the sanity check to avoid magic numbers
                const int maxIterations = 1000000;
                var sanityCheck = maxIterations;

                while (stack.Count > 0 && --sanityCheck > 0)
                {
                    var topSample = stack.Peek();
                    var remainingChildren = topSample.ChildrenCount - topSample.Children.Count;

                    Assertion.Assert(remainingChildren >= 0);
                    //waiting for other children
                    if (remainingChildren >= 1)
                        break;

                    topSample.ShareMemoryAllocationsWithParent();                  
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

    // Cache for frequently used empty strings to avoid allocations
    private static readonly string EmptyString = string.Empty;

    // Use MethodImpl to suggest inlining for this performance-critical method
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static SampleParsingResult ParseSampleName(string sampleName)
    {
        //AssemblyName.dll!NamespaceName::ClassName.MethodName() [Invoke]
        //real examples: 
        //  Assembly-CSharp.dll!MyNamespace2::HeavyScript2.get_GetName() [Invoke]
        //  Assembly-CSharp.dll!MyNamespace2::HeavyScript2.Update() 

        // Fast path for empty or null strings
        if (string.IsNullOrEmpty(sampleName))
            return new SampleParsingResult(EmptyString, EmptyString, EmptyString);

        var slice = new StringSlice(sampleName);

        // Extract dllName
        string dllName;
        var exclamationIndex = slice.IndexOf('!');
        if (exclamationIndex < 0)
        {
            dllName = EmptyString;
        }
        else
        {
            dllName = slice.Substring(0, exclamationIndex).ToString();
            slice = slice.Substring(exclamationIndex + 1);
        }

        // Process qualifiedName
        var doubleColonIndex = slice.IndexOf("::");
        if (doubleColonIndex == 0)
        {
            // Empty namespace
            slice = slice.Substring(2);
        }
        else if (doubleColonIndex > 0)
        {
            // Replace :: with .
            using var sb = PooledStringBuilder.GetInstance();
            var startIndex = 0;

            // Pre-allocate StringBuilder capacity based on input length to avoid resizing
            // Add extra capacity for potential '.' replacements
            sb.Builder.EnsureCapacity(slice.Length + 10);

            while (true)
            {
                doubleColonIndex = slice.IndexOf("::", startIndex);
                if (doubleColonIndex < 0)
                {
                    sb.Append(slice.Substring(startIndex).ToString());
                    break;
                }

                sb.Append(slice.Substring(startIndex, doubleColonIndex - startIndex).ToString());
                sb.Append('.');
                startIndex = doubleColonIndex + 2;
            }

            slice = new StringSlice(sb.ToString());
        }

        // Use ReadOnlySpan for string comparisons to avoid allocations
        // Remove [Invoke] if present
        var invokeSpan = GetInvokeSpan();
        if (slice.Length >= invokeSpan.Length)
        {
            var sliceEnd = slice.ToString().AsSpan().Slice(slice.Length - invokeSpan.Length);
            if (sliceEnd.SequenceEqual(invokeSpan))
            {
                slice = slice.Substring(0, slice.Length - invokeSpan.Length).Trim();
            }
        }

        // Remove () if present
        var bracketsSpan = GetBracketsSpan();
        if (slice.Length >= bracketsSpan.Length)
        {
            var sliceEnd = slice.ToString().AsSpan().Slice(slice.Length - bracketsSpan.Length);
            if (sliceEnd.SequenceEqual(bracketsSpan))
            {
                slice = slice.Substring(0, slice.Length - bracketsSpan.Length);
            }
        }

        // Trim and get final qualifiedName
        slice = slice.Trim();
        var qualifiedName = slice.ToString();

        // Extract typeName
        var lastDotIndex = slice.LastIndexOf('.');
        var typeName = lastDotIndex > 0 ? slice.Substring(0, lastDotIndex).ToString() : EmptyString;

        return new SampleParsingResult(dllName, qualifiedName, typeName);
    }
}
