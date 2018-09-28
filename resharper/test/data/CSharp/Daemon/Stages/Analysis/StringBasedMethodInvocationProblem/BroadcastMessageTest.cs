using UnityEngine;
namespace DefaultNamespace
{
    public class BroadcastMessageTest
    {
        public void Test(GameObject go)
        {
            go.BroadcastMessage("Message");
            go.BroadcastMessage("Message2", SendMessageOptions.RequireReceiver);
        }
    }
}