using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Collections;
using JetBrains.Rider.Model.Unity;
using JetBrains.Rider.Model.Unity.FrontendBackend;
using JetBrains.Util.DataStructures.Collections;
using JetBrains.Util.DataStructures.Specialized;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.Profiler;

[MustDisposeResource]
internal class PooledSamplesCache : IDisposable
{
    private static readonly ObjectPool<PooledSamplesCache> ourGlobalPool = CreatePool();
    private readonly ObjectPool<PooledSamplesCache> myPool;
    private PooledDictionary<string, PooledList<PooledSample>> myAssemblyToSamples;
    private PooledDictionary<string, PooledList<PooledSample>> myTypeNameToSamples;
    private PooledDictionary<string, PooledList<PooledSample>> myQualifiedNameToSamples;
    private PooledList<PooledSample> mySamples;
    private PooledList<ProfilerModelSample> myProfilerModelSamples;
    
    private int myFrameIndex;
    private ProfilerThread mySnapshotThread;

    public FrontendModelSnapshot GetFrontendModelSnapshot()
    {
        return new FrontendModelSnapshot(myProfilerModelSamples, new SelectionState(myFrameIndex, mySnapshotThread));
    }
    
    private PooledSamplesCache(ObjectPool<PooledSamplesCache> pool)
    {
        myPool = pool;
    }

    public void Dispose()
    {
        if (mySamples != null)
        {
            mySamples.Dispose();
            mySamples = null;
        }

        if (myProfilerModelSamples != null)
        {
            myProfilerModelSamples.Dispose();
            myProfilerModelSamples = null;
        }

        if (myAssemblyToSamples != null)
        {
            foreach (var (_, list) in myAssemblyToSamples)
                list.Dispose();
            myAssemblyToSamples.Clear();
            myAssemblyToSamples.Dispose();
            myAssemblyToSamples = null;
        }

        if (myTypeNameToSamples != null)
        {
            foreach (var (_, list) in myTypeNameToSamples)
                list.Dispose();
            myTypeNameToSamples.Clear();
            myTypeNameToSamples.Dispose();
            myTypeNameToSamples = null;
        }
        
        if (myQualifiedNameToSamples != null)
        {
            foreach (var (_, list) in myQualifiedNameToSamples)
                list.Dispose();
            myQualifiedNameToSamples.Clear();
            myQualifiedNameToSamples?.Dispose();
            myQualifiedNameToSamples = null;
        }

        myPool.Return(this);
    }

    [Pure]
    public static ObjectPool<PooledSamplesCache> CreatePool()
    {
        return new ObjectPool<PooledSamplesCache>(p => new PooledSamplesCache(p));
    }

    [Pure]
    [MustDisposeResource]
    public static PooledSamplesCache GetInstance(int frameIndex = -1, [CanBeNull] ProfilerThread snapshotThread = null)
    {
        var pooledSamplesCache = ourGlobalPool.Allocate();

        pooledSamplesCache.mySamples = PooledList<PooledSample>.GetInstance();
        pooledSamplesCache.myProfilerModelSamples = PooledList<ProfilerModelSample>.GetInstance();
        pooledSamplesCache.myAssemblyToSamples = PooledDictionary<string, PooledList<PooledSample>>.GetInstance();
        pooledSamplesCache.myTypeNameToSamples = PooledDictionary<string, PooledList<PooledSample>>.GetInstance();
        pooledSamplesCache.myQualifiedNameToSamples = PooledDictionary<string, PooledList<PooledSample>>.GetInstance();
        
        pooledSamplesCache.myFrameIndex = frameIndex;
        pooledSamplesCache.mySnapshotThread = snapshotThread ?? new ProfilerThread(-1, string.Empty);
        
        return pooledSamplesCache;
    }

    public bool TryGetSamplesByQualifiedName(string qualifiedName, ref IList<PooledSample> samples)
    {
        if (!myQualifiedNameToSamples.TryGetValue(qualifiedName, out var qualifiedNameSamples))
            return false;

        foreach (var sample in qualifiedNameSamples)
            samples.Add(sample);
        return true;
    }
    
    public bool TryGetTypeSamples(string typeName, ref IList<PooledSample> samples)
    {
        if (!myTypeNameToSamples.TryGetValue(typeName, out var qualifiedNameSamples))
            return false;

        foreach (var sample in qualifiedNameSamples)
            samples.Add(sample);
        return true;
    }

    public void RegisterSample(PooledSample sample)
    {
        mySamples.Add(sample);

        var dllName = sample.AssemblyName;

        if (!myAssemblyToSamples.TryGetValue(dllName, out var dllSamples))
            // ReSharper disable once NotDisposedResource
            myAssemblyToSamples.Add(dllName, dllSamples = PooledList<PooledSample>.GetInstance());

        dllSamples.Add(sample);
        
        var typeName = sample.TypeName;
        if (!myTypeNameToSamples.TryGetValue(typeName, out var typeSamples))
            // ReSharper disable once NotDisposedResource
            myTypeNameToSamples.Add(typeName, typeSamples = PooledList<PooledSample>.GetInstance());

        typeSamples.Add(sample);

        var qualifiedName = sample.QualifiedName;
        if (!myQualifiedNameToSamples.TryGetValue(qualifiedName, out var qualifiedNameSamples))
            // ReSharper disable once NotDisposedResource
            myQualifiedNameToSamples.Add(qualifiedName, qualifiedNameSamples = PooledList<PooledSample>.GetInstance());

        qualifiedNameSamples.Add(sample);
    }

    public void UpdateFrontendModelSamples()
    {
        foreach (var sample in mySamples)
            myProfilerModelSamples.Add(sample.ToProfilerModelSample());
    }
}