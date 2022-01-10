using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.UnityEngine;
using Unity.Entities;

namespace Unity
{
    namespace Entities
    {
        public interface IComponentData
        {
        }
    }
    
    namespace Jobs
    {
        [JobProducerType]
        public interface IJob
        {
            void Execute();
        }

        namespace LowLevel
        {
            namespace Unsafe
            {
                public class JobProducerTypeAttribute : Attribute
                {
                }
            }
        }
    }

    namespace Burst
    {
        public class BurstCompileAttribute : Attribute
        {
        }

        public class BurstDiscardAttribute : Attribute
        {
        }
    }

    namespace UnityEngine
    {
        public class Debug
        {
            public static void Log(object message)
            {
            }
        }
    }

    namespace Collections
    {
        public struct NativeArray<T> : IDisposable, IEnumerable<T>, IEnumerable, IEquatable<NativeArray<T>>
            where T : struct
        {
            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public IEnumerator<T> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public bool Equals(NativeArray<T> other)
            {
                throw new NotImplementedException();
            }
        }
    }
}


public struct ComponentDataType : IComponentData
{
        
}
    
public interface IFoo
{
    public abstract ComponentDataFromEntity<ComponentDataType> GetAbstractProperty { get; set; }
}

public struct FooImpl : IFoo
{
    public ComponentDataFromEntity<ComponentDataType> GetAbstractProperty { get; set; }
}
    
[BurstCompile(CompileSynchronously = true)]
internal struct OneMoreJob : IJob
{
    public void Execute()
    {
        var foo = new FooImpl();
        var abstr = foo.GetAbstractProperty;
        Foo(foo);
    }

    [BurstCompile()]
    public static void Foo<T>(in T bar) where T : struct, IFoo
    {
        ComponentDataFromEntity<ComponentDataType> res = bar.GetAbstractProperty;
    }
}   