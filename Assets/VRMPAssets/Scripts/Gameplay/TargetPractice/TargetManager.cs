using Unity.Netcode;
using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// Manages the targets in the target practice game.
    /// </summary>
    public class TargetManager : NetworkBehaviour
    {
        /// <summary>
        /// The targets in the game.
        /// </summary>
        public Target[] targets;

        /// <summary>
        /// The difficulty level of the game.
        /// </summary>
        protected NetworkVariable<int> m_DifficultyLevel = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        /// <summary>
        /// Indicates whether the targets are activated.
        /// </summary>
        protected bool m_activated = false;

        /// <inheritdoc/>
        public virtual void Start()
        {
            // Subscribe to difficulty level change events
            m_DifficultyLevel.OnValueChanged += OnDifficultyChanged;

            // Deactivate all targets
            for (int i = 0; i < targets.Length; i++)
            {
                targets[i].gameObject.SetActive(false);
            }
        }

        /// <inheritdoc/>
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // If the difficulty level is already set, manually trigger the difficulty change event
            if (m_DifficultyLevel.Value != -1)
            {
                OnDifficultyChanged(-1, m_DifficultyLevel.Value);
            }
        }

        /// <summary>
        /// Activates the targets.
        /// </summary>
        public void ActivateTargets()
        {
            m_activated = true;

            // Set the difficulty level to 0 on the server
            if (IsServer)
            {
                m_DifficultyLevel.Value = 0;
            }
        }

        /// <summary>
        /// Deactivates the targets.
        /// </summary>
        public void DeactivateTargets()
        {
            m_activated = false;

            // Set the difficulty level to -1 on the server
            if (IsServer)
            {
                m_DifficultyLevel.Value = -1;
            }
        }

        /// <summary>
        /// Called when the difficulty level changes.
        /// </summary>
        /// <param name="old">The old difficulty level.</param>
        /// <param name="current">The current difficulty level.</param>
        void OnDifficultyChanged(int old, int current)
        {
            // Activate the target corresponding to the current difficulty level and enable it
            for (int i = 0; i < targets.Length; i++)
            {
                targets[i].gameObject.SetActive(current == i);
                if (current == i)
                {
                    targets[i].EnableTarget();
                }
            }
        }

        /// <summary>
        /// Sets the difficulty level on the server.
        /// </summary>
        /// <param name="newDifficulty">The new difficulty level.</param>
        public void ServerSetDifficulty(int newDifficulty)
        {
            m_DifficultyLevel.Value = newDifficulty;
        }

        /// <summary>
        /// Increases the difficulty level on the server.
        /// </summary>
        public void ServerIncreaseDifficulty()
        {
            m_DifficultyLevel.Value = (m_DifficultyLevel.Value + 1) % targets.Length;
        }

        /// <summary>
        /// Server RPC method called when a target is hit.
        /// </summary>
        /// <param name="clientId">The client ID of the player who hit the target.</param>
        /// <param name="playerColor">The color of the player who hit the target.</param>
        [ServerRpc(RequireOwnership = false)]
        public void HitTargetServerRpc(ulong clientId, Color playerColor)
        {
            HitTargetClientRpc(clientId, playerColor);
        }

        /// <summary>
        /// Client RPC method called when a target is hit.
        /// </summary>
        /// <param name="clientId">The client ID of the player who hit the target.</param>
        /// <param name="playerColor">The color of the player who hit the target.</param>
        [ClientRpc]
        public void HitTargetClientRpc(ulong clientId, Color playerColor)
        {
            // Call the TargetHitNetwork method on the target corresponding to the current difficulty level
            targets[Mathf.Clamp(m_DifficultyLevel.Value, 0, targets.Length - 1)].TargetHitNetwork(clientId, playerColor);
        }
    }
}
