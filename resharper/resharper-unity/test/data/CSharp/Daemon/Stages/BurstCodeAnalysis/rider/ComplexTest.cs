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

public class NewBehaviourScript
{
    [BurstCompile]
    struct PrimitiveTest : IJob
    {
        public void MustBeProhibited()
        {
            string str2 = "asdasd"; 
            string str1 = null; 
            str1 = str2; 
            char c = str2[0];
            var ch = 'a';
            char ch2 = 'b';
            ch2 = ch;
            ch = 'd';
            char ch3 = new char();
        }

        public void Execute()
        {
            var varInt = 1;
            int intInt = 1;
            var newInt = new int();
            newInt = 1;
            MustBeProhibited();
        }
    }

    [BurstCompile]
    struct ExceptionsText : IJob
    {
        public void Execute()
        {
            F();
        }

        private void F()
        {
            throw new ArgumentException("exception");
            new ArgumentException(new object().ToString());
            try 
            {
                int a = 1;
            }
            catch (Exception e) 
            {
                int b = 2;
            }
            finally 
            {
                int c = 2;
            }
        }
    }

    [BurstCompile]
    struct FunctionParametersReturnValueTest : IJob
    {
        public interface IInterface
        {
            void function();
        }

        public struct strct : IInterface
        {
            public void function()
            {
            }
        }

        public void Fobject(object a)
        {
        }

        public void Finterface(IInterface @interface)
        {
        }

        public void Fstruct(strct strct)
        {
        }

        public IInterface FReturn()
        {
            return new strct();
        }

        public void GenericF<T>(T a) where T : struct, IInterface
        {
            a.function();
        }

        public void Execute()
        {
            Fobject(null); 
            Finterface(null); 
            Fstruct(new strct());
            FReturn(); 
            GenericF(new strct());
        }
    }

    [BurstCompile]
    struct ForeachTest : IJob
    {
        public void Execute()
        {
            foreach (var integer in new NativeArray<int>())
            {
                Console.WriteLine(integer);
            }
        }
    }

    public static SimpleClass myClasss = new SimpleClass();

    [BurstCompile]
    struct MethodsInvocationTest : IJob
    {
        public void Execute()
        {
            F();
        }

        public override int GetHashCode()
        {
             return base.GetHashCode();
        }

        private void F()
        {
            SimpleClass.StaticMethod();
            GetType(); 
            Equals(null, null);
            Equals(null);
            ToString(); 
            GetHashCode();
            var cls = myClasss;
            myClasss.PlainMethod(); 
        }
    }

    private enum MyEnum
    {
        enumElem1,
        enumElem2
    }

    [BurstCompile]
    struct ReferenceExpressionTest : IJob
    {
        private static int getSetProp { get; set; }
        private static int field1 = 2;
        private readonly static int field2 = 2;
        private const int field3 = 2;
        private static int getProp { get; }
        private MyEnum ourEnum;

        public void Execute()
        {
            SimpleClass myClass = new SimpleClass();
            getSetProp = 2;
            field1 = 2;
            ourEnum = ourEnum;
        }
    }

    public class SimpleClass
    {
        public static void StaticMethod()
        {
        }

        public void PlainMethod()
        {
        }
    }
    
    [BurstCompile]
    struct BurstDiscardTest : IJob
    {
        public void Execute()
        {
            F();
            D();
        }

        [BurstDiscard]
        void F()
        {
            var current = new object();
        }

        void D()
        {
            var current = new object();
        }
    }
}