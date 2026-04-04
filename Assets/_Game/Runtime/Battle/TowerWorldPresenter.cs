using ColorChargeTD.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ColorChargeTD.Battle
{
    public sealed class TowerWorldPresenter : MonoBehaviour
    {
        [SerializeField] private Transform turretYawRoot;
        [SerializeField] private Transform muzzlePoint;
        [SerializeField] private Canvas statusCanvas;
        [SerializeField] private Image chargeFill;
        [SerializeField] private Text ammoLabel;
        [SerializeField] private Text productionLabel;
        [SerializeField] private Transform magazineSlotsRoot;
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
                Transform existing = turretYawRoot.Find("Muzzle");
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

            if (statusCanvas == null && !builtRuntimeUi)
            {
                BuildRuntimeStatusHud();
            }

            if (magazineSlotsRoot == null)
            {
                magazineSlotsRoot = transform.Find("AmmoSlots");
            }
        }

        private void BuildRuntimeStatusHud()
        {
            builtRuntimeUi = true;

            GameObject canvasGo = new GameObject("TowerStatusCanvas");
            canvasGo.transform.SetParent(transform, false);
            canvasGo.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            canvasGo.transform.localRotation = Quaternion.identity;
            canvasGo.transform.localScale = Vector3.one * 0.008f;

            statusCanvas = canvasGo.AddComponent<Canvas>();
            statusCanvas.renderMode = RenderMode.WorldSpace;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(180f, 70f);

            GameObject panelGo = CreateUiChild(canvasRect, "Panel", new Vector2(0f, 0f), new Vector2(170f, 60f));
            Image panelImage = panelGo.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.55f);

            GameObject fillGo = CreateUiChild(panelGo.GetComponent<RectTransform>(), "ChargeFill", new Vector2(0f, 12f), new Vector2(150f, 14f));
            chargeFill = fillGo.AddComponent<Image>();
            chargeFill.type = Image.Type.Filled;
            chargeFill.fillMethod = Image.FillMethod.Horizontal;
            chargeFill.color = new Color(0.85f, 0.35f, 0.25f, 1f);

            GameObject ammoGo = CreateUiChild(panelGo.GetComponent<RectTransform>(), "AmmoText", new Vector2(0f, -6f), new Vector2(160f, 22f));
            ammoLabel = ammoGo.AddComponent<Text>();
            ammoLabel.fontSize = 18;
            ammoLabel.alignment = TextAnchor.MiddleCenter;
            ammoLabel.color = Color.white;

            GameObject prodGo = CreateUiChild(panelGo.GetComponent<RectTransform>(), "ProductionText", new Vector2(0f, -28f), new Vector2(160f, 18f));
            productionLabel = prodGo.AddComponent<Text>();
            Font hudFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (hudFont == null)
            {
                hudFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            if (hudFont != null)
            {
                ammoLabel.font = hudFont;
                productionLabel.font = hudFont;
            }
            productionLabel.fontSize = 14;
            productionLabel.alignment = TextAnchor.MiddleCenter;
            productionLabel.color = new Color(0.75f, 0.9f, 0.75f, 1f);
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

            if (chargeFill != null)
            {
                float normalized = definition.Capacity > 0 ? Mathf.Clamp01(tower.Charge / definition.Capacity) : 0f;
                chargeFill.fillAmount = normalized;
            }

            if (ammoLabel != null)
            {
                int ready = Mathf.FloorToInt(tower.Charge);
                ammoLabel.text = ready + "/" + definition.Capacity;
            }

            if (productionLabel != null)
            {
                productionLabel.text = "+" + definition.ProductionPerSecond.ToString("0.#") + "/s gen";
            }

            UpdateMagazineSphereVisuals(tower);

            if (chargeFill != null)
            {
                Color c = chargeFill.color;
                c.a = tower.HasCharge ? 1f : 0.35f;
                chargeFill.color = c;
            }

            EnemyRuntimeModel aim = tower.CurrentAimTarget;
            if (aim != null)
            {
                ApplyYawToward(aim.Position, tower.Slot.Position);
            }
        }

        private void ApplyYawToward(Vector3 targetWorld, Vector3 towerWorld)
        {
            if (turretYawRoot == null)
            {
                return;
            }

            Vector3 delta = targetWorld - towerWorld;
            delta.y = 0f;
            if (delta.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion look = Quaternion.LookRotation(delta.normalized, Vector3.up);
            turretYawRoot.rotation = look;
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

            int slotCount = magazineSlotsRoot.childCount;
            if (magazineBallRoots != null && magazineBallRoots.Length == slotCount)
            {
                return;
            }

            magazineBallRoots = new GameObject[slotCount];
            for (int i = 0; i < slotCount; i++)
            {
                Transform slot = magazineSlotsRoot.GetChild(i);
                magazineBallRoots[i] = slot.childCount > 0 ? slot.GetChild(0).gameObject : null;
            }
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
