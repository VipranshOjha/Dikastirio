using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace XRMultiplayer
{
    /// <summary>
    /// NetworkSocketInteractor class is responsible for synchronizing the
    /// <see cref="XRSocketInteractor"/> functionality over the network.
    /// </summary>
    [RequireComponent(typeof(XRSocketInteractor))]
    public class NetworkSocketInteractor : NetworkBehaviour
    {
        /// <summary>
        /// Socket Interactor to use.
        /// </summary>
        XRSocketInteractor m_SocketInteractor;

        /// <summary>
        /// Coroutine used to disable the <see cref="XRSocketInteractor"/> component on Hover Exit across the network.
        /// </summary>
        Coroutine m_DisableRoutine;

        private void Awake()
        {
            // Get Socket Component.
            if (!TryGetComponent(out m_SocketInteractor))
            {
                Utils.Log("Missing Components. Disabling Now.", 2);
                this.enabled = false;
                return;
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (m_DisableRoutine != null) StopCoroutine(m_DisableRoutine);
            m_DisableRoutine = StartCoroutine(DisableForTime());
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            XRGrabInteractable grabInteractable = m_SocketInteractor.GetOldestInteractableSelected() as XRGrabInteractable;
            m_SocketInteractor.enabled = false;
            if (grabInteractable != null)
            {
                NetworkPhysicsInteractable networkInteractable = grabInteractable.GetComponent<NetworkPhysicsInteractable>();
                if (networkInteractable != null)
                {
                    networkInteractable.ResetObject();
                    networkInteractable.ResetObjectPhysics();
                }
            }
        }

        /// <summary>
        /// Coroutine to disable <see cref="XRSocketInteractor"/> for 1 second.
        /// </summary>
        /// <returns></returns>
        IEnumerator DisableForTime()
        {
            m_SocketInteractor.enabled = false;
            yield return new WaitForSeconds(.5f);
            m_SocketInteractor.enabled = true;
        }
    }
}
