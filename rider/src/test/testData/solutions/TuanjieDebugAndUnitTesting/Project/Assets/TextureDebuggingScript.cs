using UnityEngine;

namespace Src.Scripts
{
    public class TextureDebuggingScript : MonoBehaviour
    {
        private void Update()
        {
            var texture2D = new Texture2D(2, 2, TextureFormat.RGBA32, 0, true);
            texture2D.name = "Temporary texture for debugging";
            texture2D.SetPixels(new[] { Color.red, Color.green, Color.blue, new Color(0.5f, 0.5f, 0.5f, 0.5f) });
            texture2D.Apply();
            Debug.Log(texture2D);
        }
    }
}