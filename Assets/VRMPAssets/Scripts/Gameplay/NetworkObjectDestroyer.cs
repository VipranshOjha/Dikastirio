using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// This class is responsible for handling the networked despawning of objects based within
    /// a trigger volume. It also resets the player controller if they are within the bounds.
    /// It also handles the spawning of particles when an object is despawned.
    /// </summary>
    public class NetworkObjectDestroyer : NetworkBehaviour
    {
        /// <summary>
        /// The sub-trigger that triggers the action.
        /// </summary>
        [SerializeField] SubTrigger m_SubTrigger;

        /// <summary>
        /// The Y offset for the particles that are spawned.
        /// </summary>
        [SerializeField] float m_YOffset = .1f;

        /// <summary>
        /// The list of scene interactables that cannot be destroyed.
        /// </summary>
        [SerializeField] List<NetworkBaseInteractable> m_UndestroyableInteractables;

        readonly List<NetworkBaseInteractable> m_DestroyedInteractables = new();

        Pooler m_ParticlePooler;

        /// <summary>
        /// Awake is called when the script instance is being loaded.
        /// </summary>
        void Awake()
        {
            if (!TryGetComponent(out m_ParticlePooler))
            {
                Utils.LogError("NetworkObjectDestroyer requires a Pooler component to be attached to the same GameObject.");
                return;
            };
            m_SubTrigger.OnTriggerAction += Triggered;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            m_SubTrigger.OnTriggerAction -= Triggered;
        }

        /// <summary>
        /// Event handler for the trigger action.
        /// </summary>
        /// <param name="other">The collider that triggered the action.</param>
        /// <param name="entered">A flag indicating if the collider entered or exited the trigger.</param>
        void Triggered(Collider other, bool entered)
        {
            if (!entered) return;
            NetworkBaseInteractable networkBaseInteractable = other.GetComponentInParent<NetworkBaseInteractable>();

            if (networkBaseInteractable != null)
            {
                if (m_UndestroyableInteractables.Contains(networkBaseInteractable) || networkBaseInteractable.isInteracting) return;

                // This will prevent objects with multiple colliders from being destroyed multiple times.
                if (m_DestroyedInteractables.Contains(networkBaseInteractable)) return;
                m_DestroyedInteractables.Add(networkBaseInteractable);

                Vector3 position = networkBaseInteractable.transform.position + Vector3.up * m_YOffset;
                if (!networkBaseInteractable.IsSpawned)
                {
                    Destroy(networkBaseInteractable);
                    PlayDestroyEffect(position);
                    // PlayDestroyEffectRpc(position);
                }
                else if (IsServer)
                {
                    networkBaseInteractable.NetworkObject.Despawn();
                    PlayDestroyEffectRpc(position);
                }
            }
            else
            {
                if (other.TryGetComponent(out CharacterResetter playerResetter))
                {
                    PlayDestroyEffectRpc(playerResetter.transform.position);
                    // PlayDestroyEffectServerRpc(playerResetter.transform.position, NetworkManager.Singleton.LocalClientId);
                    playerResetter.ResetPlayer();
                }

                if (other.TryGetComponent(out Projectile projectile))
                {
                    PlayDestroyEffectRpc(projectile.transform.position);
                    // Destroy(Instantiate(m_DestroyParticles, projectile.transform.position, Quaternion.identity), 1.0f);
                    projectile.ResetProjectile();
                }
            }
        }

        /// <summary>
        /// Plays the destroy effect at the specified position.
        /// </summary>
        /// <param name="position">The position at which to play the destroy effect.</param>
        void PlayDestroyEffect(Vector3 position)
        {
            GameObject particleObject = m_ParticlePooler.GetItem();
            particleObject.transform.position = position;
            StartCoroutine(ReturnParticleToPool(particleObject));
            // var particles = particleObject.GetComponent<ParticleSystem>();
            // particles.Play();

            // var main = particles.main;
            // main.stopAction = m_ParticlePooler.ReturnItem(particleObject);
            // Destroy(Instantiate(m_DestroyParticles, position, Quaternion.identity), 1.0f);
            // PlayDestroyEffectServerRpc(position, NetworkManager.Singleton.LocalClientId);
        }

        IEnumerator ReturnParticleToPool(GameObject particleObject)
        {
            yield return new WaitForSeconds(1.0f);
            m_ParticlePooler.ReturnItem(particleObject);
        }

        // /// <summary>
        // /// Server RPC method for playing the destroy effect on the server.
        // /// </summary>
        // /// <param name="position">The position at which to play the destroy effect.</param>
        // /// <param name="clientId">The client ID of the player triggering the effect.</param>
        // [ServerRpc(RequireOwnership = false)]
        // void PlayDestroyEffectServerRpc(Vector3 position, ulong clientId)
        // {
        //     PlayDestroyEffectClientRpc(position, clientId);
        // }

        // /// <summary>
        // /// Client RPC method for playing the destroy effect on the clients.
        // /// </summary>
        // /// <param name="position">The position at which to play the destroy effect.</param>
        // /// <param name="clientId">The client ID of the player triggering the effect.</param>
        // [ClientRpc]
        // void PlayDestroyEffectClientRpc(Vector3 position, ulong clientId)
        // {
        //     if (clientId != NetworkManager.Singleton.LocalClientId)
        //         Destroy(Instantiate(m_DestroyParticles, position, Quaternion.identity), 1.0f);
        // }

        [Rpc(SendTo.Everyone)]
        void PlayDestroyEffectRpc(Vector3 position)
        {
            PlayDestroyEffect(position);
            // if (clientId != NetworkManager.Singleton.LocalClientId)
            //     Destroy(Instantiate(m_DestroyParticles, position, Quaternion.identity), 1.0f);
        }
    }
}
