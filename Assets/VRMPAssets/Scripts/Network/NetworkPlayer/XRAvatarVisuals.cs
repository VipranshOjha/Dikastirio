using System;
using UnityEngine;

namespace XRMultiplayer
{
    [RequireComponent(typeof(XRINetworkPlayer))]
    public class XRAvatarVisuals : MonoBehaviour
    {
        /// <summary>
        /// Head Renderers to change rendering mode for local players.
        /// </summary>
        [Header("Renderer References"), SerializeField, Tooltip("Head Renderers to change rendering mode for local players.")]
        protected Renderer[] m_HeadRends;

        /// <summary>
        /// Head Renderer to control the blend shape for mouth movement. Also updates shirt color based on <see cref="playerColor"/>.
        /// </summary>
        [SerializeField, Tooltip("Head Renderer to drive mouth movement blendshape and player shirt color.")]
        protected SkinnedMeshRenderer m_headRend;

        /// <summary>
        /// GameObject to enable to show what player is the Room Host.
        /// </summary>
        [Header("Host Visuals"), SerializeField, Tooltip("GameObject that gets enabled for the Host only.")]
        protected GameObject m_HostVisuals;

        /// <summary>
        /// GameObject to enable to show what player is the Room Host.
        /// </summary>
        [SerializeField, Tooltip("Show Host Visuals.")]
        protected bool m_ShowHostVisuals = true;

        /// <summary>
        /// Materials to swap for the local player.
        /// </summary>
        [Header("Local Player Material Swap"), SerializeField]
        protected LocalPlayerMaterialSwap m_LocalPlayerMaterialSwap;

        /// <summary>
        /// Reference to the attached XRINetworkPlayerAvatar component.
        /// </summary>
        protected XRINetworkPlayer m_NetworkPlayerAvatar;

        public virtual void Awake()
        {
            if (!TryGetComponent(out m_NetworkPlayerAvatar))
            {
                Utils.LogError("XRAvatarVisuals requires a XRINetworkPlayerAvatar component to be attached to the same GameObject. Disabling this component now.");
                enabled = false;
                return;
            }

            m_NetworkPlayerAvatar.onSpawnedLocal += PlayerSpawnedLocal;
            m_NetworkPlayerAvatar.onSpawnedAll += PlayerSpawnedAll;
            m_NetworkPlayerAvatar.onColorUpdated += SetPlayerColor;
        }

        public virtual void OnDestroy()
        {
            m_NetworkPlayerAvatar.onSpawnedLocal -= PlayerSpawnedLocal;
            m_NetworkPlayerAvatar.onSpawnedAll -= PlayerSpawnedAll;
            m_NetworkPlayerAvatar.onColorUpdated -= SetPlayerColor;
        }

        public virtual void Update()
        {
            UpdateMouth();
        }

        public virtual void UpdateMouth()
        {
            if (m_headRend != null)
                m_headRend.SetBlendShapeWeight(0, 100 - (m_NetworkPlayerAvatar.playerVoiceAmp * 100));
        }

        public virtual void PlayerSpawnedLocal()
        {
            m_LocalPlayerMaterialSwap.SwapMaterials();
            int layer = LayerMask.NameToLayer("Mirror");
            foreach (var r in m_HeadRends)
            {
                r.gameObject.layer = layer;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
        }

        public virtual void PlayerSpawnedAll()
        {
            m_HostVisuals.SetActive(m_ShowHostVisuals && m_NetworkPlayerAvatar.IsOwnedByServer);
        }

        public virtual void SetPlayerColor(Color newColor)
        {
            m_headRend.materials[2].SetColor("_BaseColor", newColor);
        }
    }
}

[Serializable]

/// <summary>
/// Helper class for swapping the local player to standard materials from the dithering materials.
/// </summary>
public class LocalPlayerMaterialSwap
{
    public Renderer headRend;
    public Renderer hmdRend;
    public Renderer hostRend;
    public Renderer[] hands;
    public Material[] headMaterials;
    public Material[] hmdMaterials;
    public Material hostMaterial;
    public Material handMaterial;


    public void SwapMaterials()
    {
        for (int i = 0; i < hands.Length; i++)
        {
            hands[i].material = handMaterial;
        }

        hmdRend.materials = hmdMaterials;
        headRend.materials = headMaterials;
        hostRend.material = hostMaterial;
    }
}
