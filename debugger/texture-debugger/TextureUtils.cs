using System.Drawing;
using System.Linq;
using UnityEngine;
using Graphics = UnityEngine.Graphics;
using Object = UnityEngine.Object;

namespace JetBrains.Debugger.Worker.Plugins.Unity.Presentation.Texture
{
    public class TexturePixelsInfo
    {
        public int Width;
        public int Height;
        public int[] Pixels;
        public int OriginalWidth;
        public int OriginalHeight;
        public string GraphicsTextureFormat;
        public string TextureFormat;


        public TexturePixelsInfo(Size size, Color32[] pixels, Texture2D texture2D)
        {
            Pixels = pixels.Select(c => c.ToHex()).ToArray();
            Width = size.Width;
            Height = size.Height;
            TextureFormat = texture2D.format.ToString();
            GraphicsTextureFormat = texture2D.graphicsFormat.ToString();
            OriginalWidth = texture2D.width;
            OriginalHeight = texture2D.height;
        }
    }

    public static class UnityTextureAdapter
    {
        public static int ToHex(this Color32 c)
        {
            return (c.a << 24) | (c.r << 16) | (c.g << 8) | c.b;
        }

        public static string GetPixelsInString(Texture2D texture2D)
        {
            return GetPixelsInString(texture2D, new Size(texture2D.width, texture2D.height));
        }

        public static string GetPixelsInString(Texture2D texture2D, Size size)
        {
            size = GetTextureConvertedSize(texture2D, size);
            var color32 = GetPixels(texture2D, size);
            return JsonUtility.ToJson(color32, true);
        }

        private static TexturePixelsInfo GetPixels(Texture2D texture2d, Size size)
        {
            var targetTexture = CreateTargetTexture(size);

            try
            {
                CopyTexture(texture2d, targetTexture);
                var pixels = targetTexture.GetPixels32();
                var texturePixelsInfo = new TexturePixelsInfo(new Size(targetTexture.width, targetTexture.height)
                    , pixels
                    , texture2d);
                return texturePixelsInfo;
            }
            finally
            {
                Object.DestroyImmediate(targetTexture);
            }
        }

        private static byte[] GetRawBytes(Texture2D texture2d, Size size)
        {
            var targetTexture = CreateTargetTexture(size);
            CopyTexture(texture2d, targetTexture);
            var rawTextureData = targetTexture.GetRawTextureData();
            Object.DestroyImmediate(targetTexture);
            return rawTextureData;
        }

        private static void CopyTexture(UnityEngine.Texture texture2d, Texture2D targetTexture)
        {
            var renderTexture = RenderTexture.GetTemporary(
                targetTexture.width,
                targetTexture.height,
                0,
                RenderTextureFormat.ARGB32
            );

            try
            {
                // Blit the pixels on texture to the RenderTexture
                Graphics.Blit(texture2d, renderTexture);

                // Backup the currently set RenderTexture
                // var previous = RenderTexture.active;

                try
                {
                    // Set the current RenderTexture to the temporary one we created
                    // RenderTexture.active = renderTexture;

                    // Create a new readable Texture2D to copy the pixels to it

                    // Copy the pixels from the RenderTexture to the new Texture
                    targetTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                    targetTexture.Apply();
                }
                finally
                {
                    // Reset the active RenderTexture
                    // RenderTexture.active = previous;                    
                }
            }
            finally
            {
                // Release the temporary RenderTexture
                RenderTexture.ReleaseTemporary(renderTexture);               
            }

        }

        private static Texture2D CreateTargetTexture(Size size)
        {
            var texture2D = new Texture2D(size.Width, size.Height, TextureFormat.RGBA32, false);
            return texture2D;
        }

        private static Size GetTextureConvertedSize(UnityEngine.Texture texture2d, Size size)
        {
            var texture2dWidth = texture2d.width;
            var texture2dHeight = texture2d.height;

            var divider = 1;
            while (texture2dWidth / divider > size.Width && texture2dHeight / divider > size.Height)
                divider *= 2;

            var targetTextureWidth = texture2dWidth / divider;
            var targetTextureHeight = texture2dHeight / divider;
            return new Size(targetTextureWidth, targetTextureHeight);
        }
    }
}