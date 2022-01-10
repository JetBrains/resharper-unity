using UnityEngine;

namespace DefaultNamespace
{
    public class Test02 : MonoBehaviour
    {
        public void Update()
        {
	    var result = 7f * 7f * (Quaternion.Euler(0, 0, 0) * ({caret}5 * 7 * (Quaternion.Euler(0, 0, 0) * Vector3.back)));        

        }
    }
}