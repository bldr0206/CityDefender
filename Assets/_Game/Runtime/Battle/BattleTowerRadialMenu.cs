using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ColorChargeTD.Battle
{
    public sealed class BattleTowerRadialMenu : IDisposable
    {
        private GameObject root;

        public bool IsOpen => root != null;

        #region PublicAPI

        public void Show(
            Vector2 screenPosition,
            string slotId,
            IReadOnlyList<BuildRadialOptionData> options,
            int currentCredits,
            Func<string, string, bool> tryPlace,
            Action refreshHud,
            GameObject shellPrefab,
            GameObject optionPrefab)
        {
            Hide();

            if (options == null || options.Count == 0)
            {
                return;
            }

            List<BuildRadialOptionData> validOptions = new List<BuildRadialOptionData>(options.Count);
            for (int i = 0; i < options.Count; i++)
            {
                BuildRadialOptionData o = options[i];
                if (!string.IsNullOrWhiteSpace(o.OptionId))
                {
                    validOptions.Add(o);
                }
            }

            if (validOptions.Count == 0)
            {
                return;
            }

            if (shellPrefab == null || optionPrefab == null)
            {
                Debug.LogError("BattleTowerRadialMenu requires shell and option prefabs (assign on LevelSessionController).");
                return;
            }

            root = UnityEngine.Object.Instantiate(shellPrefab);
            BattleTowerRadialMenuShell shell = root.GetComponent<BattleTowerRadialMenuShell>();
            RectTransform canvasRt = root.GetComponent<RectTransform>();
            if (shell == null || canvasRt == null)
            {
                Debug.LogError("BattleTowerRadialMenuShell is missing on the shell prefab root.");
                UnityEngine.Object.Destroy(root);
                root = null;
                return;
            }

            RectTransform wheelRt = shell.Wheel;
            Button blocker = shell.BlockerButton;
            if (wheelRt == null || blocker == null)
            {
                Debug.LogError("BattleTowerRadialMenuShell: assign Wheel and Blocker Button in the prefab.");
                UnityEngine.Object.Destroy(root);
                root = null;
                return;
            }

            blocker.onClick.AddListener(Hide);

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screenPosition, null, out Vector2 localWheel))
            {
                wheelRt.anchoredPosition = localWheel;
            }

            const float ringRadius = 152f;
            int n = validOptions.Count;

            for (int i = 0; i < n; i++)
            {
                BuildRadialOptionData option = validOptions[i];
                float angle = (Mathf.PI * 2f / n) * i - Mathf.PI * 0.5f;
                Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ringRadius;

                GameObject optionGo = UnityEngine.Object.Instantiate(optionPrefab, wheelRt);
                optionGo.name = "BuildOption_" + option.OptionId;

                RectTransform brt = optionGo.GetComponent<RectTransform>();
                if (brt != null)
                {
                    brt.anchoredPosition = offset;
                    brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
                    brt.pivot = new Vector2(0.5f, 0.5f);
                }

                BattleTowerRadialOptionView view = optionGo.GetComponent<BattleTowerRadialOptionView>();
                if (view == null)
                {
                    Debug.LogError("BattleTowerRadialOptionView is missing on the option prefab.");
                    continue;
                }

                bool affordable = option.CanPurchase && option.BuildCost <= currentCredits;
                string oid = option.OptionId;
                string sid = slotId;
                view.Bind(option, affordable, () =>
                {
                    if (tryPlace(oid, sid))
                    {
                        Hide();
                        refreshHud?.Invoke();
                    }
                    else
                    {
                        refreshHud?.Invoke();
                    }
                });
            }
        }

        public void Hide()
        {
            if (root != null)
            {
                UnityEngine.Object.Destroy(root);
                root = null;
            }
        }

        public void Dispose()
        {
            Hide();
        }

        #endregion
    }
}
