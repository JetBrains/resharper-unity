using UnityEngine;

public class Example : MonoBehaviour
{
    // Create a material from code
    void Start()
    {
        // Create a material with transparent diffuse shader
        Material material = new Material(Shader.Find("F{caret}oo"));
        var shader = Shader.Find("Bar");
        material.color = Color.green;

        // assign the material to the renderer
        GetComponent<Renderer>().material = material;
    }
}
