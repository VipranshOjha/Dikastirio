using System.Collections.Generic;
using UnityEngine;

using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace XRMultiplayer
{
    /// <summary>
    /// Represents an anti-gravity zone in the game world.
    /// Objects and characters within the zone experience anti-gravity effects.
    /// </summary>
    public class AntiGravityZone : MonoBehaviour
    {
        [Header("Anti-Gravity Zone Settings")]
        /// <summary>
        /// The minimum range of float speeds for objects within the zone.
        /// </summary>
        [SerializeField] Vector2 m_ObjectFloatSpeedRangeMin = new(4.5f, 12.0f);

        /// <summary>
        /// The maximum range of float speeds for objects within the zone.
        /// </summary>
        [SerializeField] Vector2 m_ObjectFloatSpeedRangeMax = new Vector2(13.0f, 15.0f);

        /// <summary>
        /// The minimum range of float speeds for the player within the zone.
        /// </summary>
        [SerializeField] Vector2 m_PlayerFloatSpeedRangeMin = new Vector2(0, 0);

        /// <summary>
        /// The maximum range of float speeds for the player within the zone.
        /// </summary>
        [SerializeField] Vector2 m_PlayerFloatSpeedRangeMax = new Vector2(.025f, .075f);

        /// <summary>
        /// The range of float speeds for the particle system.
        /// </summary>
        [SerializeField] Vector2 m_ParticleFloatSpeedMinMax = new Vector2(0, 3);

        /// <summary>
        /// The maximum velocity for objects within the zone.
        /// </summary>
        [SerializeField] float m_MaxObjectVelocityThreshold = 4.0f;

        /// <summary>
        /// The particle system for the zone.
        /// </summary>
        [SerializeField] ParticleSystem[] m_Particles;

        /// <summary>
        /// The collider for the zone.
        /// </summary>
        [SerializeField] SubTrigger m_AntiGravitySubTrigger;

        /// <summary>
        /// The renderer for the zone.
        /// </summary>
        [SerializeField] Renderer m_Renderer;

        [Header("Powered Settings")]
        /// <summary>
        /// The renderers for the zone that get colored when powered on.
        /// </summary>
        [SerializeField] Renderer[] m_PoweredOnRends;

        /// <summary>
        /// The audio source for the zone.
        /// </summary>
        [SerializeField] AudioSource m_PoweredOnAudio;

        /// <summary>
        /// The material for the zone when powered on.
        /// </summary>
        [SerializeField] Material m_PoweredOnMaterial;

        /// <summary>
        /// The material for the zone when powered off.
        /// </summary>
        [SerializeField] Material m_PoweredOffMaterial;

        [Header("Black Hole Settings")]

        [SerializeField] GameObject[] m_BlackHoleObjects;

        float currentSpeed;

        /// <summary>
        /// The float speed range for objects within the zone.
        /// </summary>
        Vector2 m_FloatSpeedRange;

        /// <summary>
        /// The float speed range for the player within the zone.
        /// </summary>
        Vector2 m_PlayerFloatSpeedRange;

        /// <summary>
        /// The list of rigidbodies within the zone.
        /// </summary>
        CharacterController m_CharacterController;

        /// <summary>
        /// The list of rigidbodies within the zone.
        /// </summary>
        List<Rigidbody> m_RigidbodyList = new List<Rigidbody>();

        bool m_IsBlackHole = false;

        bool m_IsPowered = false;

        /// <inheritdoc/>
        private void Awake()
        {
            PowerOff();
            UpdateSpeed(0);
            m_AntiGravitySubTrigger.OnTriggerAction += SubTriggered;
            ToggleBlackHoles(false);
        }

        /// <inheritdoc/>
        void Update()
        {
            if (m_IsBlackHole) return;

            if (m_CharacterController != null)
            {
                m_CharacterController.transform.position += Vector3.up * Random.Range(m_PlayerFloatSpeedRange.x, m_PlayerFloatSpeedRange.y);
            }
        }

        /// <inheritdoc/>
        void FixedUpdate()
        {
            if (m_IsBlackHole) return;

            foreach (var rigidbody in m_RigidbodyList)
            {
                if (rigidbody == null) continue;
                if (!rigidbody.isKinematic)
                {
                    // If the rigidbody is not kinematic and it's Y velocity is under the threshold, apply a force to simulate floating.
                    if (rigidbody.linearVelocity.y < m_MaxObjectVelocityThreshold)
                    {
                        currentSpeed = Random.Range(m_FloatSpeedRange.x, m_FloatSpeedRange.y);
                        rigidbody.AddForce(Vector3.up * currentSpeed);
                    }
                }
            }
        }

        /// <summary>
        /// Turns off the anti-gravity zone.
        /// </summary>
        ///<remarks> Called from the Socket Interactor.</remarks>
        public void PowerOff()
        {
            m_IsPowered = false;
            m_AntiGravitySubTrigger.subTriggerCollider.enabled = false;
            m_Renderer.enabled = false;
            m_PoweredOnAudio.enabled = false;
            m_RigidbodyList.Clear();

            foreach (var p in m_Particles)
            {
                p.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }

            foreach (Renderer r in m_PoweredOnRends)
            {
                if (r == null) continue;
                r.material = m_PoweredOffMaterial;
            }

            if (m_CharacterController != null)
            {
                m_CharacterController.GetComponentInChildren<DynamicMoveProvider>().useGravity = true;
                m_CharacterController = null;
            }

            ToggleBlackHoles(false);
        }

        /// <summary>
        /// Turns on the anti-gravity zone.
        /// </summary>
        ///<remarks> Called from the Socket Interactor.</remarks>
        public void PowerOn()
        {
            m_IsPowered = true;
            m_PoweredOnAudio.enabled = true;

            foreach (Renderer r in m_PoweredOnRends)
            {
                r.material = m_PoweredOnMaterial;
            }

            if (m_IsBlackHole)
            {
                m_Renderer.enabled = false;
                ToggleBlackHoles(true);
            }
            else
            {
                m_Renderer.enabled = true;
                m_AntiGravitySubTrigger.subTriggerCollider.enabled = true;
                foreach (var p in m_Particles)
                {
                    p.Play();
                }
            }
        }

        /// <summary>
        /// Updates the speed of the anti-gravity effects based on the given value.
        /// </summary>
        /// <param name="value">The value used to interpolate the speed range.</param>
        public void UpdateSpeed(float value)
        {
            m_FloatSpeedRange = new Vector2(Mathf.Lerp(m_ObjectFloatSpeedRangeMin.x, m_ObjectFloatSpeedRangeMax.x, value), Mathf.Lerp(m_ObjectFloatSpeedRangeMin.y, m_ObjectFloatSpeedRangeMax.y, value));
            m_PlayerFloatSpeedRange = new Vector2(Mathf.Lerp(m_PlayerFloatSpeedRangeMin.x, m_PlayerFloatSpeedRangeMax.x, value), Mathf.Lerp(m_PlayerFloatSpeedRangeMin.y, m_PlayerFloatSpeedRangeMax.y, value));

            for (int i = 0; i < m_Particles.Length; i++)

            {
                var main = m_Particles[i].main;
                main.startSpeed = Mathf.Lerp(m_ParticleFloatSpeedMinMax.x / (i + 1), m_ParticleFloatSpeedMinMax.y / (i + 1), value);
            }
        }

        /// <summary>
        /// Callback for the SubTrigger action.
        /// </summary>
        /// <param name="other"></param>
        /// <param name="entered"></param>
        void SubTriggered(Collider other, bool entered)
        {
            if (entered)
            {
                SubTriggerEntered(other);
            }
            else
            {
                SubTriggerExited(other);
            }
        }

        /// <summary>
        /// Callback for the SubTrigger OnTriggerEnter.
        /// </summary>
        /// <param name="other"></param>
        private void SubTriggerEntered(Collider other)
        {
            Rigidbody body = other.GetComponentInParent<Rigidbody>();
            if (body != null)
            {
                if (m_RigidbodyList.Contains(body)) return;
                // If the object is an interactable and it throws on detach, add it to the list.
                if (body.TryGetComponent(out UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable interactable))
                {
                    if (interactable.throwOnDetach)
                    {
                        m_RigidbodyList.Add(body);
                    }
                }
                else
                {
                    m_RigidbodyList.Add(body);
                }
            }
            else
            {
                // If the object is a character controller, add it to the list.
                if (other.TryGetComponent(out CharacterController controller))
                {
                    m_CharacterController = controller;
                    m_CharacterController.GetComponentInChildren<DynamicMoveProvider>().useGravity = false;
                }
            }
        }

        /// <summary>
        /// Callback for the SubTrigger OnTriggerExit.
        /// </summary>
        /// <param name="other"></param>
        private void SubTriggerExited(Collider other)
        {
            Rigidbody body = other.GetComponentInParent<Rigidbody>();
            if (body != null)
            {
                if (m_RigidbodyList.Contains(body))
                {
                    m_RigidbodyList.Remove(body);
                }
            }
            else
            {
                if (other.TryGetComponent(out CharacterController controller))
                {
                    controller.GetComponentInChildren<DynamicMoveProvider>().useGravity = true;
                    m_CharacterController = null;
                }
            }
        }

        /// <summary>
        /// Toggles the black hole effect.
        /// This is used to destroy objects within the zone.
        /// </summary>
        /// <param name="toggle"></param>
        public void ToggleBlackHole(bool toggle)
        {
            m_IsBlackHole = toggle;
            if (m_IsBlackHole)
            {
                m_Renderer.enabled = false;
                if (m_IsPowered)
                {
                    m_AntiGravitySubTrigger.subTriggerCollider.enabled = false;
                    foreach (var p in m_Particles)
                    {
                        p.Stop();
                    }

                    ToggleBlackHoles(true);
                }
            }
            else
            {
                if (m_IsPowered)
                {
                    m_Renderer.enabled = true;
                    m_AntiGravitySubTrigger.subTriggerCollider.enabled = true;
                    foreach (var p in m_Particles)
                    {
                        p.Play();
                    }

                    ToggleBlackHoles(false);
                }
            }
        }

        void ToggleBlackHoles(bool toggle)
        {
            foreach (GameObject g in m_BlackHoleObjects)
            {
                g.SetActive(toggle);
            }
        }
    }
}
