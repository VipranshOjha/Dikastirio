using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System;
using UnityEngine.XR.Interaction.Toolkit.Samples.SpatialKeyboard;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XRMultiplayer
{
    /// <summary>
    /// Represents a message board that allows players to submit and display messages in a networked environment.
    /// </summary>
    public class NetworkMessageBoard : NetworkBehaviour
    {
        /// <summary>
        /// The prefab for the message text.
        /// </summary>
        [SerializeField] GameObject m_MessagePrefab;

        /// <summary>
        /// The transform that contains the viewport for the messages.
        /// </summary>
        [SerializeField] Transform m_ContentViewport;

        /// <summary>
        /// The maximum number of messages that can be displayed.
        /// </summary>
        [SerializeField] int m_MaxMessageCount = 100;

        /// <summary>
        /// The maximum number of characters that can be displayed in a message.
        /// </summary>
        [SerializeField] int m_MaxCharacterCount = 256;

        /// <summary>
        /// The list of current messages.
        /// </summary>
        NetworkList<FixedString512Bytes> messageList;

        /// <inheritdoc/>
        void Start()
        {
            XRINetworkGameManager.Connected.Subscribe(ConnectedToNetwork);
            messageList = new NetworkList<FixedString512Bytes>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        }

        /// <summary>
        /// Called when the network connection status changes.
        /// </summary>
        /// <param name="connected">Indicates whether the player is connected to the network.</param>
        void ConnectedToNetwork(bool connected)
        {
            if (!connected)
            {
                foreach (Transform t in m_ContentViewport)
                {
                    Destroy(t.gameObject);
                }
            }
        }

        // Called from XRIKeyboardDisplay
        public void ToggleKeyboardOpen(bool toggle)
        {
            GlobalNonNativeKeyboard.instance.keyboard.closeOnSubmit = !toggle;
        }

        /// <inheritdoc/>
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                messageList.Clear();
            }
            foreach (FixedString512Bytes message in messageList)
            {
                CreateText(message.ToString());
            }
        }

        /// <summary>
        /// Submits a text message locally.
        /// </summary>
        /// <param name="text">The text message to submit.</param>
        public void SubmitTextLocal(string text)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrWhiteSpace(text)) return;
            string textToSend = $"<b>{XRINetworkPlayer.LocalPlayer.playerName}</b>:<br><br>{text}";

            if (textToSend.Length > m_MaxCharacterCount)
            {
                textToSend = textToSend.Substring(0, m_MaxCharacterCount);
            }

            FixedString512Bytes newText = new FixedString512Bytes(textToSend);
            SubmitMessageServerRpc(newText);
        }

        /// <summary>
        /// Submits a message to the server.
        /// </summary>
        /// <param name="text">The message to submit.</param>
        [ServerRpc(RequireOwnership = false)]
        void SubmitMessageServerRpc(FixedString512Bytes text)
        {
            messageList.Add(text);
            if (messageList.Count > m_MaxMessageCount)
            {
                messageList.RemoveAt(0);
            }
            SubmitMessageClientRpc(text);
        }

        /// <summary>
        /// Submits a message to the clients.
        /// </summary>
        /// <param name="text">The message to submit.</param>
        [ClientRpc]
        void SubmitMessageClientRpc(FixedString512Bytes text)
        {
            CreateText(text.ToString());
        }

        /// <summary>
        /// Creates a text message and adds it to the message board.
        /// </summary>
        /// <param name="text">The text of the message.</param>
        void CreateText(string text)
        {
            Instantiate(m_MessagePrefab, m_ContentViewport).GetComponent<MessageText>().SetMessage(text, DateTime.Now.ToString("h:mm tt"));
            // message.SetMessage(text, DateTime.Now.ToString("h:mm tt"));

            if (m_ContentViewport.childCount > m_MaxMessageCount)
            {
                Destroy(m_ContentViewport.GetChild(0).gameObject);
            }
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(NetworkMessageBoard), true), CanEditMultipleObjects]
    public class NetworkMessageBoardEditor : Editor
    {

        [SerializeField, TextArea(10, 15)] string m_DebugText;
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            GUILayout.Space(10);
            GUILayout.Label("Debug Area", EditorStyles.boldLabel);
            GUI.enabled = XRINetworkGameManager.Connected.Value;
            if(!XRINetworkGameManager.Connected.Value)
            {
                GUILayout.Label("Connect to a network to submit messages.", EditorStyles.helpBox);
            }
            else
            {
                GUILayout.Label("Debug Text");
                m_DebugText = GUILayout.TextArea(m_DebugText);
            }
            if (GUILayout.Button("Submit Text Debug"))
            {
                ((NetworkMessageBoard)target).SubmitTextLocal(m_DebugText);
            }
            GUI.enabled = true;
        }
    }
#endif
}
