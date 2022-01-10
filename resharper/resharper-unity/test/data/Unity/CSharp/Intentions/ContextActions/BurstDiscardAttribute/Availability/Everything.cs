using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
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

namespace Burst.ContextActionsTests
{
    public class ContextActionsTests : MonoBehaviour
    {
        private void Start()
        {
            var test1 = new ContextActionsTest1();
            test1.Schedule().Complete();
            var test2 = new ContextActionsTest2();
            test2.Schedule().Complete();
            var test3 = new ContextActionsTest3();
            test3.Schedule().Complete();
            var test4 = new ContextActionsTest4();
            test4.Schedule().Complete();
        }
        
        [BurstCompile]
        public struct ContextActionsTest1 : IJob
        {
            public void Exe{on}cute()
            {
                F1();
            }

            private void F{on}1()
            {
                var obj = new object();
            }
        }
        
        [BurstCompile]
        public struct ContextActionsTest2 : IJob
        {
            public void Exec{on}ute()
            {
                var obj = new object();
                F2();
            }

            private void F{off}2()
            {
                var obj = new object();
            }
        }
        
        [BurstCompile]
        public struct ContextActionsTest3 : IJob
        {
            public void {on}Execute()
            {
                var obj = new object();
                F3();
            }

            private void F{off}3()
            {
            }
        }
        
        [BurstCompile]
        public struct ContextActionsTest4 : IJob
        {
            public void Execute{on}()
            {
                F4();
            }

            private void F{on}4()
            {
            }
        }
    }
}