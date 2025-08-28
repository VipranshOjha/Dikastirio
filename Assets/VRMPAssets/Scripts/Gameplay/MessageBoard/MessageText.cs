using UnityEngine;
using TMPro;

namespace XRMultiplayer
{
    /// <summary>
    /// Represents a message text on a message board.
    /// </summary>
    public class MessageText : MonoBehaviour
    {
        /// <summary>
        /// The text component to display the message.
        /// </summary>
        [SerializeField] private TMP_Text text;

        /// <summary>
        /// The text component to display the time.
        /// </summary>
        [SerializeField] private TMP_Text timeText;

        /// <summary>
        /// Use this value for any headers or offsets that need to be added to the message text when calculating height.
        /// </summary>
        [SerializeField, Tooltip("Use this value for any headers or offsets that need to be added to the message text when calculating height.")]
        float m_BonusScale = 1.0f;

        [SerializeField] RectTransform m_RectTransform;

        /// <summary>
        /// Sets the message and time to be displayed.
        /// </summary>
        /// <param name="message">The message to be displayed.</param>
        /// <param name="time">The time to be displayed.</param>
        public void SetMessage(string message, string time)
        {
            text.SetText(message);
            timeText.SetText(time);
            Canvas.ForceUpdateCanvases();
            SnapHeight();
        }

        [ContextMenu("Snap Height")]
        void SnapHeight()
        {
            m_RectTransform.sizeDelta = new Vector2(m_RectTransform.sizeDelta.x, text.preferredHeight * m_BonusScale /* * text.fontSize*/);
            Canvas.ForceUpdateCanvases();
        }
    }
}
