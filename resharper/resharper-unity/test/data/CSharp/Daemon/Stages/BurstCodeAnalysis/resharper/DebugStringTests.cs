using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
        public static void LogWarning(object message)
        {
        }
        public static void LogError(object message)
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
        public struct FixedString128
        {
            public FixedString128(String source)
            {
            }

            public static implicit operator FixedString128(string b) => new FixedString128(b);

        }
        public struct FixedString512
        {
            public FixedString512(String source)
            {
            }

            public static implicit operator FixedString512(string b) => new FixedString512(b);

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

namespace DebugStringTests
{
    public class DebugStringTests
    {
        private static string toFormat = "228{0}";

        [BurstCompile]
        private struct DebugStringTests1 : IJob
        {
            public void Execute()
            {
                Debug.Log("Plain log");
                Debug.LogWarning("Warning log");
                Debug.LogError("Error log");
                Debug.Log($"AAA{1}, {2}, {3}");
                Debug.Log(3 / 2 > 1 ? $"AAA{1}" : $"{2}, {3}");
                Debug.Log(string.Format("AAA{0}, {1}, {2}", 1, 2, 3));
                FixedString128 fixedString128 = "This is an integer value {12} used with FixedString128";
                FixedString512 fixedString512 = string.Format("{0}", 1);
                Debug.Log(fixedString128);
                fixedString512 = "asdasd";
                fixedString512 = "as{2}d{3}asd";
                int var1 = 2;
                FixedString128 variable = "string";

                string.Format("{0} asdasd", variable); //BC1033 - beucase no var
            }
        }

        [BurstCompile]
        private struct DebugStringTests2 : IJob
        {
            public void Execute()
            {
                string kopa1 = $"as{2}d{3}asd"; //LOADING string literal
            }
        }

        [BurstCompile]
        private struct DebugStringTests3 : IJob
        {
            public void Execute()
            {
                var fixasdfed = new FixedString128("variable"); //BC1033
            }
        }

        [BurstCompile]
        private struct DebugStringTests4 : IJob
        {
            public void Execute()
            {
                string.Format(new CultureInfo(1), "", null); //BC1021 creating managed
            }
        }

        [BurstCompile]
        private struct DebugStringTests5 : IJob
        {
            public void Execute()
            {
                string.Format(toFormat, 12); //BC1042 managed loading not supported
            }
        }

        [BurstCompile]
        private struct DebugStringTests6 : IJob
        {
            public void Execute()
            {
                Debug.Log(12); // BC1349 Invalid argument. Expecting string literal or string.
            }
        }

        [BurstCompile]
        private struct DebugStringTests7 : IJob
        {
            public void Execute()
            {
                FixedString128 variable = "string";

                var lola = string.Format("{0} asdasd", variable); //BC1033 - beucase no var
            }
        }

        [BurstCompile]
        private struct DebugStringTests8 : IJob
        {
            public void Execute()
            {
                string kopa = "asdasd";//BC1033
            }
        }
    }
}