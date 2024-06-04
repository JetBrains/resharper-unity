using UnityEngine;

namespace DefaultNamespace
{
    public class MaterialPropertyTest
    {
        public void Test()
        {
            var material = Resources.Load<Material>(materialPath);
            material.HasProperty("_Decal{caret}Layer");
        }
    }
}