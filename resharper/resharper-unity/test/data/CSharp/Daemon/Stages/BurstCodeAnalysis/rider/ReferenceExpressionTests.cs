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
                var myClass = new SimpleClass(); //Burst error BC1021: Creating a managed object `here placed object ref' is not supported
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
                getSetProp = 2; //Burst error BC1034: Writing to a static field `NewBehaviourScript.ReferenceExpressionTest.<getSetProp>k__BackingField` is not supported.
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
                field1 = 2; //Burst error BC1034: Writing to a static field `fullname till field` is not supported
            }
        }

        [BurstCompile]
        struct ReferenceExpressionTest4 : IJob
        {
            private static int field1 = 2;

            public void Execute()
            {
                var var1 = field1; //Burst error BC1040: Loading from a non-readonly static field
            }
        }
    }
}