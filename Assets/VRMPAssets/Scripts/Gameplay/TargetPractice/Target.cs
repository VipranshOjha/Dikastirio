using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// Represents a target object in the target practice gameplay.
    /// </summary>
    public class Target : MonoBehaviour
    {
        /// <summary>
        /// The target value.
        /// </summary>
        public int targetValue = 1;

        /// <summary>
        /// The animator for the target.
        /// </summary>
        [SerializeField] Animator m_Animator;

        /// <summary>
        /// The collider for the target.
        /// </summary>
        [SerializeField] Collider m_TriggerCollider;

        /// <summary>
        /// The particle system for the target.
        /// </summary>
        [SerializeField] ParticleSystem m_Particles;

        /// <summary>
        /// The target manager for the target.
        /// </summary>
        TargetManager m_TargetManager;

        /// <inheritdoc/>
        void Start()
        {
            m_TargetManager = GetComponentInParent<TargetManager>();
        }

        /// <summary>
        /// Enables the target object.
        /// </summary>
        public void EnableTarget()
        {
            m_Animator.SetTrigger("Activate");
            m_TriggerCollider.enabled = true;
        }

        /// <summary>
        /// Handles the local target hit event.
        /// </summary>
        public void TargetHitLocal()
        {
            m_TargetManager.HitTargetServerRpc(NetworkManager.Singleton.LocalClientId, XRINetworkGameManager.LocalPlayerColor.Value);
            PlayHitEffects(XRINetworkGameManager.LocalPlayerColor.Value);
        }

        /// <summary>
        /// Handles the network target hit event.
        /// </summary>
        /// <param name="clientId">The ID of the client that hit the target.</param>
        /// <param name="playerColor">The color of the player who hit the target.</param>
        public void TargetHitNetwork(ulong clientId, Color playerColor)
        {
            if (NetworkManager.Singleton.LocalClientId != clientId)
            {
                PlayHitEffects(playerColor);
            }

            if (m_TargetManager.IsServer)
            {
                StartCoroutine(TargetHitSequence());
            }
        }

        /// <summary>
        /// Plays the hit effects for the target.
        /// </summary>
        /// <param name="playerColor"></param>
        void PlayHitEffects(Color playerColor)
        {
            m_Animator.SetTrigger("Hit");
            m_TriggerCollider.enabled = false;
            var main = m_Particles.main;
            main.startColor = playerColor;
            m_Particles.Play();
            StartCoroutine(HideAfterHit());
        }

        IEnumerator HideAfterHit()
        {
            yield return new WaitForSeconds(3.0f);
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Handles the target hit sequence.
        /// </summary>
        /// <returns></returns>
        IEnumerator TargetHitSequence()
        {
            yield return new WaitForSeconds(2.25f);
            m_TargetManager.ServerIncreaseDifficulty();
        }
    }
}
