using UnityEngine;

public class NotSerialized
{
    public string NotSerializedField1;
    [Serialize{caret}Field] public const string SerializedField2 = "Something";
}
