#define ENABLE_UNITY_COLLECTIONS_CHECKS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Collections.LowLevel.Unsafe;
using Unity.UnityEngine;

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
        namespace LowLevel
        {
            namespace Unsafe
            {
                public sealed class NativeSetClassTypeToNullOnScheduleAttribute : Attribute
                {
                }
            }
        }

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

namespace DirectivesTests
{
    public class DirectivesTests
    {
        [StructLayout(LayoutKind.Sequential)]
        unsafe public struct NativeCounter
        {
            int* m_Counter;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // without this attribute Burst fails!
            [NativeSetClassTypeToNullOnScheduleAttribute] AtomicSafetyHandle m_Safety;

            DisposeSentinel m_DisposeSentinel;
#endif

            Allocator m_AllocatorLabel;

            public NativeCounter(Allocator label)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (!UnsafeUtility.IsBlittable<int>())
                    throw new ArgumentException(
                        string.Format("{0} used in NativeQueue<{0}> must be blittable", typeof(int)));
#endif
                m_AllocatorLabel = label;

                m_Counter = (int*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<int>(), 4, label);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 0, label);
#endif
                Count = 0;
            }

            public void Increment()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                (*m_Counter)++;
            }

            public int Count
            {
                get
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                    return *m_Counter;
                }
                set
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                    *m_Counter = value;
                }
            }

            public bool IsCreated
            {
                get { return m_Counter != null; }
            }

            public void Dispose()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif

                UnsafeUtility.Free(m_Counter, m_AllocatorLabel);
                m_Counter = null;
            }
        }

        [BurstCompile]
        struct DirectivesTest1 : IJob
        {
            public NativeCounter Kek;

            public DirectivesTest1(Allocator type)
            {
                Kek = new NativeCounter(type);
            }

            public void Execute()
            {
                for (var i = 0; i < 10; i++)
                    Kek.Increment();
            }
        }

        [BurstCompile]
        struct DirectivesTest2 : IJob
        {
            public void Execute()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                var obj = new object();
#endif
            }
        }
    }
}