using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Utilities.Tweenables.Primitives;

namespace XRMultiplayer
{
    public class UIComponentToggler : CalloutGazeController
    {
        [Header("Component Toggling")]
        [SerializeField] CanvasGroup m_CanvasGroup;
        [SerializeField] TooltipUI m_TooltipUI;
        [SerializeField] float m_FadeDuration = .25f;
        [SerializeField] Vector2 m_MinMaxThresholdDistance = new Vector2(2.0f, 5.0f);
        [SerializeField] Vector2 m_MinMaxFacingThreshold = new Vector2(.8f, .995f);
        [SerializeField] float m_MaxRenderingDistance = 15.0f;
        [SerializeField] List<MonoBehaviour> m_ComponentsToToggle;
        [SerializeField] GameObject[] m_ObjectsToToggle;
        [SerializeField] bool m_StartHidden = true;
        [SerializeField] bool m_DisableCanvasGroupObject = false;

#pragma warning disable CS0618 // Type or member is obsolete
        FloatTweenableVariable m_FloatFadeTweenableVariable = new FloatTweenableVariable();
#pragma warning restore CS0618 // Type or member is obsolete
        bool m_Hidden = false;
        bool m_InRange = false;

        Coroutine m_FadeRoutine;
        // Start is called before the first frame update
        void Start()
        {
            if (m_GazeTransform == null)
            {
                m_GazeTransform = Camera.main.transform;
            }

            if (m_CanvasGroup == null)
            {
                m_CanvasGroup = GetComponentInChildren<CanvasGroup>();
            }

            if (m_TooltipUI == null)
            {
                m_TooltipUI = GetComponentInChildren<TooltipUI>();
            }

            m_FacingThreshold = .98f;
            m_FacingEntered.AddListener(delegate { ToggleFade(false); });
            m_FacingExited.AddListener(delegate { ToggleFade(true); });

            m_FloatFadeTweenableVariable.Subscribe(UpdateFade);

            if (m_StartHidden)
            {
                ToggleFade(true);
            }
        }

        protected override void Update()
        {
            base.Update();

            float currentDistance = Vector3.Distance(transform.position, m_GazeTransform.position);

            if (m_InRange)
            {
                float perc = (Mathf.Clamp(currentDistance, m_MinMaxThresholdDistance.x, m_MinMaxThresholdDistance.y) - m_MinMaxThresholdDistance.x) / (m_MinMaxThresholdDistance.y - m_MinMaxThresholdDistance.x);
                m_FacingThreshold = Mathf.Lerp(m_MinMaxFacingThreshold.x, m_MinMaxFacingThreshold.y, perc);

                if (currentDistance > m_MaxRenderingDistance)
                {
                    m_InRange = false;
                    ToggleFade(true);
                }
            }
            else
            {
                if (currentDistance <= m_MaxRenderingDistance)
                {
                    m_InRange = true;
                }
            }
        }

        private void OnDestroy()
        {
            m_FacingEntered.RemoveListener(delegate { ToggleFade(false); });
            m_FacingExited.RemoveListener(delegate { ToggleFade(true); });
        }

        [ContextMenu("Get References")]
        void FindRendererReferences()
        {
            m_ComponentsToToggle = new List<MonoBehaviour>();
            List<Image> images = new List<Image>(GetComponentsInChildren<Image>());
            List<TMP_Text> texts = new List<TMP_Text>(GetComponentsInChildren<TMP_Text>());

            foreach (Image image in images)
            {
                m_ComponentsToToggle.Add(image);
            }

            foreach (TMP_Text text in texts)
            {
                m_ComponentsToToggle.Add(text);
            }
        }

        [ContextMenu("Toggle Components")]
        void ToggleFade()
        {
            ToggleFade(!m_Hidden);
        }

        void ToggleFade(bool toggle)
        {
            m_Hidden = toggle;
            if (!m_Hidden)
            {
                ToggleComponents(true);
            }

            if (m_FadeRoutine != null) StopCoroutine(m_FadeRoutine);
            m_FadeRoutine = StartCoroutine(m_FloatFadeTweenableVariable.PlaySequence(m_FloatFadeTweenableVariable.Value, m_Hidden ? 0.0f : 1.0f, m_FadeDuration, CompleteFade));
        }

        void ToggleComponents(bool show)
        {
            foreach (var c in m_ComponentsToToggle)
            {
                if (c != null)
                    c.enabled = show;
                else
                    Utils.Log("Component Toggler is missing references", 1);
            }

            foreach (GameObject go in m_ObjectsToToggle)
            {
                if (go != null)
                    go.SetActive(show);
                else
                    Utils.Log("Component Toggler is missing references", 1);
            }

            if (m_DisableCanvasGroupObject)
            {
                if (m_CanvasGroup != null)
                    m_CanvasGroup.gameObject.SetActive(show);
                else
                    Utils.Log("Component Toggler is missing references", 1);
            }

            if (m_TooltipUI != null)
            {
                if (!show)
                {
                    if (m_TooltipUI != null)
                        m_TooltipUI.ResetTooltip();
                    else
                        Utils.Log("Component Toggler is missing references", 1);
                }
            }
        }

        void UpdateFade(float fadeAmount)
        {
            if (m_CanvasGroup != null)
            {
                m_CanvasGroup.alpha = fadeAmount;
            }
        }

        void CompleteFade()
        {
            if (m_FloatFadeTweenableVariable.Value <= 0.0f)
            {
                ToggleComponents(false);
            }
        }

        public void ToggleShow(bool show)
        {
            if (show)
            {
                m_FacingEntered.Invoke();
            }
            else
            {
                CheckPointerExit();
            }
        }
    }
}
