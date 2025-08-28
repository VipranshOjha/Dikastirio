using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace XRMultiplayer.MiniGames
{
    /// <summary>
    /// Represents a mini-game called "Whack-A-Pig" where the player needs to hit pigs with a hammer.
    /// </summary>
    public class MiniGame_Whack : MiniGameBase
    {
        /// <summary>
        /// The time to wait before resetting the hammer's position.
        /// </summary>
        [SerializeField] float m_HammerResetTime = .25f;

        /// <summary>
        /// The interactable objects to use for the mini-game.
        /// </summary>
        readonly Dictionary<XRBaseInteractable, Pose> m_InteractablePoses = new();

        /// <summary>
        /// The networked gameplay to use for handling the networked gameplay logic.
        /// </summary>
        NetworkedWhackAPig m_NetworkedGameplay;

        /// <summary>
        /// The current score of the mini-game.
        /// </summary>
        int m_CurrentScore = 0;

        /// <inheritdoc/>
        public override void Start()
        {
            base.Start();

            TryGetComponent(out m_NetworkedGameplay);

            foreach (var interactable in m_GameInteractables)
            {
                if (!m_InteractablePoses.ContainsKey(interactable))
                {
                    m_InteractablePoses.Add(interactable, new Pose(interactable.transform.position, interactable.transform.rotation));
                    interactable.selectExited.AddListener(HammerDropped);
                }
            }
        }

        /// <inheritdoc/>
        void OnDestroy()
        {
            foreach (var kvp in m_InteractablePoses)
            {
                kvp.Key.selectExited.RemoveListener(HammerDropped);
            }
        }

        /// <summary>
        /// Sets up the game by resetting the current score.
        /// </summary>
        public override void SetupGame()
        {
            base.SetupGame();
            m_CurrentScore = 0;
            m_NetworkedGameplay.ResetGame();
        }

        /// <summary>
        /// Starts the game by spawning pigs if the player is the server.
        /// </summary>
        public override void StartGame()
        {
            base.StartGame();
            if (m_NetworkedGameplay.IsServer)
            {
                m_NetworkedGameplay.SpawnProcessServer();
            }
        }

        /// <summary>
        /// Finishes the game and ends the networked gameplay.
        /// </summary>
        /// <param name="submitScore">Whether to submit the score or not.</param>
        public override void FinishGame(bool submitScore = true)
        {
            base.FinishGame(submitScore);
            m_NetworkedGameplay.EndGame();
        }


        /// <summary>
        /// Called when the hammer is dropped on an interactable object.
        /// </summary>
        /// <param name="args">The interaction event arguments.</param>
        void HammerDropped(BaseInteractionEventArgs args)
        {
            XRBaseInteractable interactable = (XRBaseInteractable)args.interactableObject;
            if (m_InteractablePoses.ContainsKey(interactable))
            {
                StartCoroutine(DropHammerAfterTimeRoutine(interactable));
            }
        }

        /// <summary>
        /// Coroutine that drops the hammer after a specified time and resets the interactable's position.
        /// </summary>
        /// <param name="interactable">The interactable object.</param>
        IEnumerator DropHammerAfterTimeRoutine(XRBaseInteractable interactable)
        {
            yield return new WaitForSeconds(m_HammerResetTime);
            if (!interactable.isSelected)
            {
                Rigidbody body = interactable.GetComponent<Rigidbody>();
                bool wasKinematic = body.isKinematic;
                body.isKinematic = true;
                interactable.transform.SetPositionAndRotation(m_InteractablePoses[interactable].position, m_InteractablePoses[interactable].rotation);
                yield return new WaitForFixedUpdate();
                body.isKinematic = wasKinematic;
                foreach (var collider in interactable.colliders)
                {
                    collider.enabled = true;
                }
            }
        }

        /// <summary>
        /// Updates the local player's score and submits it to the server.
        /// </summary>
        /// <param name="pointValue">The point value to add to the score.</param>
        public void LocalPlayerScored(int pointValue)
        {
            m_CurrentScore += pointValue;
            if (m_CurrentScore < 0) m_CurrentScore = 0;
            m_MiniGameManager.SubmitScoreServerRpc(m_CurrentScore, XRINetworkPlayer.LocalPlayer.OwnerClientId);
        }
    }
}
