using System;

namespace JetBrains.Rider.Unity.Editor.AfterUnity56
{
    public static class EnumExtensions
    {
        /// <summary>
        /// HasFlag implementation for older dotnet framework
        /// </summary>
        /// <param name="flags"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static bool HasFlag(this Enum flags, Enum flag)
        {
            // check if from the same type.
            if (flags.GetType() != flag.GetType())
            {
                throw new ArgumentException("flags and flag should be of the same type.");
            }

            var intFlag = Convert.ToUInt64(flag);
            var intFlags = Convert.ToUInt64(flags);

            return (intFlags & intFlag) == intFlag;
        }
    }
}