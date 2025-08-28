using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace XRMultiplayer
{
    /// <summary>
    /// Connects a UI slider control to a video player, allowing users to scrub to a particular time in th video.
    /// </summary>
    [RequireComponent(typeof(VideoPlayer))]
    public class VideoTimeScrubControl : MonoBehaviour
    {
        private const string TIME_FORMAT = "m':'ss";
        [SerializeField]
        [Tooltip("Video play/pause button GameObject")]
        GameObject m_ButtonPlayOrPause;
        [SerializeField, Tooltip("Use Play/Pause Butotn")]
        bool m_UsePlayPauseButton = true;

        [SerializeField]
        [Tooltip("Slider that controls the video")]
        Slider m_Slider;

        [SerializeField]
        [Tooltip("Play icon sprite")]
        Sprite m_IconPlay;

        [SerializeField]
        [Tooltip("Pause icon sprite")]
        Sprite m_IconPause;

        [SerializeField]
        [Tooltip("Play or pause button image.")]
        Image m_ButtonPlayOrPauseIcon;

        [SerializeField]
        [Tooltip("If checked, the slider will fade off after a few seconds. If unchecked, the slider will remain on.")]
        bool m_HideSliderAfterFewSeconds;

        [SerializeField, Tooltip("The Video Player Time Text")]
        TMP_Text m_VideoTimeText;

        bool m_IsDragging;
        bool m_VideoIsPlaying;
        bool m_VideoJumpPending;
        long m_LastFrameBeforeScrub;
        VideoPlayer m_VideoPlayer;

        void Start()
        {
            if (!TryGetComponent(out m_VideoPlayer))
            {
                Utils.Log("VideoTimeScrubControl: No VideoPlayer component found on this GameObject.", 2);
                return;
            }
            if (!m_VideoPlayer.playOnAwake)
            {
                m_VideoPlayer.playOnAwake = true; // Set play on awake for next enable.
                m_VideoPlayer.Play(); // Play video to load first frame.
                VideoStop(); // Stop the video to set correct state and pause frame.
            }
            else
            {
                VideoPlay(); // Play to ensure correct state.
            }


            if (!m_UsePlayPauseButton && m_ButtonPlayOrPause != null)
                m_ButtonPlayOrPause.SetActive(false);
        }

        void OnEnable()
        {
            if (m_VideoPlayer != null)
            {
                m_VideoPlayer.frame = 0;
                VideoPlay(); // Ensures correct UI state update if paused.
            }

            m_Slider.value = 0.0f;
            m_Slider.gameObject.SetActive(true);
            if (m_HideSliderAfterFewSeconds)
                StartCoroutine(HideSliderAfterSeconds());
        }

        void Update()
        {
            if (m_VideoJumpPending)
            {
                // We're trying to jump to a new position, but we're checking to make sure the video player is updated to our new jump frame.
                if (m_LastFrameBeforeScrub == m_VideoPlayer.frame)
                    return;

                // If the video player has been updated with desired jump frame, reset these values.
                m_LastFrameBeforeScrub = long.MinValue;
                m_VideoJumpPending = false;
            }

            if (!m_IsDragging && !m_VideoJumpPending)
            {
                if (m_VideoPlayer.frameCount > 0)
                {
                    var progress = (float)m_VideoPlayer.frame / m_VideoPlayer.frameCount;
                    m_Slider.value = progress;
                }
            }

            TimeSpan elapsedTime = TimeSpan.FromSeconds(m_VideoPlayer.time);
            TimeSpan totalTime = TimeSpan.FromSeconds(m_VideoPlayer.length);
            m_VideoTimeText.text = $"{elapsedTime.ToString(TIME_FORMAT)} / {totalTime.ToString(TIME_FORMAT)}";
        }

        public void OnPointerDown()
        {
            m_VideoJumpPending = true;
            VideoStop();
            VideoJump();
        }

        public void OnRelease()
        {
            m_IsDragging = false;
            VideoPlay();
            VideoJump();
        }

        IEnumerator HideSliderAfterSeconds(float duration = 1f)
        {
            yield return new WaitForSeconds(duration);
            m_Slider.gameObject.SetActive(false);
        }

        public void OnDrag()
        {
            m_IsDragging = true;
            m_VideoJumpPending = true;
        }

        void VideoJump()
        {
            m_VideoJumpPending = true;
            var frame = m_VideoPlayer.frameCount * m_Slider.value;
            m_LastFrameBeforeScrub = m_VideoPlayer.frame;
            m_VideoPlayer.frame = (long)frame;
        }

        public void PlayOrPauseVideo()
        {
            if (m_VideoIsPlaying)
            {
                VideoStop();
            }
            else
            {
                VideoPlay();
            }
        }

        void VideoStop()
        {
            m_VideoIsPlaying = false;
            m_VideoPlayer.Pause();
            m_ButtonPlayOrPauseIcon.sprite = m_IconPlay;
            if (!m_UsePlayPauseButton && m_ButtonPlayOrPause != null)
                m_ButtonPlayOrPause.SetActive(true);
        }

        void VideoPlay()
        {
            m_VideoIsPlaying = true;
            m_VideoPlayer.Play();
            m_ButtonPlayOrPauseIcon.sprite = m_IconPause;
            if (!m_UsePlayPauseButton && m_ButtonPlayOrPause != null)
                m_ButtonPlayOrPause.SetActive(false);
        }
    }
}
