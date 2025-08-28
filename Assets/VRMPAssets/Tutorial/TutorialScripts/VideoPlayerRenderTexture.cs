using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace XRMultiplayer
{
    /// <summary>
    /// Create a RenderTexture for rendering video to a target renderer.
    /// </summary>
    [RequireComponent(typeof(VideoPlayer))]
    public class VideoPlayerRenderTexture : MonoBehaviour
    {
        const string k_ShaderName = "Unlit/Texture";

        [SerializeField]
        [Tooltip("The target Image which will display the video.")]
        RawImage m_Image;

        [SerializeField] Vector2 m_ImageSize = new Vector2(1920, 1080);
        [SerializeField] float m_Scale = 1.0f;
        [SerializeField] float m_ZOffset = -1;

        [SerializeField]
        [Tooltip("The width of the RenderTexture which will be created.")]
        int m_RenderTextureWidth = 1920;

        [SerializeField]
        [Tooltip("The height of the RenderTexture which will be created.")]
        int m_RenderTextureHeight = 1080;

        [SerializeField]
        [Tooltip("The bit depth of the depth channel for the RenderTexture which will be created.")]
        int m_RenderTextureDepth;

        VideoPlayer m_VideoPlayer;


        void OnValidate()
        {
            if (m_Image == null) return;
            m_Image.rectTransform.sizeDelta = m_ImageSize * m_Scale;
            m_Image.rectTransform.localPosition = new Vector3(m_Image.rectTransform.localPosition.x, m_Image.rectTransform.localPosition.y, m_ZOffset);
        }

        void Awake()
        {
            if (!TryGetComponent(out m_VideoPlayer))
            {
                Utils.Log("VideoPlayerRenderTexture requires a VideoPlayer component.", 2);
                return;
            }
            var renderTexture = new RenderTexture(m_RenderTextureWidth, m_RenderTextureHeight, m_RenderTextureDepth);
            renderTexture.Create();
            m_Image.material.mainTexture = renderTexture;
            m_VideoPlayer.targetTexture = renderTexture;
        }
    }
}
