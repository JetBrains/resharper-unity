using UnityEngine;

public class Test : MonoBehaviour
{
    private MonoBehaviour myOther;

    public void Start()
    {
        Invoke("Message", 5f);
        myOther.Invoke("Message", 5f);

        InvokeRepeating("MessageFromStartInvokeRepeating", 5f, 2f);
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
        Invoke("MessageIC", 5f);
        myOther.Invoke("MessageIC", 5f);

        InvokeRepeating("MessageIC", 5f, 2f);
        myOther.InvokeRepeating("MessageIC", 5f, 2f);

        gameObject.SendMessage("MessageIC");
        gameObject.SendMessage("MessageIC", SendMessageOptions.RequireReceiver);

        gameObject.SendMessageUpwards("MessageIC");
        gameObject.SendMessageUpwards("MessageIC", SendMessageOptions.RequireReceiver);

        gameObject.BroadcastMessage("MessageIC");
        gameObject.BroadcastMessage("MessageIC", SendMessageOptions.RequireReceiver);
    }

    public void FixedUpdate()
    {
        Invoke("MessageFU", 5f);
        myOther.Invoke("MessageFU", 5f);

        InvokeRepeating("MessageFU", 5f, 2f);
        myOther.InvokeRepeating("MessageFU", 5f, 2f);

        gameObject.SendMessage("MessageFU");
        gameObject.SendMessage("MessageFU", SendMessageOptions.RequireReceiver);

        gameObject.SendMessageUpwards("MessageFU");
        gameObject.SendMessageUpwards("MessageFU", SendMessageOptions.RequireReceiver);

        gameObject.BroadcastMessage("MessageFU");
        gameObject.BroadcastMessage("MessageFU", SendMessageOptions.RequireReceiver);

        IndirectlyCalled();
    }

    public void Update()
    {
        Invoke("MessageCalledSeveralTimesFromPCC", 5f);
        myOther.Invoke("MessageCalledSeveralTimesFromPCC", 5f);

        InvokeRepeating("MessageCalledSeveralTimesFromPCC", 5f, 2f);
        myOther.InvokeRepeating("MessageCalledSeveralTimesFromPCC", 5f, 2f);

        gameObject.SendMessage("MessageCalledSeveralTimesFromPCC");
        gameObject.SendMessage("MessageCalledSeveralTimesFromPCC", SendMessageOptions.RequireReceiver);

        gameObject.SendMessageUpwards("MessageCalledSeveralTimesFromPCC");
        gameObject.SendMessageUpwards("MessageCalledSeveralTimesFromPCC", SendMessageOptions.RequireReceiver);

        gameObject.BroadcastMessage("MessageCalledSeveralTimesFromPCC");
        gameObject.BroadcastMessage("MessageCalledSeveralTimesFromPCC", SendMessageOptions.RequireReceiver);
    }

    public void LateUpdate()
    {
        Invoke("MessageFromPCC_1", 5f);
        myOther.Invoke("MessageFromPCC_2", 5f);

        InvokeRepeating("MessageFromPCC_3", 5f, 2f);
        myOther.InvokeRepeating("MessageFromPCC_4", 5f, 2f);

        gameObject.SendMessage("MessageFromPCC_5");
        gameObject.SendMessage("MessageFromPCC_6", SendMessageOptions.RequireReceiver);

        gameObject.SendMessageUpwards("MessageFromPCC_7");
        gameObject.SendMessageUpwards("MessageFromPCC_8", SendMessageOptions.RequireReceiver);

        gameObject.BroadcastMessage("MessageFromPCC_9");
        gameObject.BroadcastMessage("MessageFromPCC_10", SendMessageOptions.RequireReceiver);
    }

    public void Message()
    {
    }

    public void MessageFU()
    {
        
    }

    public void MessageIC()
    {
        
    }
    
    public void MessageFromStartInvokeRepeating()
    {
        
    }
    
    public void MessageCalledSeveralTimesFromPCC()
    {
    }
    
    public void MessageFromPCC_1()
    {
    }
    
    public void MessageFromPCC_2()
    {
    }
    
    public void MessageFromPCC_3()
    {
    }
    
    public void MessageFromPCC_4()
    {
    }
    
    public void MessageFromPCC_5()
    {
    }
    
    public void MessageFromPCC_6()
    {
    }
    
    public void MessageFromPCC_7()
    {
    }
    
    public void MessageFromPCC_8()
    {
    }
    
    public void MessageFromPCC_9()
    {
    }
    
    public void MessageFromPCC_10()
    {
    }
}
