using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Content.Interaction;

namespace XRMultiplayer.MiniGames
{
    /// <summary>
    /// Represents a networked version of the Whack-A-Pig mini game.
    /// </summary>
    public class NetworkedWhackAPig : NetworkBehaviour
    {
        /// <summary>
        /// The proxy pigs to use for showing the trick.
        /// </summary>
        [SerializeField] GameObject[] m_ProxyPigs;

        /// <summary>
        /// The hammers to use for hitting the pigs.
        /// </summary>
        [SerializeField] NetworkPhysicsInteractable[] m_Hammers;

        [SerializeField] Collider[] m_HammerIgnoreColliders;

        /// <summary>
        /// The pig prefab to spawn.
        /// </summary>
        [SerializeField] GameObject m_PigPrefab;

        /// <summary>
        /// The bad pig prefab to spawn.
        /// </summary>
        [SerializeField] GameObject m_BadPigPrefab;

        /// <summary>
        /// The time to stay spawned before despawning.
        /// </summary>
        [SerializeField] Vector2 m_TimeToStaySpawnedMinMax = new Vector2(.5f, 1.0f);

        /// <summary>
        /// The time to wait before spawning a new pig.
        /// </summary>
        [SerializeField] float m_TimeToSpawn = .5f;

        /// <summary>
        /// The time to show the proxy pig.
        /// </summary>
        [SerializeField] float m_ProxyShowTime = .35f;

        /// <summary>
        /// The hidden height of the proxy pig.
        /// </summary>
        [SerializeField] float m_HiddenHeight = -0.25f;

        [SerializeField] float m_SpawnStartHeight = 0.0f;

        [SerializeField] float m_SpawnShowHeight = 0.1f;

        /// <summary>
        /// The trick height min and max for the proxy pig.
        /// </summary>
        [SerializeField] Vector2 m_TrickHeightMinMax = new Vector2(-0.25f, -.05f);

        /// <summary>
        /// The trick time min and max for the proxy pig.
        /// </summary>
        [SerializeField] Vector2 m_TrickTimeMinMax = new Vector2(1.0f, 3.0f);

        /// <summary>
        /// The bad pig spawn chance.
        /// </summary>
        [SerializeField] float m_BadPigSpawn = .7f;

        [SerializeField] Collider gameBarrierCollider;

        /// <summary>
        /// The mini game to use for handling the mini game logic.
        /// </summary>
        MiniGame_Whack m_MiniGame;

        /// <summary>
        /// The current breakable pig.
        /// </summary>
        Breakable m_CurrentBreakablePig;

        /// <summary>
        /// The current routine being played.
        /// </summary>
        IEnumerator m_CurrentRoutine;

        /// <summary>
        /// The current proxy ID.
        /// </summary>
        int m_CurrentProxyId = 0;

        /// <summary>
        /// Whether we are currently spawning a pig.
        /// </summary>
        bool m_Spawning = false;

        /// <inheritdoc/>
        void Start()
        {
            TryGetComponent(out m_MiniGame);
            foreach (var pig in m_ProxyPigs)
            {
                pig.SetActive(false);
            }

            foreach (var areaCollider in m_HammerIgnoreColliders)
            {
                foreach (var hammer in m_Hammers)
                {
                    foreach (var hammerInteractableCollider in hammer.baseInteractable.colliders)
                    {
                        Physics.IgnoreCollision(hammerInteractableCollider, areaCollider);
                    }
                }
            }
        }

        /// <summary>
        /// Starts the trick routine.
        /// </summary>
        [ContextMenu("Test Trick Routine")]
        void ShowTrickRoutine()
        {
            if (m_CurrentRoutine != null) StopTrickRoutine();
            m_CurrentRoutine = TrickRoutine();
            StartCoroutine(m_CurrentRoutine);
        }

        /// <summary>
        /// Stops the trick routine.
        /// </summary>
        [ContextMenu("Stop Trick Routine")]
        void StopTrickRoutine()
        {
            if (m_CurrentRoutine != null) StopCoroutine(m_CurrentRoutine);
            m_ProxyPigs[m_CurrentProxyId].SetActive(false);
            m_ProxyPigs[m_CurrentProxyId].transform.localPosition = new Vector3(m_ProxyPigs[m_CurrentProxyId].transform.localPosition.x, m_HiddenHeight, m_ProxyPigs[m_CurrentProxyId].transform.localPosition.z);
        }

