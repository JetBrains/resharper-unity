using UnityEditor;
using UnityEditor.Build;

public class A : IPreprocessBuild
{
    public int callbackOrder { get; }
    public void OnPreprocessBuild(BuildTarget target, string path)
    {
        throw new System.NotImplementedException();
    }
}

public class B : A, IPostprocessBuild
{
    public void OnPostprocessBuild(BuildTarget target, string path)
    {
        throw new System.NotImplementedException();
    }
}