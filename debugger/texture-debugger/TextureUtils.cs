using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
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

        public TexturePixelsInfo(Size size, Color32[] pixels, UnityEngine.Texture texture)
        {
            Pixels = pixels.Select(c => c.ToHex()).ToArray();
            Width = size.Width;
            Height = size.Height;
            TextureName = texture.name;
            GraphicsTextureFormat = texture.graphicsFormat.ToString();
            OriginalWidth = texture.width;
            OriginalHeight = texture.height;
            HasAlphaChannel = GraphicsFormatUtility.HasAlphaChannel(texture.graphicsFormat);
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
            return GetPixelsInString(texture, new Size(texture.width, texture.height));
        }

        public static string GetPixelsInString(UnityEngine.Texture texture, Size size)
        {
            size = GetTextureConvertedSize(texture, size);
            var texturePixelsInfo = GetPixels(texture, size);
            return JsonUtility.ToJson(texturePixelsInfo, true);
        }

        private static TexturePixelsInfo GetPixels(UnityEngine.Texture texture, Size size)
        {
            var targetTexture = CreateTargetTexture(size);

            try
            {
                CopyTexture(texture, targetTexture);
                var pixels = targetTexture.GetPixels32();
                var texturePixelsInfo = new TexturePixelsInfo(new Size(targetTexture.width, targetTexture.height)
                    , pixels
                    , texture);
                return texturePixelsInfo;
            }
            finally
            {
                Object.DestroyImmediate(targetTexture);
            }
        }

        private static byte[] GetRawBytes(UnityEngine.Texture texture, Size size)
        {
            var targetTexture = CreateTargetTexture(size);
            CopyTexture(texture, targetTexture);
            var rawTextureData = targetTexture.GetRawTextureData();
            Object.DestroyImmediate(targetTexture);
            return rawTextureData;
        }

        private static void CopyTexture(UnityEngine.Texture texture, Texture2D targetTexture2d)
        {
            var renderTexture = RenderTexture.GetTemporary(
                targetTexture2d.width,
                targetTexture2d.height,
                0,
                RenderTextureFormat.ARGB32
            );

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

        private static Texture2D CreateTargetTexture(Size size)
        {
            var texture2D = new Texture2D(size.Width, size.Height, TextureFormat.RGBA32, false);
            return texture2D;
        }

        private static Size GetTextureConvertedSize(UnityEngine.Texture texture, Size size)
        {
            var texture2dWidth = texture.width;
            var texture2dHeight = texture.height;

            var divider = 1;
            while (texture2dWidth / divider > size.Width && texture2dHeight / divider > size.Height)
                divider *= 2;

            var targetTextureWidth = texture2dWidth / divider;
            var targetTextureHeight = texture2dHeight / divider;
            return new Size(targetTextureWidth, targetTextureHeight);
        }
    }
}