using UnityEngine;

namespace DefaultNamespace
{
    public class Test02 : MonoBehaviour
    {
        public void Update()
        {
            var x = Quaternion.identity * Quaternion.identity * (Vector3.one *{caret} Vector3.one) * 5f * 6f; 
        }
    }
}