using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace XRMultiplayer.MiniGames
{
    /// <summary>
    /// Base class for mini-games.
    /// </summary>
    [RequireComponent(typeof(MiniGameManager))]
    public class MiniGameBase : MonoBehaviour, IMiniGame
    {
        /// <summary>
        /// External flag for the game being finished.
        /// </summary>
        public bool finished
        {
            get => m_Finished;
            set => m_Finished = value;
        }

        /// <summary>
        /// Internal flag for the game being finished.
        /// </summary>
        protected bool m_Finished;

        /// <summary>
        /// The name of the game.
        /// </summary>
        public string gameName;

        /// <summary>
        /// The type of game.
        /// </summary>
        public enum GameType { Time, Score }

        /// <summary>
        /// The current game type.
        /// </summary>
        public GameType currentGameType
        {
            get => m_GameType;
            set => m_GameType = value;
        }

        /// <summary>
        /// The game type.
        /// </summary>
        [SerializeField] GameType m_GameType;

        /// <summary>
        /// The length of the game.
        /// </summary>
        [SerializeField] protected float m_GameLength = 90.0f;

        [SerializeField] protected XRBaseInteractable[] m_GameInteractables;

        /// <summary>
        /// Manager for the mini-game.
        /// </summary>
        protected MiniGameManager m_MiniGameManager;

        /// <summary>
        /// Manager for the interaction.
        /// </summary>
        protected XRInteractionManager m_InteractionManager;


        /// <summary>
        /// The current timer for the game.
        /// </summary>
        protected float m_CurrentTimer;

        /// <summary>
        /// Flag indicating whether the game ending notification has been sent.
        /// </summary>
        bool m_GameEndingNotificationSent = false;

        ///<inheritdoc/>
        public virtual void Start()
        {
            TryGetComponent(out m_MiniGameManager);
            m_CurrentTimer = m_GameLength;
            m_InteractionManager = FindFirstObjectByType<XRInteractionManager>();
        }

        /// <summary>
        /// Sets up the mini-game.
        /// </summary>
        public virtual void SetupGame()
        {
            if (m_GameType == GameType.Score)
            {
                m_CurrentTimer = m_GameLength;
            }
        }

        /// <summary>
        /// Starts the mini-game.
        /// </summary>
        public virtual void StartGame()
        {
            m_GameEndingNotificationSent = false;
            m_Finished = false;
        }

        /// <summary>
        /// Updates the mini-game.
        /// </summary>
        /// <param name="deltaTime">The time since the last frame.</param>
        public virtual void UpdateGame(float deltaTime)
        {
            m_CurrentTimer -= deltaTime;
            if (m_GameType == GameType.Score)
            {
                m_MiniGameManager.m_GameStateText.text = $"Time: {m_CurrentTimer:F0}";
            }
            CheckForGameEnd();
        }

        protected void CheckForGameEnd()
        {
            if (m_CurrentTimer <= 3.5f & !m_GameEndingNotificationSent)
            {
                m_GameEndingNotificationSent = true;
                StartCoroutine(CheckForGameEndingRoutine());
            }
        }

        /// <summary>
        /// Finishes the mini-game.
        /// </summary>
        /// <param name="submitScore">Flag indicating whether to submit the score.</param>
        public virtual void FinishGame(bool submitScore = true)
        {
            RemoveInteractables();
            m_Finished = true;
            m_CurrentTimer = m_GameLength;
        }

        /// <summary>
        /// Coroutine for displaying the game end notification.
        /// </summary>
        /// <returns>An IEnumerator.</returns>
        IEnumerator CheckForGameEndingRoutine()
        {
            int seconds = 3;
            while (seconds > 0)
            {
                if (m_MiniGameManager.LocalPlayerInGame)
                {
                    PlayerHudNotification.Instance.ShowText($"Game Ending In {seconds}");
                }
                yield return new WaitForSeconds(1.0f);
                seconds--;
            }
            if (m_MiniGameManager.LocalPlayerInGame)
            {
                PlayerHudNotification.Instance.ShowText($"Game Complete!");
            }

            if (m_MiniGameManager.IsServer && m_MiniGameManager.currentNetworkedGameState == MiniGameManager.GameState.InGame)
                m_MiniGameManager.StopGameServerRpc();
        }

        /// <summary>
        /// Removes the interactables from the mini-game.
        /// </summary>
        public virtual void RemoveInteractables()
        {
            foreach (IXRInteractable interactable in m_GameInteractables)
            {
                m_InteractionManager.CancelInteractableSelection((IXRSelectInteractable)interactable);
            }
        }
    }

    /// <summary>
    /// Interface for mini-games.
    /// </summary>
    public interface IMiniGame
    {
        /// <summary>
        /// Sets up the mini-game.
        /// </summary>
        void SetupGame();

        /// <summary>
        /// Starts the mini-game.
        /// </summary>
        void StartGame();

        /// <summary>
        /// Updates the mini-game.
        /// </summary>
        /// <param name="deltaTime">The time since the last frame.</param>
        void UpdateGame(float deltaTime);

        /// <summary>
        /// Finishes the mini-game.
        /// </summary>
        /// <param name="submitScore">Flag indicating whether to submit the score.</param>
        void FinishGame(bool submitScore = true);
    }
}
