using UnityEngine;
namespace DefaultNamespace
{
    public class SendMessageTest
    {
        public void Test(GameObject go)
        {
            go.SendMessage("Message");
            go.SendMessage("Message2", SendMessageOptions.RequireReceiver);
        }
    }
}