using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace UnityEngine
{
    public class Debug
    {
        public static void Log(object message)
        {
        }
    }
}

namespace Unity
{
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


        public readonly struct SharedStatic<T> where T : struct
        {
            public static SharedStatic<T> GetOrCreate<T1>(uint par1 = 0)
            {
                return new SharedStatic<T>();
            }

            public static SharedStatic<T> GetOrCreate<TContext, TSubContext>(uint par1 = 0)
            {
                return new SharedStatic<T>();
            }

            public static SharedStatic<T> GetOrCreate(Type par1, uint par2 = 0)
            {
                return new SharedStatic<T>();
            }

            public static SharedStatic<T> GetOrCreate(Type par1, Type par2, uint par3 = 0)
            {
                return new SharedStatic<T>();
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

public class NullableTests
{

    [BurstCompile]
    public struct NullableTest1 : IJob
    {
        public void Execute()
        {
            bool? a = new bool();
            bool? b = null;
            bool? c = new bool?();
            bool? d = a;
            d = b;
            d = c;
            d = true;

            if (d.HasValue)
                F(d.Value);

            d.GetHashCode();
        }

        public void F(bool bb)
        {

        }
    }
}