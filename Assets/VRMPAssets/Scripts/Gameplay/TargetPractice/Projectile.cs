using System;
using System.Collections;
using UnityEngine;
namespace XRMultiplayer
{
    /// <summary>
    /// Represents a projectile in the game.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        /// <summary>
        /// The trail renderer for the projectile.
        /// </summary>
        [SerializeField] protected TrailRenderer m_TrailRenderer;

        [SerializeField] protected float m_Lifetime = 10.0f;

        /// <summary>
        /// The previous position of the projectile.
        /// </summary>
        Vector3 m_PrevPos = Vector3.zero;

        /// <summary>
        /// The raycast hit for the projectile.
        /// </summary>
        RaycastHit m_Hit;

        /// <summary>
        /// Indicates whether the projectile has hit a target.
        /// </summary>
        bool m_HasHitTarget = false;

        /// <summary>
        /// Indicates whether the projectile belongs to the local player.
        /// </summary>
        bool m_LocalPlayerProjectile;

        Action<Projectile> m_OnReturnToPool;

        Rigidbody m_Rigidybody;


        /// <summary>
        /// Sets up the projectile with the specified parameters.
        /// </summary>
        /// <param name="localPlayer">Indicates whether the projectile belongs to the local player.</param>
        /// <param name="playerColor">The color of the player.</param>
        public void Setup(bool localPlayer, Color playerColor, Action<Projectile> returnToPoolAction = null)
        {
            if (m_Rigidybody == null)
            {
                TryGetComponent(out m_Rigidybody);
            }

            m_LocalPlayerProjectile = localPlayer;
            m_TrailRenderer.startColor = playerColor;
            m_TrailRenderer.endColor = playerColor;
            m_TrailRenderer.Clear();
            m_PrevPos = transform.position;
            if (returnToPoolAction != null)
            {
                m_OnReturnToPool = returnToPoolAction;
                StartCoroutine(ResetProjectileAfterTime());
            }
        }

        IEnumerator ResetProjectileAfterTime()
        {
            yield return new WaitForSeconds(m_Lifetime);
            ResetProjectile();
        }

        /// <inheritdoc/>
        private void FixedUpdate()
        {
            if (!m_LocalPlayerProjectile || m_HasHitTarget) return;
            if (Physics.Linecast(m_PrevPos, transform.position, out m_Hit))
            {
                if (m_Hit.transform.CompareTag("Target"))
                {
                    HitTarget(m_Hit.transform.GetComponentInParent<Target>());
                }

                CheckForInteractableHit(m_Hit.transform);
            }

            m_PrevPos = transform.position;
        }

        /// <inheritdoc/>
        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Target"))
            {
                HitTarget(other.GetComponentInParent<Target>());
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            if (!m_LocalPlayerProjectile) return;
            CheckForInteractableHit(collision.transform);
        }

        void CheckForInteractableHit(Transform t)
        {
            NetworkPhysicsInteractable networkPhysicsInteractable = t.GetComponentInParent<NetworkPhysicsInteractable>();
            if (networkPhysicsInteractable != null)
            {
                networkPhysicsInteractable.RequestOwnership();
            }
        }

        /// <summary>
        /// Called when the projectile hits a target.
        /// </summary>
        /// <param name="target">The target that was hit.</param>
        protected virtual void HitTarget(Target target)
        {
            target.TargetHitLocal();
            m_HasHitTarget = true;
        }

        public void ResetProjectile()
        {
            StopAllCoroutines();
            m_OnReturnToPool?.Invoke(this);
        }
    }
}
