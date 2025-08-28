using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XRMultiplayer.MiniGames
{
    /// <summary>
    /// Represents a slot in the scoreboard for a player.
    /// </summary>
    public class ScoreboardSlot : MonoBehaviour
    {
        /// <summary>
        /// Gets or sets the current score of the player.
        /// </summary>
        public float currentScore
        {
            get => m_Score;
            set => m_Score = value;
        }
        /// <summary>
        /// Internal value that gets or sets the current score of the player.
        /// </summary>
        float m_Score;

        /// <summary>
        /// External value that gets or sets a value indicating whether the player has finished the game.
        /// </summary>
        public bool isFinished
        {
            get => m_IsFinished;
            set => m_IsFinished = value;
        }

        /// <summary>
        /// Internal value that gets or sets a value indicating whether the player has finished the game.
        /// </summary>
        bool m_IsFinished;

        /// <summary>
        /// External value that gets or sets a value indicating whether the player is ready.
        /// </summary>
        public bool isReady
        {
            get => m_IsReady;
            set => m_IsReady = value;
        }

        /// <summary>
        /// Internal value that gets or sets a value indicating whether the player is ready.
        /// </summary>
        bool m_IsReady;

        /// <summary>
        /// The text for the place of the player.
        /// </summary>
        [SerializeField] TMP_Text m_PlaceText;

        /// <summary>
        /// The text for the name of the player.
        /// </summary>
        [SerializeField] TMP_Text m_PlayerNameText;

        /// <summary>
        /// The text for the score of the player.
        /// </summary>
        [SerializeField] TMP_Text m_PlayerScoreText;

        /// <summary>
        /// The icon to indicate the player is ready.
        /// </summary>
        [SerializeField] Image m_ReadyIcon;

        /// <summary>
        /// The icon to indicate the player is ready.
        /// </summary>
        [SerializeField] GameObject m_PlayerIcon;

        /// <summary>
        /// The object to display when the slot is open.
        /// </summary>
        [SerializeField] GameObject m_OpenObject;

        /// <summary>
        /// The object to display when the slot is closed.
        /// </summary>
        [SerializeField] GameObject m_ClosedObject;

        /// <summary>
        /// Sets up the player slot with the specified place and player name.
        /// </summary>
        /// <param name="place">The place of the player.</param>
        /// <param name="playerName">The name of the player.</param>
        public void SetupPlayerSlot(int place, string playerName)
        {
            m_OpenObject.SetActive(false);
            m_ClosedObject.SetActive(true);
            m_PlayerIcon.SetActive(true);
            m_PlaceText.enabled = false;
            m_PlayerNameText.text = playerName;
            UpdatePlace(place);
            ToggleReady(false);
            m_PlayerScoreText.text = "";
            m_IsFinished = false;
            m_IsReady = false;
        }

        /// <summary>
        /// Sets the slot as open.
        /// </summary>
        public void SetSlotOpen()
        {
            m_OpenObject.SetActive(true);
            m_ClosedObject.SetActive(false);
            m_IsFinished = false;
        }

        /// <summary>
        /// Updates the score of the player.
        /// </summary>
        /// <param name="score">The new score of the player.</param>
        /// <param name="gameType">The type of the game.</param>
        public void UpdateScore(float score, MiniGameBase.GameType gameType)
        {
            m_PlayerIcon.SetActive(false);
            m_PlaceText.enabled = true;
            m_PlayerScoreText.enabled = true;
            m_ReadyIcon.enabled = false;
            currentScore = score;

            if (gameType == MiniGameBase.GameType.Time)
            {
                TimeSpan time = TimeSpan.FromSeconds(score);
                m_PlayerScoreText.text = time.ToString("mm':'ss'.'ff");
            }
            else
            {
                m_PlayerScoreText.text = score.ToString("N0");
            }
        }

        /// <summary>
        /// Updates the place of the player.
        /// </summary>
        /// <param name="place">The new place of the player.</param>
        public void UpdatePlace(int place)
        {
            m_PlaceText.text = $"{place}<voffset=.5em><size=3>{Utils.GetOrdinal(place)}</voffset></size>";
        }

        /// <summary>
        /// Toggles the ready state of the player.
        /// </summary>
        /// <param name="toggle">The new ready state.</param>
        public void ToggleReady(bool toggle)
        {
            m_IsReady = toggle;
            m_PlayerScoreText.enabled = false;
            m_ReadyIcon.enabled = true;
            m_ReadyIcon.color = toggle ? Color.green : Color.red;
        }
    }
}
