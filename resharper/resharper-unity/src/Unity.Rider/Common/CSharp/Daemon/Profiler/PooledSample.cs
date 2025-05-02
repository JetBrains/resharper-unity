#nullable enable
using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.Util;
using JetBrains.Util.DataStructures.Collections;
using JetBrains.Util.DataStructures.Specialized;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.Profiler;

[MustDisposeResource]
public class PooledSample : IDisposable
{
    private static readonly ObjectPool<PooledSample> ourGlobalPool = CreatePool();
    private readonly ObjectPool<PooledSample> myPool;
    private PooledList<PooledSample>? myChildren;

    private PooledSample(ObjectPool<PooledSample> pool)
    {
        myPool = pool;
    }

    //this class doesn't own this object
    public PooledSample? Parent { get; private set; }

    //this class doesn't own the objects in the list
    public PooledList<PooledSample> Children
    {
        get => myChildren.NotNull("Object was disposed");
        private set => myChildren = value;
    }
    
    private bool myMemoryIsSharedWithParent = false;

    public int ChildrenCount { get; private set; }
    public string QualifiedName { get; private set; }
    public string TypeName { get; private set; }
    public string AssemblyName { get; private set; }
    public double Duration { get; private set; }
    public double FramePercentage { get; private set; }
    public long MemoryAllocation { get; private set; }
    public int Id { get; private set; }
    public bool IsProfilerMarker => Id < 0; //Unity marks BeginSample/EndSample with negative Id

    protected bool Equals(PooledSample other)
    {
        return Id == other.Id &&
               Parent?.Id == other.Parent?.Id &&
               ChildrenCount == other.ChildrenCount &&
               QualifiedName == other.QualifiedName &&
               TypeName == other.TypeName &&
               AssemblyName == other.AssemblyName;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((PooledSample)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = QualifiedName.GetHashCode();
            hashCode = (hashCode * 397) ^ TypeName.GetHashCode();
            hashCode = (hashCode * 397) ^ AssemblyName.GetHashCode();
            hashCode = (hashCode * 397) ^ Id;
            return hashCode;
        }
    }

    public void Dispose()
    {
        if (myChildren != null)
        {
            //don't dispose internal objects, because this instances are not owned by this class
            myChildren.Clear();
            myChildren.Dispose();
            myChildren = null;
        }

        //Do not dispose Parent, because this instance doesn't own it
        Parent = null;
        
        myPool.Return(this);
    }

    [Pure]
    public static ObjectPool<PooledSample> CreatePool()
    {
        return new ObjectPool<PooledSample>(p => new PooledSample(p));
    }

    [Pure]
    [MustDisposeResource]
    public static PooledSample GetInstance()
    {
        var pooledSample = ourGlobalPool.Allocate();
        pooledSample.Children = PooledList<PooledSample>.GetInstance();
        Assertion.Assert(pooledSample.Children.IsEmpty());
        Assertion.Assert(pooledSample.Parent == null);
        return pooledSample;
    }

    [Pure, MustDisposeResource]
    public static PooledSample GetInstance(string qualifiedName, string typeName, string assemblyName,
        double sampleInfoDuration,
        double framePercentage, int sampleInfoMarkerId, int childrenCount, long memoryAllocation)
    {
        var pooledSample = GetInstance();

        pooledSample.QualifiedName = qualifiedName;
        pooledSample.AssemblyName = assemblyName;
        pooledSample.TypeName = typeName;
        pooledSample.Duration = sampleInfoDuration;
        pooledSample.FramePercentage = framePercentage;
        pooledSample.Id = sampleInfoMarkerId;
        pooledSample.ChildrenCount = childrenCount;
        pooledSample.MemoryAllocation = memoryAllocation;

        return pooledSample;
    }

    private string GetCallStack(string separator = "->")
    {
        var callStack = new List<string>();
        var current = this;
        var sanityCheck = 1000; //to avoid infinite loop
        while (current != null && --sanityCheck > 0)
        {
            callStack.Add(current.QualifiedName);
            current = current.Parent;
        }

        Assertion.Assert(sanityCheck > 0, "Possible infinite loop");

        callStack.Reverse();
        return string.Join(separator, callStack);
    }

    public override string ToString()
    {
        return $"{Id}|{QualifiedName}|{Duration}|{StringUtil.StrFormatByteSize(MemoryAllocation)}|{GetCallStack()}";
    }

    public void ShareMemoryAllocationsWithParent()
    {
        if (myMemoryIsSharedWithParent)
            throw new InvalidOperationException("Cannot share memory allocations with parent memory allocations");
        
        if(Parent != null) 
            Parent.MemoryAllocation += MemoryAllocation;
        myMemoryIsSharedWithParent = true;
    }

    public void AddChild(PooledSample sample)
    {
        Children.Add(sample);
        sample.Parent = this;
    }
}