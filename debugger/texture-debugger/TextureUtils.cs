using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Graphics = UnityEngine.Graphics;
using Object = UnityEngine.Object;

namespace JetBrains.Debugger.Worker.Plugins.Unity.Presentation.Texture
{
    [SuppressMessage("ReSharper", "NotAccessedField.Global")] //used by frontend
    public class TexturePixelsInfo
    {
        public int Width;
        public int Height;
        public int[] Pixels;
        public int OriginalWidth;
        public int OriginalHeight;
        public string GraphicsTextureFormat;
        public string TextureName;
        public bool HasAlphaChannel;

        public TexturePixelsInfo(Vector2Int size, Color32[] pixels, UnityEngine.Texture texture)
        {
            Pixels = GetPixelsInts(pixels);
            Width = size.x;
            Height = size.y;
            TextureName = texture.name;
            GraphicsTextureFormat = texture.graphicsFormat.ToString();
            OriginalWidth = texture.width;
            OriginalHeight = texture.height;
            HasAlphaChannel = GraphicsFormatUtility.HasAlphaChannel(texture.graphicsFormat);
        }

        private static int[] GetPixelsInts(Color32[] pixels)
        {
            var result = new int[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                result[i] = pixels[i].ToHex();
            }

            return result;
        }
    }

    public static class UnityTextureAdapter
    {
        public static int ToHex(this Color32 c)
        {
            return (c.a << 24) | (c.r << 16) | (c.g << 8) | c.b;
        }

        // ReSharper disable once UnusedMember.Global
        public static string GetPixelsInString(UnityEngine.Texture texture) //Called by debugger evaluator
        {
            return GetPixelsInString(texture, new Vector2Int(texture.width, texture.height));
        }

        public static string GetPixelsInString(UnityEngine.Texture texture, Vector2Int size)
        {
            var texturePixelsInfo = GetTexturePixelsInfo(texture, size);
            return JsonUtility.ToJson(texturePixelsInfo, true);
        }

        // ReSharper disable once UnusedMember.Global
        public static TexturePixelsInfo GetTexturePixelsInfo(UnityEngine.Texture texture)
        {
            return GetTexturePixelsInfo(texture, new Vector2Int(texture.width, texture.height));
        }
        
        public static TexturePixelsInfo GetTexturePixelsInfo(UnityEngine.Texture texture, Vector2Int size)
        {
            size = GetTextureConvertedSize(texture, size);
            var texturePixelsInfo = GetPixels(texture, size);
            return texturePixelsInfo;
        }

        private static TexturePixelsInfo GetPixels(UnityEngine.Texture texture, Vector2Int size)
        {
            var targetTexture = CreateTargetTexture(size);

            try
            {
                CopyTexture(texture, targetTexture);
                var pixels = targetTexture.GetPixels32();
                var texturePixelsInfo = new TexturePixelsInfo(new Vector2Int(targetTexture.width, targetTexture.height)
                    , pixels
                    , texture);
                return texturePixelsInfo;
            }
            finally
            {
                Object.DestroyImmediate(targetTexture);
            }
        }

        private static byte[] GetRawBytes(UnityEngine.Texture texture, Vector2Int size)
        {
            Texture2D targetTexture = null;
            try
            {
                targetTexture = CreateTargetTexture(size);
                //the name is set to detect Texture2D leaks  
                targetTexture.name = "[Rider Debugger] temporary Texture2D";
                CopyTexture(texture, targetTexture);
                var rawTextureData = targetTexture.GetRawTextureData();
                
                return rawTextureData;
            }
            finally
            {
                Object.DestroyImmediate(targetTexture);
            }
        }

        private static void CopyTexture(UnityEngine.Texture texture, Texture2D targetTexture2d)
        {
            var renderTexture = RenderTexture.GetTemporary(
                targetTexture2d.width,
                targetTexture2d.height,
                0,
                RenderTextureFormat.ARGB32
            );

            //the name is set to detect RenderTexture leaks  
            renderTexture.name = "[Rider Debugger] temporary RenderTexture";

            var currentRenderTexture = RenderTexture.active;

            try
            {
                RenderTexture.active = renderTexture;

                // Blit the pixels on texture to the RenderTexture
                Graphics.Blit(texture, renderTexture);

                // Copy the pixels from the RenderTexture to the new Texture
                targetTexture2d.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                targetTexture2d.Apply();
            }
            finally
            {
                RenderTexture.active = currentRenderTexture;
                // Release the temporary RenderTexture
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        private static Texture2D CreateTargetTexture(Vector2Int size)
        {
            var texture2D = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false);
            return texture2D;
        }

        private static Vector2Int GetTextureConvertedSize(UnityEngine.Texture texture, Vector2Int size)
        {
            var texture2dWidth = texture.width;
            var texture2dHeight = texture.height;

            var divider = 1;
            while (texture2dWidth / divider > size.x || texture2dHeight / divider > size.y)
                divider *= 2;

            var targetTextureWidth = texture2dWidth / divider;
            var targetTextureHeight = texture2dHeight / divider;
            return new Vector2Int(targetTextureWidth, targetTextureHeight);
        }
    }
}