        /// <summary>
        /// Performs the trick routine.
        /// </summary>
        /// <returns>An IEnumerator representing the trick routine.</returns>
        IEnumerator TrickRoutine()
        {
            while (true & !m_MiniGame.finished)
            {
                // Choose a random proxy pig to show
                m_CurrentProxyId = Random.Range(0, m_ProxyPigs.Length);
                float riseTime = m_ProxyShowTime / 2;
                Vector3 startPosition = new Vector3(m_ProxyPigs[m_CurrentProxyId].transform.localPosition.x, 0, m_ProxyPigs[m_CurrentProxyId].transform.localPosition.z);
                m_ProxyPigs[m_CurrentProxyId].transform.localPosition = startPosition + Vector3.up * m_HiddenHeight;
                m_ProxyPigs[m_CurrentProxyId].SetActive(true);

                float trickHeight = Random.Range(m_TrickHeightMinMax.x, m_TrickHeightMinMax.y);


                // Move the pig up to the trick height
                for (float i = 0; i < riseTime; i += Time.deltaTime)
                {
                    float perc = i / riseTime;

                    float lerpHeight = Mathf.Lerp(m_HiddenHeight, trickHeight, perc);
                    m_ProxyPigs[m_CurrentProxyId].transform.localPosition = startPosition + Vector3.up * lerpHeight;
                    yield return null;
                }
                m_ProxyPigs[m_CurrentProxyId].transform.localPosition = startPosition + Vector3.up * trickHeight;

                // Move the pig back down to the hidden height
                for (float i = 0; i < riseTime; i += Time.deltaTime)
                {
                    float perc = i / riseTime;

                    float lerpHeight = Mathf.Lerp(trickHeight, m_HiddenHeight, perc);
                    m_ProxyPigs[m_CurrentProxyId].transform.localPosition = startPosition + Vector3.up * lerpHeight;
                    yield return null;
                }
                m_ProxyPigs[m_CurrentProxyId].transform.localPosition = startPosition + Vector3.up * m_HiddenHeight;

                // Hide the pig
                m_ProxyPigs[m_CurrentProxyId].SetActive(false);
            }
        }

        /// <summary>
        /// Spawns a pig on the server.
        /// </summary>
        public void SpawnProcessServer()
        {
            if (IsServer)
                SpawnProcessClientRpc(Random.Range(m_TrickTimeMinMax.x, m_TrickTimeMinMax.y));
        }

        /// <summary>
        /// Spawns a pig on the clients.
        /// </summary>
        /// <param name="waitTime">The time to wait before spawning the pig.</param>
        [ClientRpc]
        public void SpawnProcessClientRpc(float waitTime)
        {
            m_Spawning = true;
            StartCoroutine(SpawnAfterTime(waitTime));
        }

        /// <summary>
        /// Spawns a pig after a certain amount of time.
        /// </summary>
        /// <param name="time">The time to wait before spawning the pig.</param>
        /// <returns>An IEnumerator representing the spawn after time routine.</returns>
        IEnumerator SpawnAfterTime(float time)
        {
            yield return new WaitForSeconds(.25f);
            ShowTrickRoutine();
            yield return new WaitForSeconds(time);
            SpawnNewPig();
        }

        /// <summary>
        /// Called from mini game manager when entering pre game state.
        /// </summary>
        public void ResetGame()
        {
            StopTrickRoutine();
            StopAllCoroutines();
            if (m_CurrentBreakablePig != null)
                Destroy(m_CurrentBreakablePig.gameObject);
        }

        /// <summary>
        /// Ends the game and cleans up.
        /// </summary>
        public void EndGame()
        {
            StopTrickRoutine();
            StopAllCoroutines();
            if (m_CurrentBreakablePig != null)
                Destroy(m_CurrentBreakablePig.gameObject);
        }

        /// <summary>
        /// Spawns a new pig on the server.
        /// </summary>
        public void SpawnNewPig()
        {
            if (!IsServer) return;
            SpawnPigClientRpc(Random.Range(0, m_ProxyPigs.Length), Random.value, Random.Range(m_TimeToStaySpawnedMinMax.x, m_TimeToStaySpawnedMinMax.y));
        }

        /// <summary>
        /// Spawns a pig on the clients.
        /// </summary>
        /// <param name="spawnIdx">The index of the spawn transform to use.</param>
        /// <param name="randomValue">A random value used to determine if a bad pig should be spawned.</param>
        [ClientRpc]
        void SpawnPigClientRpc(int spawnIdx, float randomValue, float timeToStaySpawned)
        {
            StopTrickRoutine();
            m_Spawning = false;
            m_CurrentRoutine = SpawnRoutine(spawnIdx, randomValue, timeToStaySpawned);
            StartCoroutine(m_CurrentRoutine);
        }

