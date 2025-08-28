using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace XRMultiplayer
{
    [RequireComponent(typeof(TrailRenderer))]
    public class PenTrail : MonoBehaviour
    {
        public TrailRenderer trailRenderer => m_TrailRenderer;
        TrailRenderer m_TrailRenderer;

        [SerializeField] bool m_UseLifetime = false;
        [SerializeField] float m_ObjectLifetimeInSeconds = 900.0f;

        GameObject m_SpawnedInteractableObject;
        Color m_StartColor;
        float m_StartWidth;
        // Start is called before the first frame update
        void Awake()
        {
            if (!TryGetComponent(out m_TrailRenderer))
            {
                Utils.Log("Missing Components! Disabling Now.", 2);
                enabled = false;
                return;
            }
        }

        public void SetColor(Color color)
        {
            m_StartColor = color;
            UpdateColor(m_StartColor);
        }

        void UpdateColor(Color color)
        {
            m_TrailRenderer.material.color = color;
            m_TrailRenderer.startColor = color;
            m_TrailRenderer.endColor = color;
        }

        public void CreateInteractableTrail()
        {
            m_StartWidth = m_TrailRenderer.startWidth;

            Mesh mesh = new();
            m_TrailRenderer.BakeMesh(mesh, true);

            m_SpawnedInteractableObject = new();
            m_SpawnedInteractableObject.transform.position = Vector3.zero;
            m_SpawnedInteractableObject.AddComponent<MeshFilter>().mesh = mesh;
            m_SpawnedInteractableObject.AddComponent<MeshCollider>().sharedMesh = mesh;

            m_SpawnedInteractableObject.transform.parent = transform;

            XRSimpleInteractable interactable = m_SpawnedInteractableObject.AddComponent<XRSimpleInteractable>();

            interactable.activated.AddListener(DestroyTrail);
            interactable.hoverEntered.AddListener(HoverEntered);
            interactable.hoverExited.AddListener(HoverExited);

            if (m_UseLifetime)
                Destroy(gameObject, m_ObjectLifetimeInSeconds);
        }

        void HoverEntered(HoverEnterEventArgs args)
        {
            m_TrailRenderer.startWidth = m_StartWidth * 3.0f;
            m_TrailRenderer.endWidth = m_StartWidth * 3.0f;
            UpdateColor(Color.red);
        }

        void HoverExited(HoverExitEventArgs args)
        {
            m_TrailRenderer.startWidth = m_StartWidth;
            m_TrailRenderer.endWidth = m_StartWidth;
            UpdateColor(m_StartColor);
        }

        void DestroyTrail(ActivateEventArgs args)
        {
            Destroy(gameObject);
        }
    }
}
