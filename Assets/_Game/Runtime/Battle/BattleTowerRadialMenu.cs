using System;
using System.Collections.Generic;
using ColorChargeTD.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ColorChargeTD.Battle
{
    public sealed class BattleTowerRadialMenu : IDisposable
    {
        private GameObject root;
        private Font uiFont;

        public bool IsOpen => root != null;

        #region PublicAPI

        public void Show(
            Vector2 screenPosition,
            string slotId,
            IReadOnlyList<TowerDefinition> towers,
            int currentCredits,
            Func<string, string, bool> tryPlaceTower,
            Action refreshHud)
        {
            Hide();

            List<TowerDefinition> options = new List<TowerDefinition>();
            for (int i = 0; i < towers.Count; i++)
            {
                TowerDefinition t = towers[i];
                if (t != null && !string.IsNullOrWhiteSpace(t.TowerId))
                {
                    options.Add(t);
                }
            }

            if (options.Count == 0)
            {
                return;
            }

            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (uiFont == null)
            {
                uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            root = new GameObject("TowerRadialMenu");
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 800;
            root.AddComponent<GraphicRaycaster>();

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform canvasRt = root.GetComponent<RectTransform>();
            canvasRt.anchorMin = Vector2.zero;
            canvasRt.anchorMax = Vector2.one;
            canvasRt.sizeDelta = Vector2.zero;

            GameObject blocker = CreateChild(root.transform, "Blocker");
            StretchFull(blocker);
            Image blockerImage = blocker.AddComponent<Image>();
            blockerImage.color = new Color(0f, 0f, 0f, 0.4f);
            blockerImage.raycastTarget = true;
            Button blockerButton = blocker.AddComponent<Button>();
            blockerButton.targetGraphic = blockerImage;
            blockerButton.onClick.AddListener(Hide);

            GameObject wheel = CreateChild(root.transform, "Wheel");
            RectTransform wheelRt = wheel.GetComponent<RectTransform>();
            wheelRt.anchorMin = wheelRt.anchorMax = new Vector2(0.5f, 0.5f);
            wheelRt.pivot = new Vector2(0.5f, 0.5f);
            wheelRt.sizeDelta = Vector2.zero;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screenPosition, null, out Vector2 localWheel))
            {
                wheelRt.anchoredPosition = localWheel;
            }

            float ringRadius = 152f;
            int n = options.Count;

            for (int i = 0; i < n; i++)
            {
                TowerDefinition tower = options[i];
                float angle = (Mathf.PI * 2f / n) * i - Mathf.PI * 0.5f;
                Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ringRadius;

                GameObject btnGo = CreateChild(wheel.transform, "Tower_" + tower.TowerId);
                RectTransform brt = btnGo.GetComponent<RectTransform>();
                brt.sizeDelta = new Vector2(138f, 138f);
                brt.anchoredPosition = offset;
                brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
                brt.pivot = new Vector2(0.5f, 0.5f);

                Image img = btnGo.AddComponent<Image>();
                img.color = tower.BuildCost <= currentCredits
                    ? new Color(0.25f, 0.45f, 0.38f, 0.95f)
                    : new Color(0.35f, 0.35f, 0.35f, 0.85f);
                img.raycastTarget = true;

                Button btn = btnGo.AddComponent<Button>();
                btn.targetGraphic = img;
                btn.interactable = tower.BuildCost <= currentCredits;

                GameObject nameGo = CreateChild(btnGo.transform, "Name");
                RectTransform nameRt = nameGo.GetComponent<RectTransform>();
                nameRt.anchorMin = new Vector2(0f, 0.42f);
                nameRt.anchorMax = new Vector2(1f, 1f);
                nameRt.offsetMin = new Vector2(6f, 0f);
                nameRt.offsetMax = new Vector2(-6f, -6f);
                Text nameTxt = nameGo.AddComponent<Text>();
                nameTxt.font = uiFont;
                nameTxt.fontSize = 17;
                nameTxt.alignment = TextAnchor.MiddleCenter;
                nameTxt.color = new Color(1f, 1f, 1f, 0.92f);
                nameTxt.raycastTarget = false;
                nameTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
                nameTxt.verticalOverflow = VerticalWrapMode.Truncate;
                nameTxt.text = tower.DisplayName;

                GameObject priceGo = CreateChild(btnGo.transform, "Price");
                RectTransform priceRt = priceGo.GetComponent<RectTransform>();
                priceRt.anchorMin = new Vector2(0f, 0f);
                priceRt.anchorMax = new Vector2(1f, 0.42f);
                priceRt.offsetMin = new Vector2(6f, 6f);
                priceRt.offsetMax = new Vector2(-6f, -2f);
                Text priceTxt = priceGo.AddComponent<Text>();
                priceTxt.font = uiFont;
                priceTxt.fontSize = 26;
                priceTxt.fontStyle = FontStyle.Bold;
                priceTxt.alignment = TextAnchor.MiddleCenter;
                bool affordable = tower.BuildCost <= currentCredits;
                priceTxt.color = affordable
                    ? new Color(1f, 0.92f, 0.35f, 1f)
                    : new Color(0.55f, 0.52f, 0.42f, 1f);
                priceTxt.raycastTarget = false;
                priceTxt.text = $"{tower.BuildCost}";

                string tid = tower.TowerId;
                string sid = slotId;
                btn.onClick.AddListener(() =>
                {
                    if (tryPlaceTower(tid, sid))
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

        #region Factory

        private static GameObject CreateChild(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void StretchFull(GameObject go)
        {
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        #endregion
    }
}
