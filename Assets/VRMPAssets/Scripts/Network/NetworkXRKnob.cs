using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Content.Interaction;

namespace XRMultiplayer
{
    /// <summary>
    /// Represents a networked XR knob interactable.
    /// </summary>
    [RequireComponent(typeof(XRKnob))]
    public class NetworkXRKnob : NetworkBehaviour
    {
        /// <summary>
        /// The networked knob value.
        /// </summary>
        NetworkVariable<float> m_NetworkedKnobValue = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        /// <summary>
        /// The XR knob component.
        /// </summary>
        XRKnob m_XRKnob;

        /// <inheritdoc/>
        public void Awake()
        {
            // Get associated components
            if (!TryGetComponent(out m_XRKnob))
            {
                Utils.Log("Missing Components! Disabling Now.", 2);
                enabled = false;
                return;
            }
        }

        /// <inheritdoc/>
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            m_XRKnob.onValueChange.AddListener(KnobChanged);

            if (IsServer)
            {
                m_NetworkedKnobValue.Value = m_XRKnob.value;
            }
            else
            {
                m_XRKnob.value = m_NetworkedKnobValue.Value;
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            m_XRKnob.onValueChange.RemoveListener(KnobChanged);
        }

        /// <summary>
        /// Called when the knob value is changed.
        /// </summary>
        /// <param name="newValue">The new value of the knob.</param>
        private void KnobChanged(float newValue)
        {
            KnobChangedServerRpc(newValue, NetworkManager.Singleton.LocalClientId);
        }

        /// <summary>
        /// Server RPC called when the knob value is changed.
        /// </summary>
        /// <param name="newValue">The new value of the knob.</param>
        /// <param name="clientId">The client ID of the player who changed the knob value.</param>
        [ServerRpc(RequireOwnership = false)]
        void KnobChangedServerRpc(float newValue, ulong clientId)
        {
            m_NetworkedKnobValue.Value = newValue;
            KnobChangedClientRpc(newValue, clientId);
        }

        /// <summary>
        /// Client RPC called when the knob value is changed.
        /// </summary>
        /// <param name="newValue">The new value of the knob.</param>
        /// <param name="clientId">The client ID of the player who changed the knob value.</param>
        [ClientRpc]
        void KnobChangedClientRpc(float newValue, ulong clientId)
        {
            if (clientId != NetworkManager.Singleton.LocalClientId)
            {
                m_XRKnob.onValueChange.RemoveListener(KnobChanged);
                m_XRKnob.value = newValue;
                m_XRKnob.onValueChange.AddListener(KnobChanged);
            }
        }
    }
}
