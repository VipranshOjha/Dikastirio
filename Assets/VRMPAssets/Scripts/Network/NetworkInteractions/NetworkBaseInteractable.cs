using UnityEngine;
using Unity.Netcode;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.AffordanceSystem.State;
using System;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XRMultiplayer
{
    /// <summary>
    /// NetworkInteractableBase class synchronizes the <see cref=XRBaseInteractable"/> events over the network.
    /// Options are exposed to determine which functionality you want to syncrhonize.
    /// </summary>
    /// <remarks>
    /// This is meant to be a parent class that handles the core networking functionality.
    /// Classes can interhit from this class and override where applicable.
    /// See <see cref=NetworkPhysicsInteractable"/> for an example of how to extend this class.
    /// </remarks>
    [RequireComponent(typeof(XRBaseInteractable))]
    [DisallowMultipleComponent]
    public class NetworkBaseInteractable : NetworkBehaviour, IXRSelectFilter, IXRHoverFilter
    {
        /// <summary>
        /// Allow users to take ownership of currently controlled objects.
        /// </summary>
        public bool allowOverrideOwnership
        {
            get => m_AllowOverrideOwnership;
            set => m_AllowOverrideOwnership = value;
        }

        [Header("General Options"), SerializeField, Tooltip("Allow users to take ownership of currently controlled objects")]
        protected bool m_AllowOverrideOwnership = false;

        /// <summary>
        /// Amount of time before checking for false positives for the object interaction state
        /// </summary>
        public float interactionCheckTime
        {
            get => m_InteractionCheckTime;
            set => m_InteractionCheckTime = value;
        }
        [SerializeField, Tooltip("Amount of time before checking for false positives of the object interaction state.")]
        protected float m_InteractionCheckTime = 2.0f;

        /// <summary>
        /// Ignore Socket Interaction
        /// </summary>
        public bool ignoreSocketSelectedCallback
        {
            get => m_IgnoreSocketSelectedCallback;
            set => m_IgnoreSocketSelectedCallback = value;
        }
        [SerializeField, Tooltip("Ignore Socket Interaction")]
        protected bool m_IgnoreSocketSelectedCallback = true;

        /// <summary>
        /// Resets the object position, scale, and rotation on disconnect.
        /// </summary>
        public bool resetObjectOnDisconenct
        {
            get => m_ResetObjectOnDisconnect;
            set => m_ResetObjectOnDisconnect = value;
        }
        [SerializeField, Tooltip("Reset object on disconnect")]
        protected bool m_ResetObjectOnDisconnect = true;

        /// <summary>
        /// Amount of time before relinquishing ownership of the object back to the host.
        /// </summary>
        public bool relinquishOwnershipAfterTime
        {
            get => m_RelinquishOwnershipAfterTime;
            set => m_RelinquishOwnershipAfterTime = value;
        }
        [Header("Ownership Relinquish"), SerializeField, Tooltip("Should we relinquish ownership back to the room host after a set amount of time?")]
        protected bool m_RelinquishOwnershipAfterTime = true;

        /// <summary>
        /// Amount of time before relinquishing ownership of the object back to the host.
        /// </summary>
        public float relinquishOwnershipTime
        {
            get => m_RelinquishOwnershipTime;
            set => m_RelinquishOwnershipTime = value;
        }
        [SerializeField, Tooltip("Amount of time before relinquishing ownership of the object back to the host.")]
        protected float m_RelinquishOwnershipTime = 5.0f;

        /// <summary>
        /// Gets the current state of an object being interacted over the network.
        /// </summary>
        public bool isInteracting
        {
            get => m_IsInteracting.Value;
        }
        /// <summary>
        /// Syncs the current state of being interacted with or not.
        /// Prevents users from taking control of currently controlled objects, unless <see cref="allowOverrideOwnership"/> is true.
        /// </summary>
        /// <remarks>
        /// <see cref="allowOverrideOwnership"/> will allow users to bypass this value and take ownership from other players.
        /// </remarks>
        protected NetworkVariable<bool> m_IsInteracting = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        [HideInInspector, SerializeField, Tooltip("Use a Unity Event for a callback when the IsInteracting value changes.")]

        //Disabling warning for unused variable since it's used in the editor script
#pragma warning disable 0414
        bool m_UseInteractingChangedEvent = false;
#pragma warning restore 0414
        [HideInInspector] public UnityEvent<bool> OnInteractingChanged;

        /// <summary>
        /// Sync Hover interaction over network.
        /// </summary>
        public bool syncHover
        {
            get => m_SyncHover;
            set => m_SyncHover = value;
        }
        [HideInInspector, SerializeField, Tooltip("Sync Hover interaction over network")]
        protected bool m_SyncHover = false;
        [HideInInspector, SerializeField, Tooltip("Use Unity Events for Networked Hooks")]
        protected bool m_UseHoverEvents = false;
        [HideInInspector] public UnityEvent<bool> HoverNetworkedEventServer;
        [HideInInspector] public UnityEvent<bool> HoverNetworkedEventAll;

        /// <summary>
        /// Sync Select interaction over network.
        /// </summary>
        public bool syncSelect
        {
            get => m_SyncSelect;
            set => m_SyncSelect = value;
        }
        [HideInInspector, SerializeField, Tooltip("Sync Select interaction over network")]
        protected bool m_SyncSelect = true;
        [HideInInspector, SerializeField, Tooltip("Use Unity Events for Networked Hooks")]
        protected bool m_UseSelectEvents = false;
        [HideInInspector] public UnityEvent<bool> SelectNetworkedEventServer;
        [HideInInspector] public UnityEvent<bool> SelectNetworkedEventAll;

        /// <summary>
        /// Sync Activate interaction over network.
        /// </summary>
        public bool syncActivate
        {
            get => m_SyncActivate;
            set => m_SyncActivate = value;
        }
        [HideInInspector, SerializeField, Tooltip("Sync Activate interaction over network")]
        protected bool m_SyncActivate = true;
        [HideInInspector, SerializeField, Tooltip("Use Unity Events for Networked Hooks")]
        protected bool m_UseActivateEvents = false;
        [HideInInspector] public UnityEvent<bool> ActivateNetworkedEventServer;
        [HideInInspector] public UnityEvent<bool> ActivateNetworkedEventAll;

        /// <summary>
        /// Base Interactable used for syncing events.
        /// </summary>
        public XRBaseInteractable baseInteractable
        {
            get => m_BaseInteractable;
            set => m_BaseInteractable = value;
        }
        protected XRBaseInteractable m_BaseInteractable;

        public bool canProcess => isActiveAndEnabled;

        /// <summary>
        /// Starting Pose for the object transform.
        /// </summary>
        protected Pose m_OriginalPose;

        /// <summary>
        /// Starting scale for the object transform.
        /// </summary>
        protected Vector3 m_OriginalScale;

        protected XRInteractionManager m_InteractionManager;

#pragma warning disable CS0618 // Type or member is obsolete
        protected BaseAffordanceStateProvider m_AffordanceStateProvider;
#pragma warning restore CS0618 // Type or member is obsolete

#if UNITY_EDITOR
        /// <summary>
        /// Foldout states for the editor.
        /// </summary>
        [HideInInspector, SerializeField]
        bool[] m_FoldoutValues = {true, true, true};
#endif

        /// <summary>
        /// After a set amount of time, relinquish ownership of the object back to the host.
        /// </summary>
        IEnumerator m_RelinquishToHostEnumerator;

        /// <summary>
        /// Check for false positives of the object being interacted with.
        /// </summary>
        IEnumerator m_HostInteractionCheckEnumerator;

        /// <inheritdoc/>
        public virtual void Awake()
        {
            // Get associated components
            if (!TryGetComponent(out m_BaseInteractable))
            {
                Utils.Log("Missing Components! Disabling Now.", 2);
                enabled = false;
                return;
            }

            m_BaseInteractable.selectFilters.Add(this);
            m_BaseInteractable.hoverFilters.Add(this);

            m_InteractionManager = FindFirstObjectByType<XRInteractionManager>();

#pragma warning disable CS0618 // Type or member is obsolete
            m_AffordanceStateProvider = GetComponentInChildren<BaseAffordanceStateProvider>();
#pragma warning restore CS0618 // Type or member is obsolete
        }

        /// <inheritdoc/>
        private void OnEnable()
        {
            // Set initial pose and scale
            m_OriginalPose.position = transform.position;
            m_OriginalPose.rotation = transform.rotation;
            m_OriginalScale = transform.localScale;

            SetupListeners(true);
        }

        /// <inheritdoc/>
        private void OnDisable()
        {
            SetupListeners(false);
        }

        /// <summary>
        /// Handles the listeners for the interactable events.
        /// </summary>
        /// <param name="setup">
        /// Whether or not we are adding or removing the listeners.
        /// </param>
        void SetupListeners(bool setup)
        {
            if (setup)
            {
                if (baseInteractable != null)
                {
                    // Add all the listeners for interactable events
                    baseInteractable.hoverEntered.AddListener(OnHoverEnterLocal);
                    baseInteractable.hoverExited.AddListener(OnHoverExitLocal);
                    baseInteractable.selectEntered.AddListener(OnSelectEnteredLocal);
                    baseInteractable.selectExited.AddListener(OnSelectExitedLocal);
                    baseInteractable.activated.AddListener(OnActivateLocal);
                    baseInteractable.deactivated.AddListener(OnDeactivateLocal);
                }
            }
            else
            {
                if (baseInteractable != null)
                {
                    // Removes listeners from the interactable events
                    baseInteractable.hoverEntered.RemoveListener(OnHoverEnterLocal);
                    baseInteractable.hoverExited.RemoveListener(OnHoverExitLocal);
                    baseInteractable.selectEntered.RemoveListener(OnSelectEnteredLocal);
                    baseInteractable.selectExited.RemoveListener(OnSelectExitedLocal);
                    baseInteractable.activated.RemoveListener(OnActivateLocal);
                    baseInteractable.deactivated.RemoveListener(OnDeactivateLocal);
                }
            }
        }

        /// <inheritdoc/>
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            NetworkObject.DontDestroyWithOwner = true;
            m_IsInteracting.OnValueChanged += OnIsInteractingChanged;

            if (IsOwner)
                m_IsInteracting.Value = false;

            if (m_ResetObjectOnDisconnect)
            {
                ResetObject();
            }
        }

        /// <inheritdoc/>
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            m_IsInteracting.OnValueChanged -= OnIsInteractingChanged;

            if (IsOwner)
                m_IsInteracting.Value = false;

            if (m_ResetObjectOnDisconnect)
            {
                ResetObject();
            }
        }

        /// <summary>
        /// Resets the object position, scale, and rotation based on the original pose determined in <see cref="OnEnable"/>
        /// </summary>
        public virtual void ResetObject()
        {
            transform.SetPositionAndRotation(m_OriginalPose.position, m_OriginalPose.rotation);
            transform.localScale = m_OriginalScale;
        }

        /// <summary>
        /// Callback for the Hover Enter event executed for the local user.
        /// </summary>
        /// <param name="args"></param>
        public virtual void OnHoverEnterLocal(BaseInteractionEventArgs args)
        {
            Hovered(true);
            if (syncHover)
            {
                OnHoverServerRpc(true, NetworkManager.Singleton.LocalClientId);
            }
        }

        /// <summary>
        /// Callback for the Hover Exit event executed for the local user.
        /// </summary>
        /// <param name="args"></param>
        public virtual void OnHoverExitLocal(BaseInteractionEventArgs args)
        {
            Hovered(false);
            if (syncHover)
            {
                OnHoverServerRpc(false, NetworkManager.Singleton.LocalClientId);
            }
        }

        /// <summary>
        /// Hover event executed on the Server.
        /// </summary>
        /// <param name="entered">True if hover entered, False if hover exited.</param>
        /// <param name="clientId">ClientId who sent the RPC.</param>
        [ServerRpc(RequireOwnership = false)]
        public virtual void OnHoverServerRpc(bool entered, ulong clientId)
        {
            OnHoverClientRpc(entered, clientId);
            if (m_UseHoverEvents)
                HoverNetworkedEventServer.Invoke(entered);
        }

        /// <summary>
        /// Hover event executed on all clients.
        /// </summary>
        /// <param name="entered">True if hover entered, False if hover exited.</param>
        /// <param name="clientId">ClientId who sent the RPC.</param>
        [ClientRpc]
        public virtual void OnHoverClientRpc(bool entered, ulong clientId)
        {
            if (clientId != NetworkManager.Singleton.LocalClientId)
            {
                Hovered(entered);

                if (m_AffordanceStateProvider != null)
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    m_AffordanceStateProvider.UpdateAffordanceState(new AffordanceStateData(Convert.ToByte(entered ? 2 : (isInteracting ? 4 : 0)), 1.0f));
#pragma warning restore CS0618 // Type or member is obsolete
                }
            }
        }

        /// <summary>
        /// This function gets called immediately for the local user,
        /// and gets called remotely from the server on all clients.
        /// </summary>
        /// <param name="entered">True if hover entered, False if hover exited.</param>
        public virtual void Hovered(bool entered)
        {
            if (m_UseHoverEvents)
                HoverNetworkedEventAll.Invoke(entered);
        }

        /// <summary>
        /// Callback for the Select Enter event executed for the local user.
        /// </summary>
        /// <param name="args"></param>
        public virtual void OnSelectEnteredLocal(BaseInteractionEventArgs args)
        {
            // Return out early if the interactor is ignoring sockets or not syncing select.
            if (m_IgnoreSocketSelectedCallback && args.interactorObject.transform.GetComponent<XRSocketInteractor>() != null)
                return;

            if (CanHold())
            {
                Selected(true);
                if (syncSelect)
                {
                    OnSelectServerRpc(true, NetworkManager.Singleton.LocalClientId);
                }

                // If already the owner, set the network variable for isHeld
                if (IsOwner)
                {
                    m_IsInteracting.Value = true;
                }
            }
        }

        /// <summary>
        /// Callback for the Select Exit event executed for the local user.
        /// </summary>
        /// <param name="args"></param>
        public virtual void OnSelectExitedLocal(BaseInteractionEventArgs args)
        {
            // Return out early if the interactor is ignoring sockets or not syncing select.
            if (m_IgnoreSocketSelectedCallback && args.interactorObject.transform.GetComponent<XRSocketInteractor>() != null)
                return;

            // Check if still holding with other hand.
            if (m_BaseInteractable.isSelected)
                return;

            // Check that it is still a spawned object. Select will fire on object destruction.
            if (!IsSpawned)
                return;

            Selected(false);

            if (syncSelect)
            {
                OnSelectServerRpc(false, NetworkManager.Singleton.LocalClientId);
            }

            // If still the owner, set the network variable for isHeld
            if (IsOwner)
            {
                m_IsInteracting.Value = false;
                RelinquishOwnershipAfterTime();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        void ResetObjectToHostServerRpc()
        {
            if (NetworkObject.OwnerClientId != NetworkManager.Singleton.LocalClientId)
                NetworkObject.ChangeOwnership(NetworkManager.Singleton.LocalClientId);
        }

        /// <summary>
        /// Select event executed on the Server.
        /// </summary>
        /// <param name="selected">True if select entered, False if select exited.</param>
        /// <param name="clientId">ClientId who sent the RPC.</param>
        [ServerRpc(RequireOwnership = false)]
        public virtual void OnSelectServerRpc(bool selected, ulong clientId)
        {
            OnSelectClientRpc(selected, clientId);

            // If we are not the owner and we are selecting the object, request to change ownership
            if (selected && OwnerClientId != clientId)
            {
                NetworkObject.ChangeOwnership(clientId);
            }

            SelectNetworkedEventServer.Invoke(selected);
        }

        /// <summary>
        /// Select event executed on all clients.
        /// </summary>
        /// <param name="selected">True if select entered, False if select exited.</param>
        /// <param name="clientId">ClientId who sent the RPC.</param>
        [ClientRpc]
        public virtual void OnSelectClientRpc(bool selected, ulong clientId)
        {
            if (clientId != NetworkManager.Singleton.LocalClientId)
            {
                Selected(selected);
            }
        }

        /// <summary>
        /// This function gets called immediately for the local user,
        /// and gets called remotely from the server on all clients.
        /// </summary>
        /// <param name="selected">Whether or not selected was called.</param>
        public virtual void Selected(bool selected) { }

        /// <summary>
        /// Callback for the Activate event executed for the local user.
        /// </summary>
        /// <param name="args"></param>
        public virtual void OnActivateLocal(BaseInteractionEventArgs args)
        {
            if (!IsOwner) return;

            Activated(true);
            if (syncActivate)
            {
                OnActivateServerRpc(true, NetworkManager.Singleton.LocalClientId);
            }
        }

        /// <summary>
        /// Callback for the Deactivate event executed for the local user.
        /// </summary>
        /// <param name="args"></param>
        public virtual void OnDeactivateLocal(BaseInteractionEventArgs args)
        {
            if (!IsOwner) return;

            Activated(false);
            if (syncActivate)
            {
                OnActivateServerRpc(false, NetworkManager.Singleton.LocalClientId);
            }
        }

        /// <summary>
        /// Activate event executed on the Server.
        /// </summary>
        /// <param name="activate">True if activated, False if Deactivated.</param>
        /// <param name="clientId">ClientId who sent the RPC.</param>
        [ServerRpc(RequireOwnership = false)]
        public virtual void OnActivateServerRpc(bool activate, ulong clientId)
        {
            OnActivateClientRpc(activate, clientId);
            if (m_UseActivateEvents)
                ActivateNetworkedEventServer.Invoke(activate);
        }

        /// <summary>
        /// Activate event executed on all clients.
        /// </summary>
        /// <param name="activate">True if activated, False if Deactivated.</param>
        /// <param name="clientId">ClientId who sent the RPC.</param>
        [ClientRpc]
        public virtual void OnActivateClientRpc(bool activate, ulong clientId)
        {
            if (clientId != NetworkManager.Singleton.LocalClientId)
            {
                Activated(activate);
                if (m_AffordanceStateProvider != null)
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    m_AffordanceStateProvider.UpdateAffordanceState(new AffordanceStateData(Convert.ToByte(activate ? 5 : isInteracting ? 4 : 0), 1.0f));
#pragma warning restore CS0618 // Type or member is obsolete
                }
            }
        }

        /// <summary>
        /// This function gets called immediately for the local user,
        /// and gets called remotely from the server on all clients.
        /// </summary>
        /// <param name="activate">True if activated, False if Deactivated.</param>
        public virtual void Activated(bool activate)
        {
            if (m_UseActivateEvents)
                ActivateNetworkedEventAll.Invoke(activate);
        }

        /// <summary>
        /// Checks if another user can hold or pickup an interactable.
        /// </summary>
        /// <returns></returns>
        protected virtual bool CanHold()
        {
            return !isInteracting || allowOverrideOwnership;
        }

        /// <summary>
        /// See <see cref="NetworkBehaviour"/>.
        /// </summary>
        public override void OnGainedOwnership()
        {
            base.OnGainedOwnership();

            // Check for gaining ownership of an object when a player disconnects
            if (IsOwner && IsServer && isInteracting & !baseInteractable.isSelected)
            {
                m_IsInteracting.Value = false;
            }

            // Workaround for NGO calling this always on Server, even if Server is not owner.
            // So we check IsOwner and Interactable selected state, and loop through to check for sockets.
            if (IsOwner)
            {
                if (m_HostInteractionCheckEnumerator != null)
                    StopCoroutine(m_HostInteractionCheckEnumerator);

                m_HostInteractionCheckEnumerator = CheckForOwnerInteraction();
                StartCoroutine(m_HostInteractionCheckEnumerator);

                if (m_RelinquishToHostEnumerator != null) StopCoroutine(m_RelinquishToHostEnumerator);

                if (baseInteractable.isSelected & !isInteracting)
                {
                    if (!IsSelectedBySocket())
                    {
                        m_IsInteracting.Value = true;
                    }
                }
            }
        }

        /// <summary>
        /// See <see cref="NetworkBehaviour"/>.
        /// </summary>
        public override void OnLostOwnership()
        {
            base.OnLostOwnership();

            // Have to check ownership since this is always called on the Server
            if (!IsOwner)
            {
                if (m_HostInteractionCheckEnumerator != null)
                    StopCoroutine(m_HostInteractionCheckEnumerator);

                if (m_RelinquishToHostEnumerator != null)
                    StopCoroutine(m_RelinquishToHostEnumerator);

                if (baseInteractable.isSelected)
                    m_InteractionManager.CancelInteractableSelection((IXRSelectInteractable)baseInteractable);

            }
        }

        /// <summary>
        /// Checks every <see cref="interactionCheckTime"/> for false positives of the object being interacted with.
        /// </summary>
        IEnumerator CheckForOwnerInteraction()
        {
            while (IsOwner)
            {
                if (isInteracting)
                {
                    if (!baseInteractable.isSelected || IsSelectedBySocket())
                    {
                        Utils.Log($"Interacting is true and either selected is false or selected is a socket on object {gameObject.name}. Is this intentional?");
                        m_IsInteracting.Value = false;
                    }
                }
                yield return new WaitForSeconds(interactionCheckTime);
            }
        }

        /// <summary>
        /// Checks if the object is selected by a socket.
        /// </summary>
        bool IsSelectedBySocket()
        {
            if (baseInteractable.isSelected)
            {
                foreach (var interactor in baseInteractable.interactorsSelecting)
                {
                    if (interactor is XRSocketInteractor)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Callback for anytime the <see cref="m_IsInteracting"/> value changes.
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        protected virtual void OnIsInteractingChanged(bool oldValue, bool newValue)
        {
            OnInteractingChanged.Invoke(newValue);

            if (m_AffordanceStateProvider != null)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                m_AffordanceStateProvider.UpdateAffordanceState(new AffordanceStateData(Convert.ToByte(newValue ? 4 : 0), 1.0f));
#pragma warning restore CS0618 // Type or member is obsolete
            }

            SelectNetworkedEventAll.Invoke(newValue);

            // If we are interacting and the owner, stop the coroutine to relinquish ownership
            if (newValue && IsOwner && m_RelinquishToHostEnumerator != null)
            {
                StopCoroutine(m_RelinquishToHostEnumerator);
            }
        }

        /// <summary>
        /// Relinquish ownership of the object back to the host after a set amount of time.
        /// </summary>
        protected void RelinquishOwnershipAfterTime()
        {
            if (!m_RelinquishOwnershipAfterTime) return;

            if (m_RelinquishToHostEnumerator != null) StopCoroutine(m_RelinquishToHostEnumerator);
            m_RelinquishToHostEnumerator = RelinquishOwnershipToHost();
            StartCoroutine(m_RelinquishToHostEnumerator);
        }

        /// <summary>
        /// Coroutine to relinquish ownership of the object back to the host after a set amount of time.
        /// </summary>
        IEnumerator RelinquishOwnershipToHost()
        {
            yield return new WaitForSeconds(relinquishOwnershipTime);
            if (!IsServer && !baseInteractable.isSelected)
            {
                ResetObjectToHostServerRpc();
            }
        }

        /// <summary>
        /// Process the select filter.
        /// </summary>
        /// <param name="interactor">Interactor being used to process the Select.</param>
        /// <param name="interactable"></param>
        /// <returns></returns>
        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            return IsOwner || allowOverrideOwnership || (!IsOwner & !isInteracting);
        }

        /// <summary>
        /// Process the hover filter.
        /// </summary>
        /// <param name="interactor">Interactor being used to process the Hover.</param>
        /// <param name="interactable"></param>
        /// <returns></returns>
        public bool Process(IXRHoverInteractor interactor, IXRHoverInteractable interactable)
        {
            return IsOwner || allowOverrideOwnership || (!IsOwner & !isInteracting);
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Custom Editor for the <see cref="NetworkBaseInteractable"/> class.
    /// </summary>
    [CustomEditor(typeof(NetworkBaseInteractable), true), CanEditMultipleObjects]
    public class NetworkBaseInteractableEditor : Editor
    {
        // Serialized properties
        SerializedProperty m_UseInteractingChangedEvent;
        SerializedProperty m_InteractingChangedEvent;
        SerializedProperty m_SyncHover;
        SerializedProperty m_UseHoverEvents;
        SerializedProperty m_SyncHoverEventServer;
        SerializedProperty m_SyncHoverEventAll;
        SerializedProperty m_SyncSelect;
        SerializedProperty m_UseSelectEvents;
        SerializedProperty m_SyncSelectEventServer;
        SerializedProperty m_SyncSelectEventAll;
        SerializedProperty m_SyncActivate;
        SerializedProperty m_UseActivateEvents;
        SerializedProperty m_SyncActivateEventServer;
        SerializedProperty m_SyncActivateEventAll;
        SerializedProperty m_FoldoutStates;

        /// <summary>
        /// Called when the editor is enabled.
        /// Initializes the serialized properties.
        /// </summary>
        void OnEnable()
        {
            m_UseInteractingChangedEvent = serializedObject.FindProperty("m_UseInteractingChangedEvent");
            m_InteractingChangedEvent = serializedObject.FindProperty("OnInteractingChanged");
            m_SyncHover = serializedObject.FindProperty("m_SyncHover");
            m_UseHoverEvents = serializedObject.FindProperty("m_UseHoverEvents");
            m_SyncHoverEventServer = serializedObject.FindProperty("HoverNetworkedEventServer");
            m_SyncHoverEventAll = serializedObject.FindProperty("HoverNetworkedEventAll");
            m_SyncSelect = serializedObject.FindProperty("m_SyncSelect");
            m_UseSelectEvents = serializedObject.FindProperty("m_UseSelectEvents");
            m_SyncSelectEventServer = serializedObject.FindProperty("SelectNetworkedEventServer");
            m_SyncSelectEventAll = serializedObject.FindProperty("SelectNetworkedEventAll");
            m_SyncActivate = serializedObject.FindProperty("m_SyncActivate");
            m_UseActivateEvents = serializedObject.FindProperty("m_UseActivateEvents");
            m_SyncActivateEventServer = serializedObject.FindProperty("ActivateNetworkedEventServer");
            m_SyncActivateEventAll = serializedObject.FindProperty("ActivateNetworkedEventAll");
            m_FoldoutStates = serializedObject.FindProperty("m_FoldoutValues");
        }

        /// <summary>
        /// Called to draw the custom inspector GUI.
        /// </summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Unity Events", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_UseInteractingChangedEvent);
            if (m_UseInteractingChangedEvent.boolValue)
            {
                EditorGUILayout.PropertyField(m_InteractingChangedEvent);
            }

            // Hover Logic
            EditorGUILayout.Space(10);
            SerializedProperty option = m_FoldoutStates.GetArrayElementAtIndex(0);
            option.boolValue = EditorGUILayout.Foldout(option.boolValue, "Hover Options", true);
            if (option.boolValue)
            {
                if (m_UseHoverEvents.boolValue)
                {
                    m_SyncHover.boolValue = true;
                    GUI.enabled = false;
                }

                EditorGUILayout.PropertyField(m_SyncHover);
                GUI.enabled = true;
                EditorGUILayout.PropertyField(m_UseHoverEvents);

                if (m_UseHoverEvents.boolValue)
                {
                    EditorGUILayout.PropertyField(m_SyncHoverEventServer);
                    EditorGUILayout.PropertyField(m_SyncHoverEventAll);
                }
            }

            // Select Logic
            EditorGUILayout.Space(10);
            option = m_FoldoutStates.GetArrayElementAtIndex(1);
            option.boolValue = EditorGUILayout.Foldout(option.boolValue, "Select Options", true);
            if (option.boolValue)
            {
                if (m_UseSelectEvents.boolValue)
                {
                    m_SyncSelect.boolValue = true;
                    GUI.enabled = false;
                }

                EditorGUILayout.PropertyField(m_SyncSelect);
                GUI.enabled = true;
                EditorGUILayout.PropertyField(m_UseSelectEvents);

                if (m_UseSelectEvents.boolValue)
                {
                    EditorGUILayout.PropertyField(m_SyncSelectEventServer);
                    EditorGUILayout.PropertyField(m_SyncSelectEventAll);
                }
            }

            // Activate Logic
            EditorGUILayout.Space(10);
            option = m_FoldoutStates.GetArrayElementAtIndex(2);
            option.boolValue = EditorGUILayout.Foldout(option.boolValue, "Activate Options", true);
            if (option.boolValue)
            {
                if (m_UseActivateEvents.boolValue)
                {
                    m_SyncActivate.boolValue = true;
                    GUI.enabled = false;
                }

                EditorGUILayout.PropertyField(m_SyncActivate);
                GUI.enabled = true;
                EditorGUILayout.PropertyField(m_UseActivateEvents);

                if (m_UseActivateEvents.boolValue)
                {
                    EditorGUILayout.PropertyField(m_SyncActivateEventServer);
                    EditorGUILayout.PropertyField(m_SyncActivateEventAll);
                }
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
