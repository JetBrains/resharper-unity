using UnityEngine;

public class Test : MonoBehaviour
{
    private Camera myMainCamera;

    public void Start()
    {
        myMainCamera = Camera.main;
        var fov = Camera.main.fieldOfView;
    }

    public void FixedUpdate()
    {
        var x = Camera.main;
        var fov = Camera.main.fieldOfView;
    }

    public void Update()
    {
        var x = Camera.main;
        var fov = Camera.main.fieldOfView;
    }

    public void LateUpdate()
    {
        var x = Camera.main;
        var fov = Camera.main.fieldOfView;
    }
}

public class Boring
{
    public void Update()
    {
        var x = Camera.main;
        var fov = Camera.main.fieldOfView;
    }
}
