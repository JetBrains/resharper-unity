using UnityEngine;

public class Test : MonoBehaviour
{
    private MonoBehaviour myOther;

    public void Start()
    {
        Invoke("Message", 5f);
        myOther.Invoke("Message", 5f);

        InvokeRepeating("Message", 5f, 2f);
        myOther.InvokeRepeating("Message", 5f, 2f);

        gameObject.SendMessage("Message");
        gameObject.SendMessage("Message", SendMessageOptions.RequireReceiver);

        gameObject.SendMessageUpwards("Message");
        gameObject.SendMessageUpwards("Message", SendMessageOptions.RequireReceiver);

        gameObject.BroadcastMessage("Message");
        gameObject.BroadcastMessage("Message", SendMessageOptions.RequireReceiver);
    }

    private void IndirectlyCalled()
    {
        Invoke("Message", 5f);
        myOther.Invoke("Message", 5f);

        InvokeRepeating("Message", 5f, 2f);
        myOther.InvokeRepeating("Message", 5f, 2f);

        gameObject.SendMessage("Message");
        gameObject.SendMessage("Message", SendMessageOptions.RequireReceiver);

        gameObject.SendMessageUpwards("Message");
        gameObject.SendMessageUpwards("Message", SendMessageOptions.RequireReceiver);

        gameObject.BroadcastMessage("Message");
        gameObject.BroadcastMessage("Message", SendMessageOptions.RequireReceiver);
    }

    public void FixedUpdate()
    {
        Invoke("Message", 5f);
        myOther.Invoke("Message", 5f);

        InvokeRepeating("Message", 5f, 2f);
        myOther.InvokeRepeating("Message", 5f, 2f);

        gameObject.SendMessage("Message");
        gameObject.SendMessage("Message", SendMessageOptions.RequireReceiver);

        gameObject.SendMessageUpwards("Message");
        gameObject.SendMessageUpwards("Message", SendMessageOptions.RequireReceiver);

        gameObject.BroadcastMessage("Message");
        gameObject.BroadcastMessage("Message", SendMessageOptions.RequireReceiver);

        IndirectlyCalled();
    }

    public void Update()
    {
        Invoke("Message", 5f);
        myOther.Invoke("Message", 5f);

        InvokeRepeating("Message", 5f, 2f);
        myOther.InvokeRepeating("Message", 5f, 2f);

        gameObject.SendMessage("Message");
        gameObject.SendMessage("Message", SendMessageOptions.RequireReceiver);

        gameObject.SendMessageUpwards("Message");
        gameObject.SendMessageUpwards("Message", SendMessageOptions.RequireReceiver);

        gameObject.BroadcastMessage("Message");
        gameObject.BroadcastMessage("Message", SendMessageOptions.RequireReceiver);
    }

    public void LateUpdate()
    {
        Invoke("Message", 5f);
        myOther.Invoke("Message", 5f);

        InvokeRepeating("Message", 5f, 2f);
        myOther.InvokeRepeating("Message", 5f, 2f);

        gameObject.SendMessage("Message");
        gameObject.SendMessage("Message", SendMessageOptions.RequireReceiver);

        gameObject.SendMessageUpwards("Message");
        gameObject.SendMessageUpwards("Message", SendMessageOptions.RequireReceiver);

        gameObject.BroadcastMessage("Message");
        gameObject.BroadcastMessage("Message", SendMessageOptions.RequireReceiver);
    }

    public void Message()
    {
    }
}
