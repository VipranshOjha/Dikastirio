

namespace UnityEngine.XR.Content.Interaction
{
    /// <summary>
    /// Socket interactor that only selects and hovers interactables with a keychain component containing specific keys.
    /// </summary>
    public class XRLockSocketInteractor : UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor
    {
        [Space]
        [SerializeField]
        [Tooltip("The required keys to interact with this socket.")]
        Lock m_Lock;

        /// <summary>
        /// The required keys to interact with this socket.
        /// </summary>
        public Lock keychainLock
        {
            get => m_Lock;
            set => m_Lock = value;
        }

        /// <inheritdoc />
        public override bool CanHover(UnityEngine.XR.Interaction.Toolkit.Interactables.IXRHoverInteractable interactable)
        {
            if (!base.CanHover(interactable))
                return false;

            var keyChain = interactable.transform.GetComponent<IKeychain>();
            return m_Lock.CanUnlock(keyChain);
        }

        /// <inheritdoc />
        public override bool CanSelect(UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable interactable)
        {
            if (!base.CanSelect(interactable))
                return false;

            var keyChain = interactable.transform.GetComponent<IKeychain>();
            return m_Lock.CanUnlock(keyChain);
        }
    }
}
