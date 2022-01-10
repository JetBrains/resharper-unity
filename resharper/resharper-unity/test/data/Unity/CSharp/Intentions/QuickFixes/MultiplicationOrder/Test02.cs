using UnityEngine;

namespace DefaultNamespace
{
    public class Test02 : MonoBehaviour
    {
        public void Update()
        {
            int a =5;
            var b = Vector3.up;
            var c = 5 * b{caret} * a;
        }
    }
}