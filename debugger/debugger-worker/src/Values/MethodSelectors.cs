using JetBrains.Metadata.Reader.API;
using MetadataLite.API;
using MetadataLite.API.Selectors;

// ReSharper disable InconsistentNaming

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Values
{
    public static class MethodSelectors
    {
        public static readonly MethodSelector SerializedProperty_GetArrayElementAtIndex =
            new MethodSelector(m =>
                m.Name == "GetArrayElementAtIndex"
                && m.Parameters.Length == 1 && m.Parameters[0].Type.Is("System.Int32"));

        public static readonly MethodSelector SerializedProperty_GetFixedBufferElementAtIndex =
            new MethodSelector(m =>
                m.Name == "GetFixedBufferElementAtIndex"
                && m.Parameters.Length == 1 && m.Parameters[0].Type.Is("System.Int32"));

        public static MethodSelector SerializedProperty_Copy =
            new MethodSelector(m => m.Name == "Copy" && m.Parameters.Length == 0);

        public static MethodSelector SerializedProperty_Next =
            new MethodSelector(m =>
                m.Name == "Next"
                && m.Parameters.Length == 1 && m.Parameters[0].Type.Is("System.Boolean"));

        public static MethodSelector SerializedProperty_GetEnumerator =
            new MethodSelector(m => m.Name == "GetEnumerator" && m.Parameters.Length == 0);

        public static MethodSelector Enumerator_MoveNext =
            new MethodSelector(m =>
                m.Name == StandardMemberNames.IEnumeratorMoveNext && m.Parameters.Length == 0);
    }
}