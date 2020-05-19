using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
// ReSharper disable UnusedMember.Global
// ReSharper disable RedundantExtendsListEntry
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedType.Local
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable MemberCanBePrivate.Local
// ReSharper disable MemberCanBeMadeStatic.Local
// ReSharper disable SuggestVarOrType_BuiltInTypes
// ReSharper disable ConvertToConstant.Local
// ReSharper disable StringLiteralTypo
// ReSharper disable NotAccessedVariable
// ReSharper disable RedundantAssignment
// ReSharper disable UnusedVariable
// ReSharper disable HeuristicUnreachableCode
// ReSharper disable ObjectCreationAsStatement
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedParameter.Local
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable UnusedMethodReturnValue.Local
// ReSharper disable RedundantOverriddenMember
// ReSharper disable ReturnValueOfPureMethodIsNotUsed
// ReSharper disable EqualExpressionComparison
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeModifiersOrder
// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable UnassignedGetOnlyAutoProperty

#pragma warning disable 168
#pragma warning disable 162
#pragma warning disable 219
#pragma warning disable 414
#pragma warning disable 1717

public class TestAttribute : Attribute
{
    
}
//for attributes to work
namespace Unity
{
    namespace Jobs
    {
        public interface IJob
        {
            void Execute();
        }
    }

    namespace Burst
    {
        public class BurstCompileAttribute : Attribute
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

        [Test] public void Execute()
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
        [Test] public void Execute()
        {
            F();
        }

        private void F()
        {
            throw new ArgumentException("exception");
            new ArgumentException(nameof(F)); 
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

        [Test] public void Execute()
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
        [Test] public void Execute()
        {
            //throw new ArgumentException(new object().ToString());
            throw new ArgumentException(new object().ToString());
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
        [Test] public void Execute()
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

        [Test] public void Execute()
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
}