using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace XRMultiplayer
{
    /// <summary>
    /// NetworkInteractableGrab adds some extra functionality to the NetworkBaseInteractable class
    /// to allow for better immediate feedback on non owner players.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(ClientNetworkTransform))]
    public class NetworkPhysicsInteractable : NetworkBaseInteractable
    {
        /// <summary>
        /// Enabling this will call Request Ownership on collision with non-local NetworkPhysicsInteractables.
        /// </summary>
        [Header("Collision Based Ownership Transfer"), SerializeField, Tooltip("Enabling this will call Request Ownership on collision with non-local NetworkPhysicsInteractables.")]
        protected bool m_AllowCollisionOwnershipExchange = true;

        /// <summary>
        /// Determines the minimum velocity magnitude required to request ownership on collision.
        /// </summary>
        [SerializeField, Tooltip("Determines the minimum velocity magnitude required to request ownership on collision.")]
        protected float m_MinExchangeVelocityMagitude = .025f;

        /// <summary>
        /// Sets the <see cref="Rigidbody.constraints"/> to <see cref="RigidbodyConstraints.FreezeAll"/> on spawn.
        /// </summary>
        /// <remarks>
        /// This is useful for objects that you want to be non interactable on spawn.
        /// </remarks>
        [Header("Spawn Options")]
        public bool spawnLocked = true;

        /// <summary>
        /// Used to get the current networked value of <see cref="m_LockedOnSpawn.Value"/>.
        /// </summary>
        public bool lockedOnSpawn => m_LockedOnSpawn.Value;
        protected NetworkVariable<bool> m_LockedOnSpawn = new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        /// <summary>
        /// This flag is used to prevent multiple attempts at requesting ownership.
        /// </summary>
        protected bool m_RequestingOwnership = false;

        /// <summary>
        /// Client Network Transform used for synchronizing transform data.
        /// </summary>
        protected ClientNetworkTransform m_ClientNetworkTransform;

        protected Rigidbody m_Rigidbody;
        protected Collider m_Collider;

        /// <summary>
        /// This flag is used to determine if the object should be reset on disconnect.
        /// </summary>
        protected NetworkVariable<bool> m_ResettingObject = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        /// <summary>
        /// Coroutine for checking ownership after a request has been made based on current user RTT to server.
        /// </summary>
        protected IEnumerator checkOwnershipRoutine;

        bool m_PauseVelocityCalcuations = false;
        Vector3 m_AverageVelocity;
        Vector3[] m_CurrentCalculatedVelocity;
        int m_FramesToCalculate = 3;
        int m_CurrentFrame = 0;
        Vector3 m_PrevPos;

        /// <inheritdoc/>
        public override void Awake()
        {
            base.Awake();

            // Get associated required components
            if (!TryGetComponent(out m_Rigidbody) || !TryGetComponent(out m_ClientNetworkTransform))
            {
                Utils.Log("Missing Components! Disabling Now.", 2);
                enabled = false;
                return;
            }
            m_CurrentCalculatedVelocity = new Vector3[m_FramesToCalculate];
            m_ClientNetworkTransform.enabled = false;
            m_Collider = GetComponentInChildren<Collider>();
        }

        void Update()
        {
            if (m_PauseVelocityCalcuations) return;
            Vector3 velocity = (transform.position - m_PrevPos) / Time.deltaTime;
            m_CurrentCalculatedVelocity[m_CurrentFrame] = velocity;
            m_CurrentFrame = (m_CurrentFrame + 1) % m_FramesToCalculate;
            m_PrevPos = transform.position;

            m_AverageVelocity = GetWorldVelocity();
        }

        Vector3 GetWorldVelocity()
        {
            Vector3 averageVelocity = Vector3.zero;
            for (int i = 0; i < m_FramesToCalculate; i++)
            {
                averageVelocity += m_CurrentCalculatedVelocity[i];
            }
            return averageVelocity / m_FramesToCalculate;
        }

        /// <inheritdoc/>
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            m_ResettingObject.OnValueChanged += OnObjectPhysicsReset;
            if (IsOwner)
            {
                m_LockedOnSpawn.Value = spawnLocked;
                m_Rigidbody.constraints = spawnLocked ? RigidbodyConstraints.FreezeAll : RigidbodyConstraints.None;
            }

            m_ClientNetworkTransform.enabled = syncSelect;
        }

        /// <inheritdoc/>
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (m_ResetObjectOnDisconnect & !m_Rigidbody.isKinematic)
            {
                ResetObjectPhysics();
            }
        }

        /// <inheritdoc/>
        protected override void OnIsInteractingChanged(bool oldValue, bool newValue)
        {
            base.OnIsInteractingChanged(oldValue, newValue);
            if (IsOwner && newValue && m_LockedOnSpawn.Value)
            {
                m_LockedOnSpawn.Value = false;
            }

            if (!newValue)
            {
                m_Collider.enabled = false;
                m_Collider.enabled = true;
            }
        }

        /// <summary>
        /// This function is called when the <see cref="m_ResettingObject"/> value changes.
        /// </summary>
        void OnObjectPhysicsReset(bool oldValue, bool currentValue)
        {
            if (currentValue)
            {
                m_CurrentCalculatedVelocity = new Vector3[m_FramesToCalculate];
                m_PauseVelocityCalcuations = true;
            }
            else
            {
                m_PauseVelocityCalcuations = false;
            }
        }

        /// <inheritdoc/>
        public override void ResetObject()
        {
            base.ResetObject();
            ResetObjectPhysics();
        }

        /// <summary>
        /// Resets the object to its original state.
        /// </summary>
        public void ResetObjectPhysics()
        {
            m_CurrentCalculatedVelocity = new Vector3[m_FramesToCalculate];

            // Take a snapshot of the current rigidbody
            int snapShotRigidbodyInterpolation = (int)m_Rigidbody.interpolation;
            bool wasKinematic = m_Rigidbody.isKinematic;
            if (!m_Rigidbody.isKinematic)
            {
                m_Rigidbody.linearVelocity = Vector3.zero;
                m_Rigidbody.angularVelocity = Vector3.zero;
            }
            m_Rigidbody.interpolation = RigidbodyInterpolation.None;
            m_Rigidbody.isKinematic = true;

            if (IsOwner && NetworkManager.IsConnectedClient & !NetworkManager.Singleton.ShutdownInProgress)
                m_ResettingObject.Value = true;

            // Wait for a fixed update to reset the object.
            StartCoroutine(ResetPhysicsRoutine(wasKinematic, snapShotRigidbodyInterpolation));
        }

        IEnumerator ResetPhysicsRoutine(bool wasKinematic, int interpolation)
        {
            // Since the Rigidbody is using Interpolate, we need to disable Kinematic for a frame to reset the object.
            yield return new WaitForFixedUpdate();
            m_Rigidbody.interpolation = (RigidbodyInterpolation)interpolation;
            m_Rigidbody.isKinematic = wasKinematic;
            if (IsOwner && NetworkManager.IsConnectedClient & !NetworkManager.Singleton.ShutdownInProgress)
                m_ResettingObject.Value = false;
        }

        /// <inheritdoc/>
        public override void OnSelectEnteredLocal(BaseInteractionEventArgs args)
        {
            base.OnSelectEnteredLocal(args);

            // Return out early if the interactor is ignoring sockets or not syncing select.
            if (m_IgnoreSocketSelectedCallback && args.interactorObject.transform.GetComponent<XRSocketInteractor>() != null) return;

            // Disable the network transform to allow smooth interaction with high latency and wait for ownership or timeout to re-enable.
            if (CanHold() & !IsOwner)
            {
                m_ClientNetworkTransform.enabled = false;
            }
        }

        /// <inheritdoc/>
        public override void OnSelectExitedLocal(BaseInteractionEventArgs args)
        {
            base.OnSelectExitedLocal(args);
            // Return out early if the interactor is ignoring sockets or not syncing select.
            if (m_IgnoreSocketSelectedCallback && args.interactorObject.transform.GetComponent<XRSocketInteractor>() != null) return;

            // Check if still holding with other hand.
            if (m_BaseInteractable.isSelected) return;

            if (!IsOwner)
            {
                // Enable Network Transform if releasing the object before ownership has been gained.
                m_ClientNetworkTransform.enabled = true;
            }

            if (IsOwner)
            {
                // Check for interactable type and update kinematic state on release.
                if (baseInteractable.GetType() == typeof(XRGrabInteractable))
                {
                    if (((XRGrabInteractable)baseInteractable).movementType == XRBaseInteractable.MovementType.VelocityTracking || ((UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable)baseInteractable).throwOnDetach)
                    {
                        m_Rigidbody.isKinematic = false;
                    }
                }
            }
        }

        /// <summary>
        /// Override for enabling the NetworkTransform once ownership has been gained.
        /// </summary>
        public override void OnGainedOwnership()
        {
            base.OnGainedOwnership();
            if (IsOwner)
            {
                m_RequestingOwnership = false;
                m_ClientNetworkTransform.enabled = true;
                m_IsInteracting.Value = baseInteractable.isSelected;
                if (!baseInteractable.isSelected & !m_Rigidbody.isKinematic)
                {
                    m_Rigidbody.linearVelocity = m_AverageVelocity;
                }
            }
        }

        /// <summary>
        /// If Ownership was lost due to another player taking over, make sure we cancel all ownership requests and re-enable the ClientNetworkTransform.
        /// </summary>
        public override void OnLostOwnership()
        {
            base.OnLostOwnership();
            if (checkOwnershipRoutine != null) StopCoroutine(checkOwnershipRoutine);
            m_RequestingOwnership = false;
            m_ClientNetworkTransform.enabled = true;
        }

        /// <inheritdoc/>
        void OnCollisionEnter(Collision collision)
        {
            if (!IsOwner || !m_AllowCollisionOwnershipExchange) return;
            NetworkPhysicsInteractable networkPhysicsInteractable = collision.transform.GetComponentInParent<NetworkPhysicsInteractable>();
            if (networkPhysicsInteractable != null && (isInteracting || IsMovingFaster(networkPhysicsInteractable.m_Rigidbody)))
            {
                networkPhysicsInteractable.RequestOwnership();
            }
        }


        /// <summary>
        /// Checks if the current object is moving faster than the other object based on velocity magnitude.
        /// </summary>
        /// <param name="otherBody">The Other Rigidbody you are colliding with</param>
        /// <returns>Returns true if this object is moving faster than the <see cref="m_MinExchangeVelocityMagitude"/> and the object we are hitting.</returns>
        protected bool IsMovingFaster(Rigidbody otherBody)
        {
            return m_Rigidbody.linearVelocity.magnitude > m_MinExchangeVelocityMagitude && m_Rigidbody.linearVelocity.magnitude > otherBody.linearVelocity.magnitude;
        }

        /// <summary>
        /// Checks if ownership transfer is blocked by current conditions.
        /// </summary>
        /// <returns></returns>
        public bool OwnershipTransferBlocked()
        {
            return isInteracting || IsOwner || m_RequestingOwnership || m_LockedOnSpawn.Value || !m_AllowCollisionOwnershipExchange || !NetworkObject.IsSpawned || baseInteractable.isSelected || m_ResettingObject.Value;
        }

        /// <summary>
        /// This function will request ownership of this object.
        /// This will disable the <see cref="m_ClientNetworkTransform"/> to allow for smooth interaction.
        /// This will also set the <see cref="m_RequestingOwnership"/> flag to true.
        /// This will also start a Coroutine to check if ownership was not granted based on the current RTT to the server.
        /// </summary>
        public void RequestOwnership()
        {
            if (IsOwner || OwnershipTransferBlocked()) return;

            m_RequestingOwnership = true;
            m_ClientNetworkTransform.enabled = false;
            if (!m_Rigidbody.isKinematic)
            {
                m_Rigidbody.linearVelocity = m_AverageVelocity;
            }
            if (((XRGrabInteractable)baseInteractable).movementType != XRBaseInteractable.MovementType.Kinematic)
            {
                m_Rigidbody.isKinematic = false;
            }

            RelinquishOwnershipAfterTime();
            RequestOwnershipRpc(NetworkManager.Singleton.LocalClientId);
            if (checkOwnershipRoutine != null) StopCoroutine(checkOwnershipRoutine);
            checkOwnershipRoutine = CheckOwnershipRoutine();
            StartCoroutine(checkOwnershipRoutine);
        }

        [Rpc(SendTo.Server)]
        void RequestOwnershipRpc(ulong clientId)
        {
            NetworkObject.ChangeOwnership(clientId);
        }

        /// <summary>
        /// Coroutine to check if ownership was granted based on the current RTT to the server.
        /// </summary>
        /// <returns></returns>
        IEnumerator CheckOwnershipRoutine()
        {
            // Get the current RTT to the server and wait for twice that time before checking if ownership was granted.
            float waitTime = NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(NetworkManager.ServerClientId) * 2;

            if (waitTime < .025f || waitTime > 5.0f)
            {
                waitTime = 1.0f;
            }

            yield return new WaitForSeconds(waitTime);
            if (!IsOwner)
            {
                Utils.Log($"Ownership Request Timed Out on Object {gameObject.name}");
            }
            m_RequestingOwnership = false;
            m_ClientNetworkTransform.enabled = true;
        }
    }
}
