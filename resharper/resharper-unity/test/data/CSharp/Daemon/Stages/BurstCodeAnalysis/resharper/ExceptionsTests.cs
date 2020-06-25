using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
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


namespace ExceptionsTests
{
    public class ExceptionsTests 
    {
        [BurstCompile]
        struct ExceptionsTest1 : IJob
        {
            public void Execute()
            {
                NoWarnings();
                ContainsWarning();
            }

            private void NoWarnings()
            {
                throw new ArgumentException(new object().ToString());
                throw new ArgumentException("exception");
            }

            private void ContainsWarning()
            {
                new ArgumentException(nameof(ContainsWarning));
            }
        }

        [BurstCompile]
        struct ExceptionsTest2 : IJob
        {
            public void Execute()
            {
                try
                {
                }
                catch (Exception e)
                {
                }
                finally
                {
                }
            }
        }

        [BurstCompile]
        struct ExceptionsTest3 : IJob
        {
            public void Execute()
            {
                try
                {
                }
                finally
                {
                }
            }
        }

        [BurstCompile]
        struct ExceptionsTest4 : IJob
        {
            public void Execute()
            {
                try
                {
                }
                catch (ArgumentException e) 
                {
                }
                catch (NullReferenceException e)
                {
                }
                finally
                {
                }
            }
        }

        [BurstCompile]
        struct ExceptionsTest5 : IJob
        {
            public void Execute()
            {
                foreach (var i in new NativeArray<int>())
                {
                    Debug.Log(i);
                }
            }
        }
    }
}