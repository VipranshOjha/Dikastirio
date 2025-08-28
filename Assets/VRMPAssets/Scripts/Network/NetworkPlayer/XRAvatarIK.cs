using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// This class is used to represent the Avatar IK system in both the <see cref="XRINetworkPlayer"/> and the Offline Player"/>.
    /// </summary>
    public class XRAvatarIK : MonoBehaviour
    {
        /// <summary>
        /// Transform for the Network Player Head.
        /// </summary>
        [SerializeField, Tooltip("Transform for the Network Player Head.")] Transform m_HeadTransform;

        /// <summary>
        /// Torso Parent Transform.
        /// </summary>
        [SerializeField, Tooltip("Torso Parent Transform.")] Transform m_TorsoParentTransform;

        /// <summary>
        /// Root of the Head Visuals.
        /// </summary>
        [SerializeField, Tooltip("Root of the Head Visuals.")] Transform m_HeadVisualsRoot;

        /// <summary>
        /// Neck Transform.
        /// </summary>
        [SerializeField, Tooltip("Neck Transform.")] Transform m_Neck;

        /// <summary>
        /// Offset to be applied to the head height.
        /// </summary>
        [SerializeField, Tooltip("Offset to be applied to the head height.")] float m_HeadHeightOffset = .3f;

        /// <summary>
        /// Theshold to where body rotation appoximation is applied.
        /// </summary>
        [Range(0, 360.0f)]
        [SerializeField, Tooltip("Theshold to where body rotation appoximation is applied.")] float m_RotateThreshold = 25.0f;

        /// <summary>
        /// Speed at which the body rotates.
        /// </summary>
        [SerializeField, Tooltip("Speed at which the body rotates.")] float m_RotateSpeed = 3.0f;

        /// <summary>
        /// Transform associated with this script.
        /// </summary>
        Transform m_Transform;

        /// <summary>
        /// Rotation destination for the Y euler value.
        /// </summary>
        float m_DestinationY;

        /// <inheritdoc/>
        private void Start()
        {
            m_Transform = GetComponent<Transform>();
            m_DestinationY = m_HeadTransform.transform.eulerAngles.y;
        }

        /// <inheritdoc/>
        private void Update()
        {
            // Update Head.
            m_HeadVisualsRoot.position = m_HeadTransform.position;
            m_HeadVisualsRoot.position -= m_HeadTransform.up * m_HeadHeightOffset;
            m_Neck.rotation = m_HeadTransform.rotation;

            // Update Body.
            m_Transform.position = m_HeadTransform.position;
            m_TorsoParentTransform.rotation = Quaternion.Slerp(m_TorsoParentTransform.rotation, Quaternion.Euler(new Vector3(0, m_DestinationY, 0)), Time.deltaTime * m_RotateSpeed);

            // Rotate Body if past threshold.
            if (Mathf.Abs(m_TorsoParentTransform.eulerAngles.y - m_HeadTransform.eulerAngles.y) >= m_RotateThreshold)
            {
                m_DestinationY = m_HeadTransform.transform.eulerAngles.y;
            }

            // Update scale.
            m_HeadVisualsRoot.localScale = m_HeadTransform.localScale;
        }
    }
}
