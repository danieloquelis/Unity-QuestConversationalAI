using System;
using UnityEngine;

public static class ImageEncodingUtil
{
    /// <summary>
    /// Captures a frame from a <see cref="WebCamTexture"/>, downsizes it to fit within maxSize while preserving
    /// aspect ratio, and returns a data URL (PNG or JPEG) as a string. Must be called on the main thread.
    /// </summary>
    public static string CaptureDataUrlFromWebCam(WebCamTexture cam, int maxSize, bool useJpeg, int jpegQuality)
    {
        if (cam == null || !cam.isPlaying) return string.Empty;

        var srcW = cam.width;
        var srcH = cam.height;
        if (srcW <= 0 || srcH <= 0) return string.Empty;

        var scale = 1f;
        var maxDim = Mathf.Max(srcW, srcH);
        if (maxDim > maxSize)
        {
            scale = (float)maxSize / maxDim;
        }

        var dstW = Mathf.Max(1, Mathf.RoundToInt(srcW * scale));
        var dstH = Mathf.Max(1, Mathf.RoundToInt(srcH * scale));

        RenderTexture rt = null;
        Texture2D tex = null;
        try
        {
            rt = new RenderTexture(dstW, dstH, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(cam, rt);

            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            tex = new Texture2D(dstW, dstH, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, dstW, dstH), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;

            var bytes = useJpeg ? tex.EncodeToJPG(jpegQuality) : tex.EncodeToPNG();
            var mime = useJpeg ? "image/jpeg" : "image/png";
            return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
        }
        finally
        {
            if (tex != null) UnityEngine.Object.Destroy(tex);
            if (rt != null)
            {
                rt.Release();
                UnityEngine.Object.Destroy(rt);
            }
        }
    }
}


