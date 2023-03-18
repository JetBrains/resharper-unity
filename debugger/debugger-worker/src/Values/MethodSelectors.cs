// ReSharper disable InconsistentNaming

using Mono.Debugging.MetadataLite.API;
using Mono.Debugging.MetadataLite.API.Selectors;

namespace JetBrains.Debugger.Worker.Plugins.Unity.Values
{
    public static class MethodSelectors
    {
        public static readonly MethodSelector SerializedProperty_GetArrayElementAtIndex =
            new(m => m.Name == "GetArrayElementAtIndex" && m.Parameters.Length == 1 &&
                     m.Parameters[0].Type.Is("System.Int32"));

        public static readonly MethodSelector SerializedProperty_GetFixedBufferElementAtIndex =
            new(m => m.Name == "GetFixedBufferElementAtIndex" && m.Parameters.Length == 1 &&
                     m.Parameters[0].Type.Is("System.Int32"));

        public static readonly MethodSelector SerializedProperty_Copy =
            new(m => m.Name == "Copy" && m.Parameters.Length == 0);

        public static readonly MethodSelector SerializedProperty_Next =
            new(m => m.Name == "Next" && m.Parameters.Length == 1 && m.Parameters[0].Type.Is("System.Boolean"));

        public static readonly MethodSelector SerializedObject_GetIterator =
            new(m => m.Name == "GetIterator" && m.Parameters.Length == 0);
    }
}