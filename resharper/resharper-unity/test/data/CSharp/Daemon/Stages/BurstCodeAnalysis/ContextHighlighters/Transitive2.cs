using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

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

namespace SmartMarkingTests
{
    public class SmartMarkingTests
    {
        private class SimpleClass
        {
            public void D()
            {
                var obj = new object();
            }
        }

        [BurstCompile]
        private struct SmartMarkingTest1 : IJob
        {
            public void Execute()
            {
                F();
            }

            private void F()
            {
                {caret}
                var obj = new object();
                D();
            }

            private void D()
            {
                var obj = new object();
            }
        }

        [BurstCompile]
        private struct SmartMarkingTest2 : IJob
        {
            public void Execute()
            {
                F();
                D();
            }

            private void F()
            {
                var obj = new object();
                D();
            }

            private void D()
            {
                var obj = new object();
            }
        }

        [BurstCompile]
        private struct SmartMarkingTest3 : IJob
        {
            public void Execute()
            {
                SimpleClass simpleClass = null;
                simpleClass.D();
            }
        }
    }
}