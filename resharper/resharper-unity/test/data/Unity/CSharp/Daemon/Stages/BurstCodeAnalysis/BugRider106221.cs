using System;
using Unity.Burst;

namespace Unity
{
    namespace Burst
    {
        public class BurstCompileAttribute : Attribute
        {
        }

        public class BurstDiscardAttribute : Attribute
        {
        }
    }

    [BurstCompile]
    public static class BurstThings
    {
        public interface IInterfaceForThing1
        {
            int Value { get; }
        }

        [BurstCompile]
        public readonly struct StructForThing : IInterfaceForThing1
        {
            public StructForThing(int value)
            {
                Value = value;
            }

            public int Value { get; }
        }

        public interface IInterfaceContained
        {
            StructForThing Thing { get; }
        }

        [BurstCompile]
        public struct StructContained : IInterfaceContained
        {
            public StructContained(StructForThing thing)
            {
                Thing = thing;
            }

            public StructForThing Thing { get; }
        }

        [BurstCompile]
        public static int DoTest<TTest>(TTest test)
            where TTest : IInterfaceContained
        {
            return DoTestInternal(test.Thing);  //There must be no ```Burst: Loading managed type 'x' is not supported``` warning
        }

        [BurstCompile]
        public static StructForThing GetThing<TTest>(TTest test)
            where TTest : IInterfaceContained
        {
            return test.Thing; //There must be no ```Burst: Loading managed type 'x' is not supported``` warning
        }

        private static int DoTestInternal(in StructForThing thing)
        {
            return thing.Value;
        }
    }
}