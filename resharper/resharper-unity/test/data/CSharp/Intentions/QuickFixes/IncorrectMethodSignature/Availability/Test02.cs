using UnityEditor;

public class Foo : UnityEditor.AssetModificationProcessor
{
    public int On{caret}StatusUpdated(int value)
    {
        return 42;
    }
}
