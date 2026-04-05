using ColorChargeTD.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ColorChargeTD.Battle
{
    public sealed class StructureAuxiliaryIncomeRingPresenter : MonoBehaviour
    {
        private static Sprite cachedWhiteSprite;

        [SerializeField] private Vector3 localOffset = new Vector3(0f, 1.2f, 0f);
        [SerializeField] private float pixelSize = 96f;
        [SerializeField] private float worldScale = 0.0065f;

        private Canvas canvas;
        private Image fillImage;
        private PlacedStructureRuntimeModel boundModel;
        private AuxiliaryBuildingDefinition boundDefinition;
        private Camera mainCamera;
        private bool built;

        #region PublicAPI

        public void Setup(PlacedStructureRuntimeModel model, AuxiliaryBuildingDefinition definition)
        {
            boundModel = model;
            boundDefinition = definition;
            bool active = definition != null && definition.HasPeriodicIncome;
            if (!active)
            {
                if (canvas != null)
                {
                    canvas.gameObject.SetActive(false);
                }

                return;
            }

            EnsureBuilt();
            canvas.gameObject.SetActive(true);
            RefreshFill();
        }

        public void RefreshFill()
        {
            if (fillImage == null || boundModel == null || boundDefinition == null || !boundDefinition.HasPeriodicIncome)
            {
                return;
            }

            float period = boundDefinition.IncomePeriodSeconds;
            float fill = period > 0.0001f
                ? Mathf.Clamp01(boundModel.AuxiliaryIncomeElapsed / period)
                : 1f;
            fillImage.fillAmount = fill;
        }

        public void Hide()
        {
            boundModel = null;
            boundDefinition = null;
            if (canvas != null)
            {
                canvas.gameObject.SetActive(false);
            }
        }

        #endregion

        #region UnityLifecycle

        private void LateUpdate()
        {
            if (canvas == null || !canvas.gameObject.activeSelf)
            {
                return;
            }

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera == null)
            {
                return;
            }

            Transform t = canvas.transform;
            Vector3 forward = mainCamera.transform.position - t.position;
            if (forward.sqrMagnitude < 0.0001f)
            {
                return;
            }

            t.rotation = Quaternion.LookRotation(-forward.normalized, Vector3.up);
        }

        #endregion

        #region BuildUI

        private void EnsureBuilt()
        {
            if (built)
            {
                return;
            }

            built = true;

            GameObject canvasGo = new GameObject("FarmIncomeRadial");
            canvasGo.transform.SetParent(transform, false);
            canvasGo.transform.localPosition = localOffset;
            canvasGo.transform.localRotation = Quaternion.identity;
            canvasGo.transform.localScale = Vector3.one * worldScale;

            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 12;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
            float size = Mathf.Max(32f, pixelSize);
            canvasRect.sizeDelta = new Vector2(size, size);

            Sprite white = GetOrCreateWhiteSprite();

            GameObject trackGo = CreateUiChild(canvasRect, "IncomeTrack", Vector2.zero, new Vector2(size * 0.92f, size * 0.92f));
            Image track = trackGo.AddComponent<Image>();
            track.sprite = white;
            track.type = Image.Type.Filled;
            track.fillMethod = Image.FillMethod.Radial360;
            track.fillOrigin = (int)Image.Origin360.Top;
            track.fillAmount = 1f;
            track.color = new Color(0.15f, 0.12f, 0.08f, 0.9f);
            track.raycastTarget = false;

            GameObject fillGo = CreateUiChild(canvasRect, "IncomeFill", Vector2.zero, new Vector2(size * 0.88f, size * 0.88f));
            fillImage = fillGo.AddComponent<Image>();
            fillImage.sprite = white;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Radial360;
            fillImage.fillOrigin = (int)Image.Origin360.Top;
            fillImage.fillClockwise = true;
            fillImage.color = new Color(1f, 0.82f, 0.25f, 0.95f);
            fillImage.fillAmount = 0f;
            fillImage.raycastTarget = false;
        }

        private static GameObject CreateUiChild(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            GameObject go = new GameObject(name);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            return go;
        }

        private static Sprite GetOrCreateWhiteSprite()
        {
            if (cachedWhiteSprite != null)
            {
                return cachedWhiteSprite;
            }

            Texture2D tex = Texture2D.whiteTexture;
            cachedWhiteSprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f);
            return cachedWhiteSprite;
        }

        #endregion
    }
}
