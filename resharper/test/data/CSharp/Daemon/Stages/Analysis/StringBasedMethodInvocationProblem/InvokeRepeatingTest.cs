using UnityEngine;
namespace DefaultNamespace
{
    public class InvokeRepeatingTest
    {
        public void Test(MonoBehaviour mb)
        {
            mb.InvokeRepeating("Message", 5f, 2f);
        }
    }
}