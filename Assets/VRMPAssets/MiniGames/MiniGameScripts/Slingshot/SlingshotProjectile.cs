using System;
using System.Collections;
using UnityEngine;

namespace XRMultiplayer.MiniGames
{
    /// <summary>
    /// Represents a projectile for the slingshot mini-game.
    /// </summary>
    public class SlingshotProjectile : Projectile
    {
        /// <summary>
        /// Event that is triggered when the local player hits a target with the projectile.
        /// </summary>
        public Action<SlingshotProjectile, int> localPlayerHitTarget;

        /// <summary>
        /// The lifetime of the projectile.
        /// </summary>
        [SerializeField] float m_LifeTime = 10.0f;

        /// <summary>
        /// The collider for the projectile.
        /// </summary>
        [SerializeField] Collider m_Collider;

        /// <summary>
        /// The rigidbody for the projectile.
        /// </summary>
        [SerializeField] Rigidbody m_Rigidbody;

        /// <summary>
        /// Called before the first frame update.
        /// </summary>
        void Start()
        {
            Destroy(gameObject, m_LifeTime);
        }

        /// <summary>
        /// Launches the projectile with the specified parameters.
        /// </summary>
        /// <param name="launchForce">The force to launch the projectile with.</param>
        /// <param name="isLocalPlayer">Indicates whether the player launching the projectile is the local player.</param>
        /// <param name="playerColor">The color of the player launching the projectile.</param>
        public void LaunchProjectile(Vector3 launchForce, bool isLocalPlayer, Color playerColor)
        {
            Setup(isLocalPlayer, playerColor);
            m_Collider.enabled = false;
            m_Rigidbody.linearVelocity = launchForce;
            StartCoroutine(LaunchRoutine());
        }

        /// <summary>
        /// Coroutine that enables the collider after a short delay.
        /// </summary>
        IEnumerator LaunchRoutine()
        {
            yield return new WaitForSeconds(.15f);
            m_Collider.enabled = true;
        }

        /// <summary>
        /// Called when the projectile hits a target.
        /// </summary>
        /// <param name="target">The target that was hit.</param>
        protected override void HitTarget(Target target)
        {
            base.HitTarget(target);
            localPlayerHitTarget?.Invoke(this, target.targetValue);
        }
    }
}
