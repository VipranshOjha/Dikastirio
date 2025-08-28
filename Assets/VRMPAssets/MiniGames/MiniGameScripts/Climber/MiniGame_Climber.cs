using System.Collections.Generic;
using UnityEngine;

namespace XRMultiplayer.MiniGames
{
    /// <summary>
    /// Represents a climbing mini-game.
    /// </summary>
    public class MiniGame_Climber : MiniGameBase
    {
        [SerializeField] SubTrigger[] m_FinishBells;
        [SerializeField, ColorUsage(true, true)] Color m_BellStartColor;
        [SerializeField, ColorUsage(true, true)] Color m_BellCompleteColor;

        List<Renderer> m_BellRenderers = new();
        protected float m_LocalPlayerTimer = 0.0f;

        ///<inheritdoc/>
        public override void Start()
        {
            base.Start();
            m_LocalPlayerTimer = 0.0f;
            m_CurrentTimer = m_GameLength;
            foreach (var bell in m_FinishBells)
            {
                bell.subTriggerCollider.enabled = false;

                var rend = bell.GetComponent<Renderer>();
                bell.OnTriggerAction += (Collider c, bool b) => { HitBell(c, b, rend); };
                m_BellRenderers.Add(rend);
            }
        }

        ////<inheritdoc/>
        void OnDestroy()
        {
            foreach (var bell in m_FinishBells)
            {
                bell.OnTriggerAction -= (Collider c, bool b) => { HitBell(c, b, bell.GetComponent<Renderer>()); };
            }
        }

        public override void SetupGame()
        {
            base.SetupGame();
            foreach (var rend in m_BellRenderers)
            {
                rend.material.color = m_BellStartColor;
                rend.material.SetColor("_EmissionColor", m_BellStartColor);
            }
        }

        ///<inheritdoc/>
        public override void StartGame()
        {
            base.StartGame();
            foreach (var bell in m_FinishBells)
            {
                bell.subTriggerCollider.enabled = true;
            }

            foreach (var rend in m_BellRenderers)
            {
                rend.material.color = m_BellStartColor;
                rend.material.SetColor("_EmissionColor", m_BellStartColor);
            }

            m_LocalPlayerTimer = 0.0f;
            m_CurrentTimer = m_GameLength;
        }

        ///<inheritdoc/>
        public override void UpdateGame(float deltaTime)
        {
            base.UpdateGame(deltaTime);
            if (!m_Finished)
            {
                m_LocalPlayerTimer += Time.deltaTime;
            }
            m_MiniGameManager.UpdatePlayerScores();
        }

        ///<inheritdoc/>
        public override void FinishGame(bool submitScore = true)
        {
            base.FinishGame(submitScore);

            foreach (var bells in m_FinishBells)
            {
                bells.subTriggerCollider.enabled = false;
            }

            if (submitScore)
                m_MiniGameManager.SubmitScoreServerRpc(m_LocalPlayerTimer, XRINetworkPlayer.LocalPlayer.OwnerClientId, true);
        }

        void HitBell(Collider collider, bool isTriggered, Renderer bell)
        {
            if (isTriggered && collider.CompareTag("PlayerHand"))
            {
                XRINetworkPlayer player = collider.gameObject.GetComponentInParent<XRINetworkPlayer>();
                if (player.IsLocalPlayer & !m_Finished)
                {
                    bell.material.color = m_BellCompleteColor;
                    bell.material.SetColor("_EmissionColor", m_BellCompleteColor);
                    var particle = bell.GetComponentInChildren<ParticleSystem>();
                    if (particle != null)
                    {
                        particle.Play();
                    }

                    FinishGame();
                }
            }
        }
    }
}
