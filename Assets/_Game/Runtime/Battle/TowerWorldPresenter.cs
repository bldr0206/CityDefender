using ColorChargeTD.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ColorChargeTD.Battle
{
    public sealed class TowerWorldPresenter : MonoBehaviour
    {
        private static Sprite cachedWhiteSprite;

        [SerializeField] private Transform turretYawRoot;
        [SerializeField] private Transform muzzlePoint;
        [SerializeField] private Canvas statusCanvas;
        [SerializeField] private Transform magazineSlotsRoot;

        [Header("Regen radial HUD")]
        [Tooltip("World-space UI prefab (root: Canvas + RegenBackdrop / RegenTrack / RegenFill). Edit layout and colors in the prefab.")]
        [SerializeField] private GameObject regenRadialHudPrefab;
        [Tooltip("Optional: assign RegenFill Image directly instead of using the prefab.")]
        [SerializeField] private Image regenRadialFill;
        [SerializeField] private Transform regenHudAnchor;
        [SerializeField] private Vector3 regenHudLocalOffset = new Vector3(0.9f, 1.05f, 0f);
        [Tooltip("Used only when the HUD is built at runtime (no prefab and no RegenFill reference).")]
        [SerializeField] private float regenHudPixelSize = 112f;
        [Tooltip("Used only when the HUD is built at runtime.")]
        [SerializeField] private float regenHudWorldScale = 0.007f;

        private GameObject regenHudInstance;
        private Vector3 regenFillColorRgb = new Vector3(0.25f, 0.82f, 0.95f);
        private bool regenFillBaseCaptured;
        private GameObject[] magazineBallRoots;
        private Camera mainCamera;
        private bool builtRuntimeUi;

        #region UnityLifecycle
        private void Awake()
        {
            EnsureReferences();
        }

        private void LateUpdate()
        {
            if (statusCanvas == null || statusCanvas.renderMode != RenderMode.WorldSpace)
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

            Transform canvasTransform = statusCanvas.transform;
            Vector3 forward = mainCamera.transform.position - canvasTransform.position;
            if (forward.sqrMagnitude < 0.0001f)
            {
                return;
            }

            canvasTransform.rotation = Quaternion.LookRotation(-forward.normalized, Vector3.up);
        }

        #endregion

        #region Setup
        public Transform MuzzleTransform => muzzlePoint;

        private void EnsureReferences()
        {
            if (turretYawRoot == null)
            {
                Transform turret = transform.Find("View/Turret");
                if (turret != null)
                {
                    turretYawRoot = turret;
                }
                else
                {
                    Transform view = transform.Find("View");
                    turretYawRoot = view != null ? view : transform;
                }
            }

            if (muzzlePoint == null && turretYawRoot != null)
            {
                Transform existing = FindDeepChildByName(turretYawRoot, "Muzzle");
                if (existing != null)
                {
                    muzzlePoint = existing;
                }
                else
                {
                    GameObject muzzleGo = new GameObject("Muzzle");
                    muzzleGo.transform.SetParent(turretYawRoot, false);
                    muzzleGo.transform.localPosition = new Vector3(0.9f, 0.2f, 0f);
                    muzzlePoint = muzzleGo.transform;
                }
            }

            ResolveRegenHud();

            if (magazineSlotsRoot == null)
            {
                magazineSlotsRoot = transform.Find("AmmoSlots");
            }
        }

        private void ResolveRegenHud()
        {
            if (regenRadialFill == null && regenRadialHudPrefab != null && regenHudInstance == null)
            {
                Transform parent = regenHudAnchor != null ? regenHudAnchor : transform;
                regenHudInstance = Instantiate(regenRadialHudPrefab, parent);
                regenHudInstance.name = regenRadialHudPrefab.name;
                regenHudInstance.transform.localPosition = regenHudLocalOffset;
                regenHudInstance.transform.localRotation = Quaternion.identity;
                regenRadialFill = regenHudInstance.transform.Find("RegenFill")?.GetComponent<Image>();
                if (statusCanvas == null)
                {
                    statusCanvas = regenHudInstance.GetComponent<Canvas>();
                }
            }

            if (regenRadialFill == null)
            {
                Transform legacyFill = transform.Find("TowerRegenRadial/RegenFill");
                if (legacyFill != null)
                {
                    regenRadialFill = legacyFill.GetComponent<Image>();
                }
            }

            if (regenRadialFill != null)
            {
                if (statusCanvas == null)
                {
                    statusCanvas = regenRadialFill.GetComponentInParent<Canvas>();
                }

                EnsureRadialImagesUseProceduralWhite(regenRadialFill.transform.root);
                CaptureRegenFillBaseIfNeeded();
            }
            else if (!builtRuntimeUi)
            {
                BuildRuntimeRegenHud();
                CaptureRegenFillBaseIfNeeded();
            }
        }

        private void CaptureRegenFillBaseIfNeeded()
        {
            if (regenFillBaseCaptured || regenRadialFill == null)
            {
                return;
            }

            Color c = regenRadialFill.color;
            regenFillColorRgb = new Vector3(c.r, c.g, c.b);
            regenFillBaseCaptured = true;
        }

        private static void EnsureRadialImagesUseProceduralWhite(Transform hudRoot)
        {
            if (hudRoot == null)
            {
                return;
            }

            Graphic[] graphics = hudRoot.GetComponentsInChildren<Graphic>(true);
            Sprite white = GetOrCreateWhiteSprite();
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] is Image img && img.sprite == null)
                {
                    img.sprite = white;
                }
            }
        }

        private void BuildRuntimeRegenHud()
        {
            builtRuntimeUi = true;

            Transform parent = regenHudAnchor != null ? regenHudAnchor : transform;
            GameObject canvasGo = new GameObject("TowerRegenRadial");
            canvasGo.transform.SetParent(parent, false);
            canvasGo.transform.localPosition = regenHudLocalOffset;
            canvasGo.transform.localRotation = Quaternion.identity;
            canvasGo.transform.localScale = Vector3.one * regenHudWorldScale;

            statusCanvas = canvasGo.AddComponent<Canvas>();
            statusCanvas.renderMode = RenderMode.WorldSpace;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
            float size = Mathf.Max(32f, regenHudPixelSize);
            canvasRect.sizeDelta = new Vector2(size, size);

            Sprite white = GetOrCreateWhiteSprite();

            GameObject backdropGo = CreateUiChild(canvasRect, "RegenBackdrop", Vector2.zero, new Vector2(size, size));
            Image backdrop = backdropGo.AddComponent<Image>();
            backdrop.sprite = white;
            backdrop.type = Image.Type.Simple;
            backdrop.color = new Color(0f, 0f, 0f, 0.35f);
            backdrop.raycastTarget = false;

            GameObject trackGo = CreateUiChild(canvasRect, "RegenTrack", Vector2.zero, new Vector2(size * 0.92f, size * 0.92f));
            Image track = trackGo.AddComponent<Image>();
            track.sprite = white;
            track.type = Image.Type.Filled;
            track.fillMethod = Image.FillMethod.Radial360;
            track.fillOrigin = (int)Image.Origin360.Top;
            track.fillAmount = 1f;
            track.color = new Color(0.12f, 0.12f, 0.14f, 0.85f);
            track.raycastTarget = false;

            GameObject fillGo = CreateUiChild(canvasRect, "RegenFill", Vector2.zero, new Vector2(size * 0.92f, size * 0.92f));
            regenRadialFill = fillGo.AddComponent<Image>();
            regenRadialFill.sprite = white;
            regenRadialFill.type = Image.Type.Filled;
            regenRadialFill.fillMethod = Image.FillMethod.Radial360;
            regenRadialFill.fillOrigin = (int)Image.Origin360.Top;
            regenRadialFill.fillClockwise = true;
            regenRadialFill.color = new Color(regenFillColorRgb.x, regenFillColorRgb.y, regenFillColorRgb.z, 1f);
            regenRadialFill.fillAmount = 0f;
            regenRadialFill.raycastTarget = false;
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

        private static Transform FindDeepChildByName(Transform parent, string childName)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == childName)
                {
                    return child;
                }

                Transform nested = FindDeepChildByName(child, childName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private static GameObject CreateUiChild(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject go = new GameObject(name);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return go;
        }
        #endregion

        #region Presentation
        public void ApplyPresentation(TowerRuntimeModel tower)
        {
            if (tower == null || tower.Definition == null)
            {
                return;
            }

            EnsureReferences();
            TowerDefinition definition = tower.Definition;

            if (regenRadialFill != null)
            {
                float fill;
                if (tower.Charge >= definition.Capacity)
                {
                    fill = 1f;
                }
                else
                {
                    fill = Mathf.Clamp01(tower.Charge - Mathf.Floor(tower.Charge));
                }

                regenRadialFill.fillAmount = fill;
                Color c;
                c.r = regenFillColorRgb.x;
                c.g = regenFillColorRgb.y;
                c.b = regenFillColorRgb.z;
                c.a = 1f;
                regenRadialFill.color = c;
            }

            UpdateMagazineSphereVisuals(tower);

            ApplyTurretYawFromSimulation(tower);
        }

        private void ApplyTurretYawFromSimulation(TowerRuntimeModel tower)
        {
            if (turretYawRoot == null)
            {
                return;
            }

            turretYawRoot.rotation = Quaternion.Euler(0f, tower.TurretYawDegrees, 0f);
        }

        private void ResolveMagazineBallsIfNeeded()
        {
            if (magazineSlotsRoot == null)
            {
                magazineSlotsRoot = transform.Find("AmmoSlots");
            }

            if (magazineSlotsRoot == null)
            {
                magazineBallRoots = null;
                return;
            }

            int ballSlotCount = CountTransformsWithChildren(magazineSlotsRoot);
            if (ballSlotCount == 0)
            {
                magazineBallRoots = null;
                return;
            }

            if (magazineBallRoots != null && magazineBallRoots.Length == ballSlotCount)
            {
                return;
            }

            magazineBallRoots = new GameObject[ballSlotCount];
            int write = 0;
            for (int i = 0; i < magazineSlotsRoot.childCount; i++)
            {
                Transform slot = magazineSlotsRoot.GetChild(i);
                if (slot.childCount == 0)
                {
                    continue;
                }

                magazineBallRoots[write++] = slot.GetChild(0).gameObject;
            }
        }

        private static int CountTransformsWithChildren(Transform parent)
        {
            int count = 0;
            for (int i = 0; i < parent.childCount; i++)
            {
                if (parent.GetChild(i).childCount > 0)
                {
                    count++;
                }
            }

            return count;
        }

        private void UpdateMagazineSphereVisuals(TowerRuntimeModel tower)
        {
            ResolveMagazineBallsIfNeeded();
            if (magazineBallRoots == null || magazineBallRoots.Length == 0)
            {
                return;
            }

            int capacity = tower.Definition.Capacity;
            int wholeRounds = Mathf.Clamp(Mathf.FloorToInt(tower.Charge), 0, capacity);
            int visibleCount = Mathf.Min(wholeRounds, magazineBallRoots.Length, capacity);

            for (int i = 0; i < magazineBallRoots.Length; i++)
            {
                GameObject ball = magazineBallRoots[i];
                if (ball == null)
                {
                    continue;
                }

                bool visible = i < visibleCount;
                if (ball.activeSelf != visible)
                {
                    ball.SetActive(visible);
                }
            }
        }
        #endregion
    }
}
