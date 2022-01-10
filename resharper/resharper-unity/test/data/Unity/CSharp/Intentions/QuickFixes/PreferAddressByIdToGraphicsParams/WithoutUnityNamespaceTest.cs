namespace DefaultNamespace
{
    public class WithoutUnityNamespaceTest
    {
        public void Test(UnityEngine.Material material)
        {
            material.SetFloat("t{caret}est", 10.0f);
        }
    }
}