        /// <summary>
        /// Spawns a pig on the server and sets up collision ignoring.
        /// </summary>
        /// <param name="spawnIdx">The index of the spawn transform to use.</param>
        /// <param name="randomValue">A random value used to determine if a bad pig should be spawned.</param>
        /// <returns>An IEnumerator representing the spawn routine.</returns>
        IEnumerator SpawnRoutine(int spawnIdx, float randomValue, float timeToStaySpawned)
        {
            Vector3 spawnPos = m_ProxyPigs[spawnIdx].transform.position + (Vector3.up * m_SpawnStartHeight);

            // Determine if we are spawning a bad pig or a good pig
            m_CurrentBreakablePig = Instantiate(randomValue > m_BadPigSpawn ? m_BadPigPrefab : m_PigPrefab, spawnPos, m_ProxyPigs[spawnIdx].transform.rotation).GetComponent<Breakable>();

            m_CurrentBreakablePig.collider.enabled = false;
            // Updates Physics Ignore Collision to non local hammers cannot interact with the pigs
            foreach (var hammer in m_Hammers)
            {
                if (!hammer.IsOwner || !hammer.isInteracting)
                {
                    foreach (var collider in hammer.baseInteractable.colliders)
                    {
                        Physics.IgnoreCollision(collider, m_CurrentBreakablePig.collider);
                    }
                }
            }

            m_CurrentBreakablePig.onBreak += LocalPigDestroyed;

            Vector3 startPosition = m_CurrentBreakablePig.transform.position;
            // Move the pig up to the show height
            for (float i = 0; i < m_TimeToSpawn; i += Time.deltaTime)
            {
                float perc = i / m_TimeToSpawn;

                float lerpHeight = Mathf.Lerp(m_SpawnStartHeight, m_SpawnShowHeight, perc);
                m_CurrentBreakablePig.transform.position = startPosition + Vector3.up * lerpHeight;
                yield return null;
            }
            m_CurrentBreakablePig.transform.position = startPosition + Vector3.up * m_SpawnShowHeight;

            m_CurrentBreakablePig.collider.enabled = true;
            yield return new WaitForSeconds(timeToStaySpawned);
            m_CurrentBreakablePig.collider.enabled = false;
            // Move the pig back down to the hidden height
            for (float i = 0; i < m_TimeToSpawn; i += Time.deltaTime)
            {
                float perc = i / m_TimeToSpawn;

                float lerpHeight = Mathf.Lerp(m_SpawnShowHeight, m_SpawnStartHeight, perc);
                m_CurrentBreakablePig.transform.position = startPosition + Vector3.up * lerpHeight;
                yield return null;
            }
            m_CurrentBreakablePig.transform.position = startPosition + Vector3.up * m_SpawnStartHeight;

            // Destroy the pig and spawn a new one if we are the server
            Destroy(m_CurrentBreakablePig.gameObject);
            if (IsServer)
                SpawnProcessServer();
        }

        /// <summary>
        /// Handles the destruction of a pig on the local client.
        /// </summary>
        /// <param name="collider">The collider of the pig that was destroyed.</param>
        void LocalPigDestroyed(Collider collider)
        {
            NetworkPhysicsInteractable grabInteractable = collider.GetComponentInParent<NetworkPhysicsInteractable>();

            if (grabInteractable == null)        // Probably in editor testing
            {
                if (IsServer & !m_Spawning)
                    SpawnProcessServer();
            }
            else if (grabInteractable.IsOwner)
            {
                m_MiniGame.LocalPlayerScored(m_CurrentBreakablePig.pointValue);
                DestroyPigServerRpc(grabInteractable.OwnerClientId);
                StopCoroutine(m_CurrentRoutine);
            }
        }

        /// <summary>
        /// Destroys a pig on the server and notifies the clients.
        /// </summary>
        /// <param name="clientId">The client ID of the owner of the pig.</param>
        [ServerRpc(RequireOwnership = false)]
        void DestroyPigServerRpc(ulong clientId)
        {
            DestroyPigClientRpc(clientId);
        }

        /// <summary>
        /// Destroys a pig on the clients.
        /// </summary>
        /// <param name="clientId">The client ID of the owner of the pig.</param>
        [ClientRpc]
        void DestroyPigClientRpc(ulong clientId)
        {
            if (XRINetworkPlayer.LocalPlayer.OwnerClientId != clientId && m_CurrentBreakablePig != null)
            {
                StopCoroutine(m_CurrentRoutine);
                m_CurrentBreakablePig.onBreak -= LocalPigDestroyed;
                m_CurrentBreakablePig.Break(null);
            }

            if (IsServer & !m_Spawning)
                SpawnProcessServer();
        }
    }
}
