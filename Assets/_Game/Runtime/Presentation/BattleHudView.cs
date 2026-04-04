using System;
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

        #endregion

        #region StartWave

        private Action onStartWaveClicked;

        private void Awake()
        {
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

        public void SetChargeWarningVisible(bool value)
        {
            if (chargeWarningRoot != null)
            {
                chargeWarningRoot.SetActive(value);
            }
        }

        #endregion
    }
}
