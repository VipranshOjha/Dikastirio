using System.Collections;
using UnityEngine;

namespace XRMultiplayer.MiniGames
{
    /// <summary>
    /// Updates the visual elements of the slingshot in the gameplay.
    /// </summary>
    [ExecuteInEditMode]
    public class SlingshotVisualUpdater : MonoBehaviour
    {
        /// <summary>
        /// The line renderer displaying the current trajectory.
        /// </summary>
        public LineRenderer trajectoryLineRenderer;

        [SerializeField] Rigidbody m_Rigidbody;

        /// <summary>
        /// The transform of the bucket.
        /// </summary>
        [SerializeField] Rigidbody m_BucketRigibody;

        /// <summary>
        /// The transform of the shot center.
        /// </summary>
        [SerializeField] Transform m_ShotCenterTransform;

        /// <summary>
        /// The line renderers of the slingshot.
        /// </summary>
        [SerializeField] LineRenderer[] m_LineRenderers;

        /// <summary>
        /// The transform of the line end.
        /// </summary>
        [SerializeField] Transform[] m_LineEndTransforms;

        /// <summary>
        /// The minimum and maximum distance of the slingshot.
        /// </summary>
        [SerializeField] Vector2 m_MinMaxDistance = new Vector2(0.0f, 1.0f);

        /// <summary>
        /// The minimum and maximum size of the line.
        /// </summary>
        [SerializeField] Vector2 m_MinMaxLineSize = new Vector2(.05f, .01f);

        /// <summary>
        /// The minimum and maximum shot force.
        /// </summary>
        [SerializeField] Vector2 m_MinMaxShotForce = new Vector2(5.0f, 20.0f);

        /// <summary>
        /// The force multiplier of the shot.
        /// </summary>
        [SerializeField] float m_ForceMultiplier = 2.0f;

        /// <summary>
        /// The maximum distance to display the debug line.
        /// </summary>
        [SerializeField] float m_MaxDebugShowDistance = 2.0f;

        /// <summary>
        /// The time to reset the bucket position.
        /// </summary>
        [SerializeField] float m_BucketResetTime = .35f;

        /// <summary>
        /// The color divisor of the debug line.
        /// </summary>
        [SerializeField] float m_ColorDivisor = 5.0f;

        /// <summary>
        /// The line display distance.
        /// </summary>
        [SerializeField] float m_LineDisplayDistance = 2.0f;

        [SerializeField] bool m_LockBucketRotation = true;

        /// <summary>
        /// The shot force of the slingshot.
        /// </summary>
        float m_ShotForce;

        /// <summary>
        /// The aim direction of the slingshot.
        /// </summary>
        Vector3 m_AimDirection;

        /// <summary>
        /// The reset position of the bucket.
        /// </summary>
        // Vector3 m_ResetPosition;

        Pose m_ResetPose;

        /// <inheritdoc/>
        void Awake()
        {
            trajectoryLineRenderer.enabled = false;
            m_ResetPose = new Pose(m_BucketRigibody.transform.localPosition, m_BucketRigibody.transform.localRotation);
            // m_ResetPosition = m_BucketTransform.localPosition;
        }

        /// <inheritdoc/>
        void LateUpdate()
        {
            for (int i = 0; i < m_LineRenderers.Length; i++)
            {
                m_LineRenderers[i].SetPosition(0, m_LineRenderers[i].transform.position);
                m_LineRenderers[i].SetPosition(1, m_LineEndTransforms[i].position);

                float distance = Vector3.Distance(m_LineRenderers[i].transform.position, m_LineEndTransforms[i].position);
                float perc = Utils.GetPercentOfValueBetweenTwoValues(m_MinMaxDistance.x, m_MinMaxDistance.y, distance);
                float size = Mathf.Lerp(m_MinMaxLineSize.x, m_MinMaxLineSize.y, perc);
                m_LineRenderers[i].startWidth = size;
                m_LineRenderers[i].endWidth = size;
            }

            float lineDistance = Vector3.Distance(m_ShotCenterTransform.position, m_BucketRigibody.position);
            m_ShotForce = Mathf.Clamp(lineDistance * m_ForceMultiplier, m_MinMaxShotForce.x, m_MinMaxShotForce.y);
            m_AimDirection = (m_ShotCenterTransform.position - m_BucketRigibody.position).normalized;

            Vector3 lineEndPos = m_BucketRigibody.position + (m_AimDirection * Mathf.Clamp(lineDistance * m_LineDisplayDistance, 0.0f, m_MaxDebugShowDistance));

            Color forceColor = Color.Lerp(Color.yellow, Color.red, Mathf.Clamp01(m_ShotForce / m_MaxDebugShowDistance / m_ColorDivisor));
            Color transparentForceColor = new Color(forceColor.r, forceColor.g, forceColor.b, 0.0f);

            trajectoryLineRenderer.startColor = forceColor;
            trajectoryLineRenderer.endColor = transparentForceColor;

            trajectoryLineRenderer.SetPosition(0, m_BucketRigibody.position);
            trajectoryLineRenderer.SetPosition(1, lineEndPos);
            if (m_LockBucketRotation)
                m_BucketRigibody.transform.forward = m_AimDirection;
        }

        /// <summary>
        /// Gets the force of the shot.
        /// </summary>
        /// <returns>The force of the shot.</returns>
        public Vector3 GetShotForce()
        {
            return m_AimDirection * m_ShotForce;
        }

        public void ResetSlingshot(float height)
        {
            StartCoroutine(ResetSlingshotHeightRoutine(height));
            ResetBucketPosition();
        }

        IEnumerator ResetSlingshotHeightRoutine(float height)
        {
            bool wasKinematic = m_Rigidbody.isKinematic;
            m_Rigidbody.isKinematic = true;
            transform.localPosition = new Vector3(transform.localPosition.x, height, transform.localPosition.z);

            yield return new WaitForSeconds(.15f);
            m_Rigidbody.isKinematic = wasKinematic;
        }

        /// <summary>
        /// Resets the position of the bucket.
        /// </summary>
        public void ResetBucketPosition()
        {
            StartCoroutine(ResetBucketPositionRoutine());
        }

        /// <summary>
        /// Coroutine to reset the position of the bucket over time.
        /// </summary>
        /// <returns>An IEnumerator used for the coroutine.</returns>
        IEnumerator ResetBucketPositionRoutine()
        {
            bool wasKinematic = m_BucketRigibody.isKinematic;
            m_BucketRigibody.isKinematic = true;
            for (float i = 0; i < m_BucketResetTime; i += Time.deltaTime)
            {
                m_BucketRigibody.transform.localPosition = Vector3.Lerp(m_BucketRigibody.transform.localPosition, m_ResetPose.position, i / m_BucketResetTime);
                yield return null;
            }

            m_BucketRigibody.transform.SetLocalPositionAndRotation(m_ResetPose.position, m_ResetPose.rotation);
            m_BucketRigibody.isKinematic = wasKinematic;
        }
    }
}
