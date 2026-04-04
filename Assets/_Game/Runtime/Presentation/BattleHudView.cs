using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ColorChargeTD.Presentation
{
    public sealed class BattleHudView : MonoBehaviour
    {
        #region References

        [SerializeField] private Text levelTitleText;
        [SerializeField] private Text resourceText;
        [SerializeField] private Text livesText;
        [SerializeField] private Text waveText;
        [SerializeField] private Image waveProgressFill;
        [SerializeField] private Text slotsText;
        [SerializeField] private GameObject chargeWarningRoot;
        [SerializeField] private GameObject startWaveRoot;
        [SerializeField] private Button startWaveButton;
        [SerializeField] private RectTransform resourcePulseRoot;

        #endregion

        #region StartWave

        private Action onStartWaveClicked;
        private Vector3 resourcePulseBaseScale = Vector3.one;
        private float lastResourcePulseUnscaledTime = -100f;
        private Coroutine resourcePulseRoutine;
        private const float ResourcePulseCooldownSeconds = 0.12f;

        private void Awake()
        {
            if (resourcePulseRoot != null)
            {
                resourcePulseBaseScale = resourcePulseRoot.localScale;
            }

            if (startWaveButton != null)
            {
                startWaveButton.onClick.AddListener(OnStartWaveButtonClicked);
            }
        }

        private void OnDestroy()
        {
            if (startWaveButton != null)
            {
                startWaveButton.onClick.RemoveListener(OnStartWaveButtonClicked);
            }
        }

        private void OnStartWaveButtonClicked()
        {
            onStartWaveClicked?.Invoke();
        }

        public void SetStartWaveClickedHandler(Action handler)
        {
            onStartWaveClicked = handler;
        }

        public void SetStartWaveVisible(bool visible)
        {
            if (startWaveRoot != null)
            {
                startWaveRoot.SetActive(visible);
                return;
            }

            if (startWaveButton != null)
            {
                startWaveButton.gameObject.SetActive(visible);
            }
        }

        public void SetStartWaveButtonVisible(bool visible)
        {
            if (startWaveButton != null)
            {
                startWaveButton.gameObject.SetActive(visible);
            }
        }

        public void SetPlaceTowerHintVisible(bool visible)
        {
            if (chargeWarningRoot != null)
            {
                chargeWarningRoot.SetActive(visible);
            }
        }

        #endregion

        #region PublicAPI

        public void SetLevelTitle(string title)
        {
            if (levelTitleText == null)
            {
                return;
            }

            levelTitleText.text = string.IsNullOrWhiteSpace(title) ? "—" : title;
        }

        public void SetResource(int value)
        {
            int v = Mathf.Max(0, value);
            if (resourceText != null)
            {
                resourceText.text = $"Credits: {v}";
            }
        }

        public void SetLives(int value)
        {
            int v = Mathf.Max(0, value);
            if (livesText != null)
            {
                livesText.text = $"Lives: {v}";
            }
        }

        public void SetWaveLabel(int currentWave, int totalWaves)
        {
            if (waveText == null)
            {
                return;
            }

            if (totalWaves <= 0)
            {
                waveText.text = "Waves: —";
                return;
            }

            int shown = Mathf.Clamp(currentWave, 1, totalWaves);
            waveText.text = $"Wave {shown} / {totalWaves}";
        }

        public void SetWaveProgress(float value)
        {
            float p = Mathf.Clamp01(value);
            if (waveProgressFill != null)
            {
                waveProgressFill.fillAmount = p;
            }
        }

        public void SetSlots(int freeSlots, int totalSlots)
        {
            if (slotsText == null)
            {
                return;
            }

            int free = Mathf.Clamp(freeSlots, 0, Mathf.Max(0, totalSlots));
            int total = Mathf.Max(0, totalSlots);
            slotsText.text = $"Slots: {free} / {total}";
        }

        public void PlayResourceEarnPulse()
        {
            if (resourcePulseRoot == null)
            {
                return;
            }

            float now = Time.unscaledTime;
            if (now - lastResourcePulseUnscaledTime < ResourcePulseCooldownSeconds)
            {
                return;
            }

            lastResourcePulseUnscaledTime = now;

            if (resourcePulseRoutine != null)
            {
                StopCoroutine(resourcePulseRoutine);
            }

            resourcePulseRoutine = StartCoroutine(ResourcePulseRoutine());
        }

        #endregion

        #region ResourcePulse

        private IEnumerator ResourcePulseRoutine()
        {
            RectTransform rt = resourcePulseRoot;
            Vector3 baseScale = resourcePulseBaseScale;
            float half = 0.09f;

            float t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / half);
                float k = 1f + 0.08f * Mathf.Sin(u * Mathf.PI * 0.5f);
                rt.localScale = baseScale * k;
                yield return null;
            }

            t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / half);
                float k = 1.08f - 0.08f * Mathf.Sin(u * Mathf.PI * 0.5f);
                rt.localScale = baseScale * k;
                yield return null;
            }

            rt.localScale = baseScale;
            resourcePulseRoutine = null;
        }

        #endregion
    }
}
