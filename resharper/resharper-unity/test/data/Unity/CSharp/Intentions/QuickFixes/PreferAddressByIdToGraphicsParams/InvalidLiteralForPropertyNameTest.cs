using UnityEngine;

namespace DefaultNamespace
{
    public class InvalidLiteralForProperyNameTest
    {
        public void Test(Material material)
        {
            material.SetFloat("f{caret}loat", 10.0f);
        }
    }
}