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
                new ArgumentException(nameof(ContainsWarning)); //Burst error BC1021: Creating a managed object `here placed object ref' is not supported
            }
        }

        [BurstCompile]
        struct ExceptionsTest2 : IJob
        {
            public void Execute()
            {
                // very strange, but burst compiler places errors on line 41 and 42 (open and close brackets).
                // I decided to place them on keywors, imo it's better
                try //Burst error BC1005: The `try` construction is not supported.
                {
                }
                catch (Exception e) //Burst error BC1037: The `catch` construction (e.g `foreach`/`using`) is not supported.
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
                try //Burst error BC1005: The `try` construction is not supported
                {
                }
                finally //Burst error BC1037: The `finally` construction (e.g `foreach`/`using`) is not supported.
                {
                }
            }
        }

        [BurstCompile]
        struct ExceptionsTest4 : IJob
        {
            public void Execute()
            {
                try //Burst error BC1005: The `try` construction is not supported
                {
                }
                catch (ArgumentException e) //Burst error BC1037: The `finally` construction (e.g `foreach`/`using`) is not supported.
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
                // also here is placed BC1005, but I think BC1037 is sufficient
                foreach (var i in new NativeArray<int>()) //Burst error BC1037: The `try` construction (e.g `foreach`/`using`) is not supported.
                {
                    Debug.Log(i);
                }
            }
        }
    }
}