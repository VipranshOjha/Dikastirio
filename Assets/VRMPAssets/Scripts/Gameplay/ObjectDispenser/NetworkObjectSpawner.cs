using UnityEngine;
using UnityEngine.Events;

namespace XRMultiplayer
{
    public class NetworkObjectSpawner : MonoBehaviour
    {
        public UnityEvent<float> OnSpawnDistanceUpdated;
        public UnityEvent<float> OnSpawnCooldownUpdated;
        public UnityEvent OnObjectSpawned;

        public Renderer fadeRenderer;

        Vector2 minMax = new Vector2(-1.0f, -0.25f);

        public void UpdateDistance(float distance)
        {
            fadeRenderer.material.SetFloat("_FadeOffset", Mathf.Lerp(minMax.x, minMax.y, distance));
            if (distance >= 1.0f)
            {
                fadeRenderer.gameObject.SetActive(false);
            }
        }

        public void ResetDistance()
        {
            fadeRenderer.material.SetFloat("_FadeOffset", minMax.x);
            fadeRenderer.gameObject.SetActive(true);
        }
    }
}
