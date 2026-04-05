using UnityEngine;
using UnityEngine.UI;

namespace ColorChargeTD.Battle
{
    public sealed class EnemyHealthWorldBar : MonoBehaviour
    {
        [SerializeField] private Vector3 localOffset = new Vector3(0f, 1.1f, 0f);
        [SerializeField] private Vector2 worldBarSize = new Vector2(0.75f, 0.07f);
        [SerializeField] [Min(0.001f)] private float canvasUniformScale = 0.01f;
        [SerializeField] private Color backgroundColor = new Color(0.12f, 0.12f, 0.14f, 0.92f);
        [SerializeField] private Color fillColor = new Color(0.35f, 0.85f, 0.4f, 1f);

        [SerializeField] private RectTransform barRoot;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image fillImage;

        #region Built-in UI
        private static Sprite cachedWhiteSprite;
        private bool built;
        private Camera cachedCamera;
        #endregion

        #region Unity lifecycle
        private void Awake()
        {
            EnsureBuilt();
        }
        #endregion

        #region Public API
        public void Apply(EnemyRuntimeModel model)
        {
            EnsureBuilt();

            if (barRoot == null || fillImage == null)
            {
                return;
            }

            if (model == null || model.Definition == null)
            {
                barRoot.gameObject.SetActive(false);
                return;
            }

            int maxHp = model.Definition.HitPoints;
            int cur = model.CurrentHitPoints;
            bool show = cur > 0 && cur < maxHp;
            barRoot.gameObject.SetActive(show);

            if (!show)
            {
                return;
            }

            fillImage.fillAmount = Mathf.Clamp01((float)cur / maxHp);
            FaceCamera();
        }
        #endregion

        #region Setup
        private void EnsureBuilt()
        {
            if (built && barRoot != null && fillImage != null)
            {
                return;
            }

            if (barRoot != null && backgroundImage != null && fillImage != null)
            {
                built = true;
                return;
            }

            barRoot = new GameObject("EnemyHealthBar", typeof(RectTransform)).GetComponent<RectTransform>();
            barRoot.SetParent(transform, false);
            barRoot.localPosition = localOffset;
            barRoot.localRotation = Quaternion.identity;
            barRoot.anchorMin = new Vector2(0.5f, 0.5f);
            barRoot.anchorMax = new Vector2(0.5f, 0.5f);
            barRoot.pivot = new Vector2(0.5f, 0.5f);
            barRoot.sizeDelta = new Vector2(
                worldBarSize.x / canvasUniformScale,
                worldBarSize.y / canvasUniformScale);
            barRoot.localScale = Vector3.one * canvasUniformScale;

            Canvas canvas = barRoot.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 8;

            RectTransform panel = CreateUiChild(barRoot, "Panel");
            StretchFull(panel);
            backgroundImage = panel.gameObject.AddComponent<Image>();
            backgroundImage.sprite = GetWhiteSprite();
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.color = backgroundColor;
            backgroundImage.raycastTarget = false;

            RectTransform fillRt = CreateUiChild(panel, "Fill");
            StretchFull(fillRt);
            fillImage = fillRt.gameObject.AddComponent<Image>();
            fillImage.sprite = GetWhiteSprite();
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.color = fillColor;
            fillImage.fillAmount = 1f;
            fillImage.raycastTarget = false;

            built = true;
        }

        private static RectTransform CreateUiChild(RectTransform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
        }

        private static Sprite GetWhiteSprite()
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

        #region Billboard
        private void FaceCamera()
        {
            if (barRoot == null)
            {
                return;
            }

            if (cachedCamera == null)
            {
                cachedCamera = Camera.main;
            }

            if (cachedCamera == null)
            {
                return;
            }

            barRoot.rotation = cachedCamera.transform.rotation;
        }
        #endregion
    }
}
