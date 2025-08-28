using UnityEngine;

namespace XRMultiplayer.MiniGames
{
    /// <summary>
    /// Represents a slingshot mini-game.
    /// </summary>
    [RequireComponent(typeof(TargetManager))]
    public class MiniGame_Slingshot : MiniGameBase
    {
        [SerializeField] SlingshotVisualUpdater[] m_Slingshots;

        [SerializeField] float m_PreGameHeight = 0.65f;
        [SerializeField] float m_StartGameHeight = 0.0f;

        /// <summary>
        /// The target manager.
        /// </summary>
        TargetManager m_TargetManager;

        /// <summary>
        /// The current player score.
        /// </summary>
        int m_CurrentPlayerScore = 0;


        ///<inheritdoc/>
        public override void Start()
        {
            base.Start();
            TryGetComponent(out m_TargetManager);
            SetSlingshotHeights(m_PreGameHeight);
        }

        ///<inheritdoc/>
        public override void SetupGame()
        {
            base.SetupGame();
            SetSlingshotHeights(m_PreGameHeight);
        }

        ///<inheritdoc/>
        public override void StartGame()
        {
            base.StartGame();
            m_CurrentPlayerScore = 0;
            m_TargetManager.ActivateTargets();
            SetSlingshotHeights(m_StartGameHeight);
        }

        ///<inheritdoc/>
        public override void FinishGame(bool submitScore = true)
        {
            base.FinishGame(submitScore);
            m_TargetManager.DeactivateTargets();
            m_MiniGameManager.FinishGame();

            SetSlingshotHeights(m_PreGameHeight);
        }

        ///<inheritdoc/>
        public override void UpdateGame(float deltaTime)
        {
            base.UpdateGame(deltaTime);
        }

        void SetSlingshotHeights(float height)
        {
            foreach (var slingshot in m_Slingshots)
            {
                slingshot.ResetSlingshot(height);
            }
        }

        /// <summary>
        /// Called when the local player hits a target.
        /// </summary>
        /// <param name="targetValue"></param>
        public void LocalPlayerHitTarget(int targetValue)
        {
            if (m_MiniGameManager.currentNetworkedGameState == MiniGameManager.GameState.InGame)
            {
                m_CurrentPlayerScore += targetValue;
                m_MiniGameManager.SubmitScoreServerRpc(m_CurrentPlayerScore, XRINetworkPlayer.LocalPlayer.OwnerClientId);
            }
        }
    }
}
