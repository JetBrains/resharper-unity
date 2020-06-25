using System;
using System.Collections;
using System.Collections.Generic;
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


namespace FunctionParametersReturnTests
{
    public class FunctionParametersReturnTests
    {
        public interface IFoo
        {
            void Foo();
        }

        public struct Struct : IFoo
        {
            public void Foo()
            {
            }
        }

        public struct BadFoo : IFoo
        {
            public void Foo()
            {
                var obj = new object();
            }
        }

        [BurstCompile]
        struct FunctionParametersReturnValueTest1 : IJob
        {
            public void Fobject(object obj)
            {
            }

            public void FStruct(Struct @struct)
            {
            }

            public void Execute()
            {
                FStruct(new Struct());
                Fobject(null); // Burst error BC1016: The managed function `NewBehaviourScript.FunctionParametersReturnValueTest.Fobject(NewBehaviourScript.FunctionParametersReturnValueTest* this, object a)` is not supported
            }
        }

        [BurstCompile]
        struct FunctionParametersReturnValueTest2 : IJob
        {
            public void FFoo(IFoo foo)
            {
            }

            public void Execute()
            {
                FFoo(null); // BC1016
            }
        }

        [BurstCompile]
        struct FunctionParametersReturnValueTest3 : IJob
        {
            public IFoo FReturn()
            {
                return new Struct();
            }

            public void Execute()
            {
                FReturn(); // BC1016
            }
        }

        [BurstCompile]
        struct FunctionParametersReturnValueTest4 : IJob
        {
            public void GenericF<T>(T a) where T : struct, IFoo
            {
                a.Foo();
            }

            public void Execute()
            {
                GenericF(new Struct());
            }
        }
    }
}