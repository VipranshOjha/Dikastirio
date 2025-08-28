using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace XRMultiplayer.MiniGames
{
    /// <summary>
    /// A networked slingshot that shoots projectiles.
    /// </summary>
    [RequireComponent(typeof(SlingshotVisualUpdater))]
    public class Slingshot : MonoBehaviour
    {
        /// <summary>
        /// The projectile prefab to shoot from the slingshot.
        /// </summary>
        [SerializeField] GameObject m_ProjectilePrefab;

        /// <summary>
        /// The projectile proxy object to show when the bucket is held.
        /// </summary>
        [SerializeField] GameObject m_ProjectileProxyObject;

        /// <summary>
        /// The bucket interactable to use for setting the power of the slingshot.
        /// </summary>
        [SerializeField] NetworkPhysicsInteractable m_BucketInteractable;

        /// <summary>
        /// The time to wait before resetting the proxy object.
        /// </summary>
        [SerializeField] float m_ResetTime = 1.0f;

        /// <summary>
        /// The colliders to ignore when shooting the projectile.
        /// </summary>
        [SerializeField] Collider[] m_CollidersToIgnore;

        /// <summary>
        /// The slingshot visual updater to use for updating the slingshot visuals.
        /// </summary>
        SlingshotVisualUpdater m_SlingshotVisualUpdater;

        /// <summary>
        /// The slingshot mini game to use for handling the mini game logic.
        /// </summary>
        MiniGame_Slingshot m_MiniGame;

        /// <summary>
        /// Called when the script instance is being loaded.
        /// </summary>
        private void Start()
        {
            TryGetComponent(out m_SlingshotVisualUpdater);
            m_MiniGame = GetComponentInParent<MiniGame_Slingshot>();
        }

        /// <summary>
        /// Called when the bucket's held state changes.
        /// </summary>
        /// <param name="old">The previous held state.</param>
        /// <param name="current">The current held state.</param>
        public void OnBucketHeldChanged(bool current)
        {
            if (!current)
            {
                ShootProjectile();
            }
            else
            {
                if (m_BucketInteractable.IsOwner)
                {
                    m_SlingshotVisualUpdater.trajectoryLineRenderer.enabled = true;
                }
            }
        }

        /// <summary>
        /// Shoots a projectile from the slingshot.
        /// </summary>
        private void ShootProjectile()
        {
            m_ProjectileProxyObject.SetActive(false);
            SlingshotProjectile projectile = Instantiate(m_ProjectilePrefab, m_BucketInteractable.transform.position, Quaternion.identity).GetComponent<SlingshotProjectile>();
            XRINetworkGameManager.Instance.GetPlayerByID(m_BucketInteractable.OwnerClientId, out XRINetworkPlayer player);
            projectile.LaunchProjectile(m_SlingshotVisualUpdater.GetShotForce(), player.IsLocalPlayer, player.playerColor);

            foreach (var collider in m_CollidersToIgnore)
            {
                Physics.IgnoreCollision(projectile.GetComponent<Collider>(), collider);
            }

            if (m_BucketInteractable.IsOwner)
            {
                projectile.localPlayerHitTarget += OnLocalPlayerHitTarget;
                m_SlingshotVisualUpdater.ResetBucketPosition();
                ResetProxyObjectServerRpc();
                m_SlingshotVisualUpdater.trajectoryLineRenderer.enabled = false;
            }
        }

        /// <summary>
        /// Resets the proxy object on the server.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        private void ResetProxyObjectServerRpc()
        {
            ResetProxyObjectClientRpc();
        }

        /// <summary>
        /// Resets the proxy object on the clients.
        /// </summary>
        [ClientRpc]
        private void ResetProxyObjectClientRpc()
        {
            StartCoroutine(ResetProxyAfterTime());
        }

        /// <summary>
        /// Resets the proxy object after a specified time.
        /// </summary>
        /// <returns>An enumerator to control the coroutine.</returns>
        private IEnumerator ResetProxyAfterTime()
        {
            yield return new WaitForSeconds(m_ResetTime);
            m_ProjectileProxyObject.SetActive(true);
        }

        /// <summary>
        /// Called when the local player hits a target with the projectile.
        /// </summary>
        /// <param name="projectile">The projectile that hit the target.</param>
        /// <param name="targetValue">The value of the target hit.</param>
        private void OnLocalPlayerHitTarget(SlingshotProjectile projectile, int targetValue)
        {
            projectile.localPlayerHitTarget -= OnLocalPlayerHitTarget;
            m_MiniGame.LocalPlayerHitTarget(targetValue);
        }
    }
}
