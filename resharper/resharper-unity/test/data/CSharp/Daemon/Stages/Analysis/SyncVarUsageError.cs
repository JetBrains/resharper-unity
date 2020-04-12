using UnityEngine;
using UnityEngine.Networking;

public class IllegalUsage : MonoBehaviour
{
    [SyncVar]
    public int IntValue;
}

public class ValidUsage : NetworkBehaviour
{
    [SyncVar]
    public int IntValue;
}
