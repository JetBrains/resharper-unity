using System;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;

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

public class SharedStaticCreateTests2 : MonoBehaviour
    {
        private static readonly Type myDoubleType = typeof(double);
        private static readonly Type myIntType = typeof(int);

        [BurstCompile]
        public struct SharedStaticTest1 : IJob
        {
            public void Execute()
            {
                var sharedStatic0 = SharedStatic<int>.GetOrCreate(myDoubleType);
            }
        }

        [BurstCompile]
        public struct SharedStaticTest4 : IJob
        {
            public void Execute()
            {
                var sharedStatic3 = SharedStatic<int>.GetOrCreate(myDoubleType, myIntType);
                F();
            }

            private void F()
            {
                var obj = new object();
            }
        }
    }