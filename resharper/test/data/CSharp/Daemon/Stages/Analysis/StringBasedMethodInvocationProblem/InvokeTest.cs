using UnityEngine;
namespace DefaultNamespace
{
    public class InvokeTest
    {
        public void Test(MonoBehaviour mb)
        {
            mb.Invoke("Message", 5f);
        }
    }
}