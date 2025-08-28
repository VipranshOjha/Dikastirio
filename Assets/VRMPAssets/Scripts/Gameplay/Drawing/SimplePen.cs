using System.Collections.Generic;
using UnityEngine;
using XRMultiplayer;

public class SimplePen : MonoBehaviour
{
    [SerializeField] PenTrail m_TrailRendererPrefab;
    [SerializeField] Transform m_PenTipTransform;
    [SerializeField] Renderer m_PenTipRenderer;

    NetworkPhysicsInteractable m_NetworkInteractable;

    PenTrail m_CurrentTrailRenderer;

    Color m_CurrentColor;

    List<PenTrail> m_PenTrails = new();

    void Awake()
    {
        TryGetComponent(out m_NetworkInteractable);
    }

    void Start()
    {
        SetColor();
        XRINetworkGameManager.Connected.Subscribe(ConnectedToNetworkGame);
    }

    void OnDestroy()
    {
        XRINetworkGameManager.Connected.Unsubscribe(ConnectedToNetworkGame);
    }

    void ConnectedToNetworkGame(bool connected)
    {
        if (!connected)
        {
            foreach (var trail in m_PenTrails)
            {
                Destroy(trail.gameObject);
            }
        }
    }

    public void ToggleDrawing(bool toggle)
    {
        if (toggle && m_CurrentTrailRenderer == null)
        {
            m_CurrentTrailRenderer = Instantiate(m_TrailRendererPrefab, m_PenTipTransform.position, m_PenTipTransform.rotation, m_PenTipTransform);
            m_CurrentTrailRenderer.SetColor(m_CurrentColor);
        }
        else if (!toggle && m_CurrentTrailRenderer != null)
        {
            m_CurrentTrailRenderer.transform.SetParent(null);
            m_CurrentTrailRenderer.CreateInteractableTrail();
            m_CurrentTrailRenderer = null;
        }
    }

    public void SetColor()
    {
        if (XRINetworkGameManager.Instance.GetPlayerByID(m_NetworkInteractable.OwnerClientId, out XRINetworkPlayer player))
        {
            m_CurrentColor = player.playerColor;
        }
        // Failed to get player, might be offline
        else
        {
            m_CurrentColor = XRINetworkGameManager.LocalPlayerColor.Value;
        }
        m_PenTipRenderer.material.color = m_CurrentColor;
    }

}
