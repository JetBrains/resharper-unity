using UnityEngine;

public class Test : MonoBehaviour
{
    public void Update()
    {
        // ReSharper disable once Unity.PerformanceCriticalCodeCameraMain
        var x = Camera.main;
        var fov = Camera.main.fieldOfView;
    }
}
