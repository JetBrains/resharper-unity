﻿using System;
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

namespace ReferenceExpressionTests
{
    public class ReferenceExpressionTests
    {
        private enum MyEnum
        {
            enumElem1,
            enumElem2
        }

        private class SimpleClass
        {
        }

        [BurstCompile]
        struct ReferenceExpressionTest1 : IJob
        {
            private MyEnum ourEnum;

            public void Execute()
            {
                ourEnum = ourEnum;
                ourEnum = MyEnum.enumElem1;
                var myClass = new SimpleClass();
            }
        }

        [BurstCompile]
        struct ReferenceExpressionTest2 : IJob
        {
            private static int getSetProp { get; set; }
            private static int getProp { get; }

            public void Execute()
            {
                var vari1 = getProp;
                getSetProp = 2;
            }
        }

        [BurstCompile]
        struct ReferenceExpressionTest3 : IJob
        {
            private static int field1 = 2;
            private readonly static int field2 = 2;
            private const int field3 = 2;

            public void Execute()
            {
                var var1 = field2;
                var var2 = field3;
                field1 = 2; 
            }
        }

        [BurstCompile]
        struct ReferenceExpressionTest4 : IJob
        {
            private static int field1 = 2;

            public void Execute()
            {
                var var1 = field1;
            }
        }
    }
}