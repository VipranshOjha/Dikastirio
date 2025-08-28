using UnityEngine.Events;

namespace UnityEngine.XR.Content.Interaction
{
    /// <summary>
    /// Detects a collision with a tagged collider, replacing this object with a 'broken' version
    /// </summary>
    public class Breakable : MonoBehaviour
    {
        public UnityAction<Collider> onBreak;
        public int pointValue = 1;

#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
        public Collider collider => m_Collider;
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword

        [SerializeField]
        Collider m_Collider;

        [SerializeField]
        [Tooltip("The 'broken' version of this object.")]
        GameObject m_BrokenVersion;

        [SerializeField]
        [Tooltip("The tag a collider must have to cause this object to break.")]
        string m_ColliderTag = "Destroyer";

        bool m_Destroyed = false;

        void OnCollisionEnter(Collision collision)
        {
            if (m_Destroyed)
                return;

            if (collision.gameObject.CompareTag(m_ColliderTag))
            {
                Break(collision.collider);
            }
        }

        public void Break(Collider collider)
        {
            if (m_Destroyed) return;
            m_Destroyed = true;
            Instantiate(m_BrokenVersion, transform.position, transform.rotation);
            onBreak?.Invoke(collider);
            Destroy(gameObject);
        }
    }
}
