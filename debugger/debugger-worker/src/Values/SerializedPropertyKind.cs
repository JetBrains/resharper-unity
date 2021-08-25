namespace JetBrains.Debugger.Worker.Plugins.Unity.Values
{
    public enum SerializedPropertyKind
    {
        // Not used in SerializedPropertyType, but useful for code
        Invalid = -999,
        ArrayModifier = -99,
        FixedBufferModifier = -98,

        // Maps to UnityEditor.SerializedPropertyType
        // These MUST be kept in sync!

        Generic = -1, // Arrays, custom serializable structs, etc.
        Integer = 0,
        Boolean = 1,
        Float = 2,
        String = 3,
        Color = 4,
        ObjectReference = 5,
        LayerMask = 6,
        Enum = 7,
        Vector2 = 8,
        Vector3 = 9,
        Vector4 = 10,
        Rect = 11,
        ArraySize = 12, // Used when iterating through the properties that make up an array. Comes before the data
        Character = 13,
        AnimationCurve = 14,
        Bounds = 15,
        Gradient = 16,
        Quaternion = 17,
        ExposedReference = 18,
        FixedBufferSize = 19,   // Like ArraySize
        Vector2Int = 20,
        Vector3Int = 21,
        RectInt = 22,
        BoundsInt = 23,
        ManagedReference = 24
    }
}