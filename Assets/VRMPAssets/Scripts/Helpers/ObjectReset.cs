using System.Collections.Generic;
using Unity.Netcode.Components;
using XRMultiplayer;

namespace UnityEngine.XR.Content.Interaction
{
    /// <summary>
    /// Provides the ability to reset objects
    /// </summary>
    public class ObjectReset : MonoBehaviour
    {
        [SerializeField] Transform m_ResetTransform;
        List<NetworkPhysicsInteractable> m_Interactables = new List<NetworkPhysicsInteractable>();

        void OnTriggerEnter(Collider collider)
        {
            NetworkPhysicsInteractable networkBaseInteractable = collider.GetComponentInParent<NetworkPhysicsInteractable>();
            if (networkBaseInteractable != null && !networkBaseInteractable.isInteracting & !m_Interactables.Contains(networkBaseInteractable) && networkBaseInteractable.IsOwner)
            {
                m_Interactables.Add(networkBaseInteractable);
                ResetTransform(networkBaseInteractable);
            }
        }

        void ResetTransform(NetworkPhysicsInteractable networkBaseInteractable)
        {
            Transform currentTransform = networkBaseInteractable.transform;
            networkBaseInteractable.GetComponent<NetworkTransform>().Teleport(m_ResetTransform.position, m_ResetTransform.rotation, networkBaseInteractable.transform.localScale);

            var rigidBody = currentTransform.GetComponentInChildren<Rigidbody>();
            if (rigidBody != null)
            {
                networkBaseInteractable.ResetObjectPhysics();
            }

            if (m_Interactables.Contains(networkBaseInteractable))
                m_Interactables.Remove(networkBaseInteractable);
        }
    }
}
