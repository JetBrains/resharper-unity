using UnityEngine;
namespace DefaultNamespace
{
    public class SendMessageUpwardsTest
    {
        public void Test(GameObject go)
        {
            go.SendMessageUpwards("Message");
            go.SendMessageUpwards("Message2", SendMessageOptions.RequireReceiver);
        }
    }
}