using UnityEngine;

namespace DefaultNamespace
{
    public class ConstConcatCreateFieldTest
    {
        public const string test = "test";
            
        public void Method(Material material)
        {
            material.SetFloat(test + "Foo" + {caret} test, 10.0f)};
        }
    }
}