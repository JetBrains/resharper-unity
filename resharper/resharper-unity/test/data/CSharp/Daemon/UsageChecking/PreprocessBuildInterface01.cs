using UnityEditor;
using UnityEditor.Build;

public class A : IPreprocessBuild
{
    public int callbackOrder { get; }
    public int RandomProperty { get; }
    
	public virtual void OnPreprocessBuild(BuildTarget target, string path)
    {
        throw new System.NotImplementedException();
    }
	
	public void RandomMethod()
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

public class C : A
{
    public override void OnPreprocessBuild(BuildTarget target, string path)
    {
        throw new System.NotImplementedException();
    }
}