using UnityEngine;
using System.Collections;

namespace DefaultNamespace
{
    public class CoroutineTest : MonoBehaviour
    {
        public void Start()
        {
            StartCoroutine("HotMethod");
            StartCoroutine(HotMethod2());
        }

        public void HotMethod()
        {
            var x = gameObject.GetComponent<Transform>();
        }
        
        public IEnumerator HotMethod2()
        {
            var x = GetComponent<Transform>();
        }
    }
}