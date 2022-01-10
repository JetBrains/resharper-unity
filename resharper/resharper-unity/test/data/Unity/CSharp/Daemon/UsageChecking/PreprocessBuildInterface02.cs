using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class A : IPreprocessBuildWithReport
{
    public int callbackOrder { get; }
    public int RandomProperty { get; }
    
	public virtual void OnPreprocessBuild(BuildReport report)
    {
        throw new System.NotImplementedException();
    }
	
	public void RandomMethod()
    {
        throw new System.NotImplementedException();
    }
}

public class B : A, IPostprocessBuildWithReport
{
    public void OnPostprocessBuild(BuildReport report)
    {
        throw new System.NotImplementedException();
    }
}

public class C : A
{
	public override void OnPreprocessBuild(BuildReport report)
    {
        throw new System.NotImplementedException();
    }
}
