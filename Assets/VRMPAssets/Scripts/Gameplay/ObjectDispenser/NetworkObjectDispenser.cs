using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Unity.Netcode;
using System;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace XRMultiplayer
{
    /// <summary>
    /// Represents a networked object dispenser that can spawn and despawn interactable objects.
    /// </summary>
    public class NetworkObjectDispenser : NetworkBehaviour
    {
        const int k_NonToggleablePanelId = -1;
        public Action OnProxiesUpdated;

        /// <summary>
        /// The button used to clear the current interactables.
        /// </summary>
        [SerializeField] UnityEngine.UI.Button m_ClearButton;

        // [SerializeField] bool m_UseCapacity = false;
        /// <summary>
        /// The maximum capacity of the dispenser.
        /// </summary>
        [SerializeField] int m_Capacity;

        [SerializeField] float m_DistanceCheckTimeInterval = .5f;
        /// <summary>
        /// The text component displaying the current capacity.
        /// </summary>
        [SerializeField] TMP_Text m_CountText;

        /// <summary>
        /// The network variable representing the current capacity.
        /// </summary>
        NetworkVariable<int> m_CurrentCapacityNetworked = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        /// <summary>
        /// The network variable representing the current panel ID.
        /// </summary>
        NetworkVariable<int> m_CurrentPanelIdNetworked = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        /// <summary>
        /// The list of currently active interactables.
        /// </summary>
        List<NetworkBaseInteractable> m_ActiveInteractables = new List<NetworkBaseInteractable>();

        // /// <summary>
        // /// The panels containing the dispenser slots.
        // /// </summary>
        [SerializeField] DispenserPanel[] m_Panels;

        // /// <summary>
        // /// This panel is persistent and does not switch on or off.
        // /// /// </summary>
        [SerializeField] DispenserPanel m_PersistentPanel;

        [SerializeField] Transform m_DefaultSpawnTransform;

        ///<inheritdoc/>
        private void Start()
        {
            CheckForProxies();
            m_CurrentCapacityNetworked.OnValueChanged += UpdateCapacity;
        }

        ///<inheritdoc/>
        public override void OnDestroy()
        {
            base.OnDestroy();
            m_CurrentCapacityNetworked.OnValueChanged -= UpdateCapacity;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            CheckForProxies(false);

            PrefabStage.prefabStageOpened -= (PrefabStage stage) => CheckForProxies();
            PrefabStage.prefabStageOpened += (PrefabStage stage) => CheckForProxies();

            for(int i = 0; i < m_Panels.Length; i++)
            {
                m_Panels[i].panelId = i;
                for(int j = 0; j < m_Panels[i].dispenserSlots.Length; j++)
                {
                    if(m_Panels[i].dispenserSlots[j] == null) continue;
                    m_Panels[i].dispenserSlots[j].panelId = i;
                    m_Panels[i].dispenserSlots[j].slotId = j;
                }
            }

            m_PersistentPanel.panelId = k_NonToggleablePanelId;
            for(int j = 0; j < m_PersistentPanel.dispenserSlots.Length; j++)
            {
                if(m_PersistentPanel.dispenserSlots[j] == null) continue;
                m_PersistentPanel.dispenserSlots[j].panelId = k_NonToggleablePanelId;
                m_PersistentPanel.dispenserSlots[j].slotId = j;
            }
        }
#endif

        IEnumerator ServerSpawnCooldownRoutine()
        {
            float deltaTime;
            while (IsServer)
            {
                deltaTime = Time.deltaTime;
                foreach (DispenserPanel panel in m_Panels)
                {
                    foreach (DispenserSlot slot in panel.dispenserSlots)
                    {
                        if (!slot.dispenserSlotTransform.gameObject.activeInHierarchy) continue;
                        if (slot.CanSpawn(deltaTime))
                        {
                            AddInteractableToDispenser(panel, slot.slotId);
                        }
                    }
                }

                foreach (DispenserSlot slot in m_PersistentPanel.dispenserSlots)
                {
                    if (!slot.dispenserSlotTransform.gameObject.activeInHierarchy) continue;
                    if (slot.CanSpawn(deltaTime))
                    {
                        AddInteractableToDispenser(m_PersistentPanel, slot.slotId);
                    }
                }

                yield return new WaitForEndOfFrame();
            }
        }
        IEnumerator ServerDistanceCheckRoutine()
        {
            while (IsServer)
            {
                foreach (DispenserPanel panel in m_Panels)
                {
                    foreach (DispenserSlot slot in panel.dispenserSlots)
                    {
                        if (slot.CheckInteractablePosition())
                        {
                            m_ActiveInteractables.Add(slot.currentInteractable);
                            m_CurrentCapacityNetworked.Value = m_ActiveInteractables.Count;
                            slot.currentInteractable = null;
                        }
                    }
                }

                foreach (DispenserSlot slot in m_PersistentPanel.dispenserSlots)
                {
                    if (slot.CheckInteractablePosition())
                    {
                        m_ActiveInteractables.Add(slot.currentInteractable);
                        m_CurrentCapacityNetworked.Value = m_ActiveInteractables.Count;
                        slot.currentInteractable = null;
                    }
                }

                yield return new WaitForSeconds(m_DistanceCheckTimeInterval);
            }
        }

        ///<inheritdoc/>
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            m_ActiveInteractables.Clear();
            if (IsServer)
            {
                EnablePanel(0);
                StartCoroutine(ServerDistanceCheckRoutine());
                StartCoroutine(ServerSpawnCooldownRoutine());
                m_CurrentCapacityNetworked.Value = 0;
            }
            else if (m_CurrentPanelIdNetworked.Value != -1)
            {
                EnablePanel(m_CurrentPanelIdNetworked.Value);
                if (m_CountText != null)
                    m_CountText.text = $"Current Capacity: {m_CurrentCapacityNetworked.Value} / {m_Capacity}";
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            StopAllCoroutines();
        }

        /// <summary>
        /// Updates the capacity UI based on the current capacity value.
        /// </summary>
        /// <param name="old">The old capacity value.</param>
        /// <param name="current">The current capacity value.</param>
        void UpdateCapacity(int old, int current)
        {
            if (m_CountText != null)
            {
                m_CountText.text = $"Current Capacity: {m_CurrentCapacityNetworked.Value} / {m_Capacity}";
                m_CountText.color = m_CurrentCapacityNetworked.Value > m_Capacity ? Color.red : Color.white;
            }

            if (m_ClearButton != null)
                m_ClearButton.interactable = m_CurrentCapacityNetworked.Value > 0;
        }

        /// <summary>
        /// Clears the current interactables on the server.
        /// </summary>
        public void ClearCurrentInteractables()
        {
            if (IsServer)
            {
                for (int i = m_ActiveInteractables.Count - 1; i >= 0; i--)
                {
                    if (m_ActiveInteractables[i] != null & !m_ActiveInteractables[i].isInteracting)
                    {
                        m_ActiveInteractables[i].NetworkObject.Despawn();
                        m_ActiveInteractables.Remove(m_ActiveInteractables[i]);
                    }
                }
                m_CurrentCapacityNetworked.Value = m_ActiveInteractables.Count;
            }
        }

        /// <summary>
        /// Picks up an object from a dispenser panel slot.
        /// </summary>
        /// <param name="panel">The dispenser panel.</param>
        /// <param name="slotId">The slot ID.</param>
        void PickupObject(UnityAction<bool> bindingAction, int panelId, int slotId)
        {
            var panel = GetPanelById(panelId);
            if (!IsServer) { Utils.Log("Trying to Spawn Object from Non-Server Client"); return; }
            NetworkBaseInteractable netInteractable = panel.dispenserSlots[slotId].currentInteractable;
            if (netInteractable == null) return;

            netInteractable.OnInteractingChanged.RemoveListener(bindingAction);

            if (netInteractable.TryGetComponent(out Rigidbody rb))
            {
                rb.constraints = RigidbodyConstraints.None;
            }
        }

        /// <summary>
        /// Adds an interactable to a dispenser panel slot on the server.
        /// </summary>
        /// <param name="panel">The dispenser panel.</param>
        /// <param name="slotId">The slot ID.</param>
        /// <param name="prefabId">The prefab ID, if default (-1) it will spawn based on the index of the prefab list.</param>
        void AddInteractableToDispenser(DispenserPanel panel, int slotId)
        {
            if (panel.dispenserSlots[slotId].currentInteractable != null)
            {
                Utils.Log($"Cannot Spawn. Interactable already exists in Panel {panel.panelId}, slot {panel.dispenserSlots[slotId].slotId}");
                return;
            }

            Transform spawnerTransform = panel.dispenserSlots[slotId].dispenserSlotTransform;
            panel.dispenserSlots[slotId].m_SpawnCooldownTimer = panel.dispenserSlots[slotId].spawnCooldown;
            NetworkBaseInteractable spawnedInteractable = panel.dispenserSlots[slotId].SpawnInteractablePrefab(spawnerTransform);

            panel.dispenserSlots[slotId].currentInteractable = spawnedInteractable;
            panel.dispenserSlots[slotId].currentInteractable.NetworkObject.Spawn();

            // Creates a UnityAction<bool> that calls PickupObject with the correct parameters
            void PickupBinding(bool arg0)
            {
                PickupObject(PickupBinding, panel.panelId, slotId);
            }

            spawnedInteractable.OnInteractingChanged.AddListener(PickupBinding);

            if (panel.dispenserSlots[slotId].freezeOnSpawn && spawnedInteractable.TryGetComponent(out Rigidbody rb))
            {
                rb.constraints = RigidbodyConstraints.FreezeAll;
            }
        }

        DispenserPanel GetPanelById(int Id)
        {
            if (Id == k_NonToggleablePanelId) return m_PersistentPanel;
            return m_Panels[Id];
        }

        /// <summary>
        /// Clears the current panel on the server.
        /// </summary>
        void ClearPanel()
        {
            StopAllCoroutines();
            foreach (var slot in m_Panels[m_CurrentPanelIdNetworked.Value].dispenserSlots)
            {
                if (slot.currentInteractable != null)
                {
                    slot.currentInteractable.NetworkObject.Despawn();
                    slot.currentInteractable = null;
                }
            }
            StartCoroutine(ServerDistanceCheckRoutine());
            StartCoroutine(ServerSpawnCooldownRoutine());
        }

        /// <summary>
        /// Enables a specific panel and adds interactables to its slots on the server.
        /// </summary>
        /// <param name="panelId">The panel ID.</param>
        public void EnablePanel(int panelId)
        {
            for (int i = 0; i < m_Panels.Length; i++)
            {
                m_Panels[i].panel.SetActive(i == panelId);
            }
            if (IsServer)
            {
                if (m_CurrentPanelIdNetworked.Value != -1)
                {
                    ClearPanel();
                }

                m_CurrentPanelIdNetworked.Value = panelId;
            }
        }

        [ContextMenu("Spawn Random Object")]
        void SpawnRandomObject()
        {
            SpawnRandomRpc(m_DefaultSpawnTransform.position, m_DefaultSpawnTransform.rotation);
        }

        [Rpc(SendTo.Server)]
        public void SpawnRandomRpc(Vector3 spawnPosition, Quaternion spawnRotation)
        {
            var spawnObject = m_PersistentPanel.dispenserSlots[UnityEngine.Random.Range(0, m_PersistentPanel.dispenserSlots.Length)].spawnableInteractablePrefab;
            if (UnityEngine.Random.value < .85f)
            {
                int randomPanel = UnityEngine.Random.Range(0, m_Panels.Length);
                int randomSlot = UnityEngine.Random.Range(0, m_Panels[randomPanel].dispenserSlots.Length);
                spawnObject = m_Panels[randomPanel].dispenserSlots[randomSlot].spawnableInteractablePrefab;
            }

            NetworkPhysicsInteractable spawnedObject = Instantiate(spawnObject.gameObject, spawnPosition, spawnRotation).GetComponent<NetworkPhysicsInteractable>();
            spawnedObject.spawnLocked = false;
            spawnedObject.NetworkObject.Spawn();

            m_ActiveInteractables.Add(spawnedObject.GetComponent<NetworkBaseInteractable>());
            m_CurrentCapacityNetworked.Value = m_ActiveInteractables.Count;
        }

        [ContextMenu("Clear Proxies")]
        public void ClearProxies()
        {
            foreach (var panel in m_Panels)
            {
                foreach (var slot in panel.dispenserSlots)
                {
                    slot.ClearProxy();
                }
            }
            foreach (var slot in m_PersistentPanel.dispenserSlots)
            {
                slot.ClearProxy();
            }
        }

        public void ClearProxyPanel(int panelId)
        {
            (panelId == k_NonToggleablePanelId ? m_PersistentPanel : m_Panels[panelId]).ClearProxyPanel();
            OnProxiesUpdated?.Invoke();
        }

        public void ClearProxy(int panelId, int slotId)
        {
            (panelId == k_NonToggleablePanelId ? m_PersistentPanel : m_Panels[panelId]).dispenserSlots[slotId].ClearProxy();
            OnProxiesUpdated?.Invoke();
        }

        public void SpawnProxyPanel(int panelId)
        {
            if (panelId != k_NonToggleablePanelId)
            {
                for (int i = 0; i < m_Panels.Length; i++)
                {
                    if (panelId != i)
                    {
                        m_Panels[i].ClearProxyPanel();
                    }
                }
            }
            (panelId == k_NonToggleablePanelId ? m_PersistentPanel : m_Panels[panelId]).SpawnProxyPanel();

            OnProxiesUpdated?.Invoke();
        }

        public void SpawnProxy(int panelId, int slotId)
        {
            (panelId == k_NonToggleablePanelId ? m_PersistentPanel : m_Panels[panelId]).dispenserSlots[slotId].SpawnProxy();

            NetworkBaseInteractable interactable = (panelId == k_NonToggleablePanelId ? m_PersistentPanel : m_Panels[panelId]).dispenserSlots[slotId].currentInteractable;
            OnProxiesUpdated?.Invoke();
        }

        public bool IsProxyPanelShowing(int panelId)
        {
            if (m_Panels.Length == 0) return false;
            DispenserPanel panel = panelId == k_NonToggleablePanelId ? m_PersistentPanel : m_Panels[panelId];
            foreach (var slot in panel.dispenserSlots)
            {
                if (slot.hasSpawnedProxy) return true;
            }
            return false;
        }

        public bool IsProxyPanelFull(int panelId)
        {
            if (m_Panels.Length == 0) return false;
            DispenserPanel panel = panelId == k_NonToggleablePanelId ? m_PersistentPanel : m_Panels[panelId];
            foreach (var slot in panel.dispenserSlots)
            {
                if (!slot.hasSpawnedProxy) return false;
            }
            return true;
        }

        public bool IsProxySlotShowing(int panelId, int slotId)
        {
            if (m_Panels.Length == 0) return false;
            return (panelId == k_NonToggleablePanelId ? m_PersistentPanel : m_Panels[panelId]).dispenserSlots[slotId].hasSpawnedProxy;
        }

        public void CheckForProxies(bool clearAllProxies = true)
        {
            foreach (var panel in m_Panels)
            {
                foreach (var slot in panel.dispenserSlots)
                {
                    if (slot.currentInteractable == null && slot.dispenserSlotTransform != null)
                    {
                        if (slot.dispenserSlotTransform.parent.GetComponentInChildren<NetworkBaseInteractable>() != null)
                        {
                            slot.currentInteractable = slot.dispenserSlotTransform.parent.GetComponentInChildren<NetworkBaseInteractable>();
                        }
                    }
                }
            }
            foreach (var slot in m_PersistentPanel.dispenserSlots)
            {
                if (slot.currentInteractable == null && slot.dispenserSlotTransform != null)
                {
                    if (slot.dispenserSlotTransform.parent.GetComponentInChildren<NetworkBaseInteractable>() != null)
                    {
                        slot.currentInteractable = slot.dispenserSlotTransform.parent.GetComponentInChildren<NetworkBaseInteractable>();
                    }
                }
            }

            if (clearAllProxies)
                ClearProxies();

            OnProxiesUpdated?.Invoke();
        }
    }

    [Serializable]
    /// <summary>
    /// Represents a dispenser panel.
    /// </summary>
    public class DispenserPanel
    {
        /// <summary>
        /// The type of physics used by this panel.
        /// </summary>
        public string panelName;

        /// <summary>
        /// The panel game object associated with the object dispenser.
        /// </summary>
        public GameObject panel;

        /// <summary>
        /// The array of dispenser slots used by the object dispenser.
        /// </summary>
        [SerializeField] public DispenserSlot[] dispenserSlots;

        public int panelId;

        public void SpawnProxyPanel()
        {
            for (int i = 0; i < dispenserSlots.Length; i++)
            {
                dispenserSlots[i].SpawnProxy();
            }
        }

        public void ClearProxyPanel()
        {
            for (int i = 0; i < dispenserSlots.Length; i++)
            {
                dispenserSlots[i].ClearProxy();
            }
        }
    }

    [Serializable]
    /// <summary>
    /// Represents a dispenser slot.
    /// </summary>
    public class DispenserSlot
    {
        /// <summary>
        /// The transform of the dispenser slot.
        /// </summary>
        public Transform dispenserSlotTransform;

        public NetworkBaseInteractable spawnableInteractablePrefab;

        public NetworkObjectSpawner objectSpawner;
        public int panelId;
        public int slotId;

        public bool freezeOnSpawn = true;
        public float distanceToSpawnNew = .5f;
        public float spawnCooldown = .5f;

        internal float m_SpawnCooldownTimer = 0f;


        [SerializeField] public bool hasSpawnedProxy = false;
        /// <summary>
        /// The current network interactable object in the dispenser slot.
        /// </summary>
        [SerializeField] public NetworkBaseInteractable currentInteractable;
        public void ClearProxy()
        {
            if (currentInteractable == null) return;
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(currentInteractable.gameObject);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(currentInteractable.gameObject);
            }
            hasSpawnedProxy = false;
        }

        public void SpawnProxy()
        {
            if (currentInteractable != null) ClearProxy();
            NetworkBaseInteractable spawnedInteractable = UnityEngine.Object.Instantiate(spawnableInteractablePrefab, dispenserSlotTransform.position, dispenserSlotTransform.rotation);
            spawnedInteractable.transform.localScale = dispenserSlotTransform.localScale;
            spawnedInteractable.transform.parent = dispenserSlotTransform.transform;
            currentInteractable = spawnedInteractable;
            hasSpawnedProxy = true;
        }

        public bool CheckInteractablePosition()
        {
            if (currentInteractable == null)
                return false;

            float currentDistance = Vector3.Distance(currentInteractable.transform.position, dispenserSlotTransform.position);
            if (objectSpawner != null && currentDistance > 0.001f)
                objectSpawner.OnSpawnDistanceUpdated.Invoke(Mathf.Clamp01(currentDistance / distanceToSpawnNew));

            return currentDistance > distanceToSpawnNew;
        }

        public bool CanSpawn(float deltaTime)
        {
            if (currentInteractable != null) return false;
            if (m_SpawnCooldownTimer > 0)
            {
                UpdateCooldown(m_SpawnCooldownTimer - deltaTime);
                return false;
            }

            UpdateCooldown(spawnCooldown);
            return true;
        }

        void UpdateCooldown(float newTime)
        {
            m_SpawnCooldownTimer = newTime;
            if (objectSpawner != null)
                objectSpawner.OnSpawnCooldownUpdated.Invoke(Mathf.Clamp01(1 - (m_SpawnCooldownTimer / spawnCooldown)));
        }

        public NetworkBaseInteractable SpawnInteractablePrefab(Transform spawnerTransform)
        {
            UpdateCooldown(spawnCooldown);
            NetworkBaseInteractable spawnedInteractable = UnityEngine.Object.Instantiate
            (
                spawnableInteractablePrefab,
                spawnerTransform.position,
                spawnerTransform.rotation
            );
            spawnedInteractable.transform.localScale = spawnerTransform.localScale;
            if (objectSpawner != null)
                objectSpawner.OnObjectSpawned.Invoke();

            return spawnedInteractable;
        }
    }
}
