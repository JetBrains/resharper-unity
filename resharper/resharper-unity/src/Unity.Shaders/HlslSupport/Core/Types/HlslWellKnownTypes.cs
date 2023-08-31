#nullable enable
using JetBrains.ReSharper.Psi.Cpp.Types;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Core.Types
{
    public static class HlslWellKnownTypes
    {
        public static readonly CppQualType Half2 = HlslPredefinedTypes.GetPredefinedVectorType(HlslPredefinedType.Half, 2);
        public static readonly CppQualType Half3 = HlslPredefinedTypes.GetPredefinedVectorType(HlslPredefinedType.Half, 3);
        public static readonly CppQualType Half4 = HlslPredefinedTypes.GetPredefinedVectorType(HlslPredefinedType.Half, 4);
        public static readonly CppQualType Float2 = HlslPredefinedTypes.GetPredefinedVectorType(HlslPredefinedType.Float, 2);
        public static readonly CppQualType Float3 = HlslPredefinedTypes.Float3;
        public static readonly CppQualType Float4 = HlslPredefinedTypes.Float4;
    }
}