using System.Collections;
using OpenAI;
using PassthroughCameraSamples;
using UnityEngine;
using UnityEngine.UI;

public class PCAImageProvider : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private WebCamTextureManager webCamTextureManager;

        [Header("Encoding")]
        [Range(256, 1024)] public int maxSize = 648;
        [Range(10, 100)]   public int jpegQuality = 85;
        [Tooltip("Default PNG")] public bool useJpeg;
        
        [Header("Debugging (Optional)")]
        [SerializeField] private RawImage debugTexture;
        
        private WebCamTexture _webCamTexture;
        
        private IEnumerator Start() {
            yield return new WaitUntil(() => webCamTextureManager.WebCamTexture != null && webCamTextureManager.WebCamTexture.isPlaying);

            _webCamTexture = webCamTextureManager.WebCamTexture;
            if (debugTexture != null)
            {
                debugTexture.texture = webCamTextureManager.WebCamTexture;   
            }
            
            RealtimeConversationManager.OnRequestImage += OnRequestImage;
        }

        private void OnDisable()
        {
            RealtimeConversationManager.OnRequestImage -= OnRequestImage;
        }

        private string OnRequestImage()
        {
            if (_webCamTexture == null || !_webCamTexture.isPlaying)
                return string.Empty;

            // Perform capture and encode on the main thread to avoid Unity API usage off-thread
            return ImageEncodingUtil.CaptureDataUrlFromWebCam(_webCamTexture, maxSize, useJpeg, jpegQuality);
        }
    }
