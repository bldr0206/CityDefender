using UnityEngine;
using UnityEngine.UI;

namespace ColorChargeTD.Presentation
{
    public static class ResultScreenViewLayout
    {
        #region SharedResources

        private static Sprite cachedWhiteSprite;
        private static Font cachedUiFont;

        public static Sprite GetWhiteSprite()
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

        private static Font GetUiFont()
        {
            if (cachedUiFont != null)
            {
                return cachedUiFont;
            }

            cachedUiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (cachedUiFont == null)
            {
                cachedUiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return cachedUiFont;
        }

        public static void ApplyDefaultFont(Text text)
        {
            Font font = GetUiFont();
            if (font != null)
            {
                text.font = font;
            }
        }

        #endregion

        #region Build

        public struct BuiltRefs
        {
            public GameObject contentRoot;
            public Text titleText;
            public Text detailText;
            public Button primaryButton;
            public Text primaryButtonCaption;
            public GameObject restartLevelRow;
            public Button restartLevelButton;
        }

        public static BuiltRefs BuildUnder(Transform root)
        {
            Transform parent = root;
            GameObject contentRoot = new GameObject("Content", typeof(RectTransform));
            contentRoot.transform.SetParent(parent, false);
            RectTransform contentRt = contentRoot.GetComponent<RectTransform>();
            StretchFull(contentRt);

            GameObject dim = new GameObject("Dim", typeof(RectTransform), typeof(Image));
            dim.transform.SetParent(contentRoot.transform, false);
            RectTransform dimRt = dim.GetComponent<RectTransform>();
            StretchFull(dimRt);
            Image dimImg = dim.GetComponent<Image>();
            dimImg.sprite = GetWhiteSprite();
            dimImg.color = new Color(0.04f, 0.06f, 0.1f, 0.82f);

            GameObject card = new GameObject("Card", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            card.transform.SetParent(contentRoot.transform, false);
            RectTransform cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(420f, 300f);
            Image cardImg = card.GetComponent<Image>();
            cardImg.sprite = GetWhiteSprite();
            cardImg.color = new Color(0.12f, 0.14f, 0.18f, 0.98f);

            VerticalLayoutGroup vlg = card.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(28, 28, 24, 24);
            vlg.spacing = 14f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;

            Text titleText = CreateTextLine(card.transform, "Title", 32, FontStyle.Bold, TextAnchor.MiddleCenter);
            Text detailText = CreateTextLine(card.transform, "Detail", 18, FontStyle.Normal, TextAnchor.MiddleCenter);

            GameObject buttonHost = new GameObject("PrimaryButton", typeof(RectTransform), typeof(LayoutElement), typeof(Image), typeof(Button));
            buttonHost.transform.SetParent(card.transform, false);
            LayoutElement buttonLayout = buttonHost.GetComponent<LayoutElement>();
            buttonLayout.minHeight = 48f;
            buttonLayout.preferredHeight = 48f;
            RectTransform buttonRt = buttonHost.GetComponent<RectTransform>();
            buttonRt.sizeDelta = new Vector2(0f, 48f);
            Image buttonBg = buttonHost.GetComponent<Image>();
            buttonBg.sprite = GetWhiteSprite();
            buttonBg.color = new Color(0.22f, 0.55f, 0.95f, 1f);
            Button primaryButton = buttonHost.GetComponent<Button>();
            primaryButton.targetGraphic = buttonBg;

            GameObject captionGo = new GameObject("Caption", typeof(RectTransform), typeof(Text));
            captionGo.transform.SetParent(buttonHost.transform, false);
            RectTransform capRt = captionGo.GetComponent<RectTransform>();
            StretchFull(capRt);
            Text primaryButtonCaption = captionGo.GetComponent<Text>();
            primaryButtonCaption.alignment = TextAnchor.MiddleCenter;
            primaryButtonCaption.color = Color.white;
            primaryButtonCaption.fontSize = 20;
            primaryButtonCaption.fontStyle = FontStyle.Bold;
            primaryButtonCaption.resizeTextForBestFit = false;
            primaryButtonCaption.supportRichText = false;
            ApplyDefaultFont(primaryButtonCaption);

            GameObject restartHost = new GameObject("RestartLevelButton", typeof(RectTransform), typeof(LayoutElement), typeof(Image), typeof(Button));
            restartHost.transform.SetParent(card.transform, false);
            GameObject restartLevelRow = restartHost;
            LayoutElement restartLayout = restartHost.GetComponent<LayoutElement>();
            restartLayout.minHeight = 32f;
            restartLayout.preferredHeight = 32f;
            RectTransform restartRt = restartHost.GetComponent<RectTransform>();
            restartRt.sizeDelta = new Vector2(0f, 32f);
            Image restartBg = restartHost.GetComponent<Image>();
            restartBg.sprite = GetWhiteSprite();
            restartBg.color = new Color(0.2f, 0.22f, 0.28f, 0.95f);
            Button restartLevelButton = restartHost.GetComponent<Button>();
            restartLevelButton.targetGraphic = restartBg;

            GameObject restartCaptionGo = new GameObject("Caption", typeof(RectTransform), typeof(Text));
            restartCaptionGo.transform.SetParent(restartHost.transform, false);
            RectTransform restartCapRt = restartCaptionGo.GetComponent<RectTransform>();
            StretchFull(restartCapRt);
            Text restartCaption = restartCaptionGo.GetComponent<Text>();
            restartCaption.alignment = TextAnchor.MiddleCenter;
            restartCaption.color = new Color(0.88f, 0.9f, 0.94f, 1f);
            restartCaption.fontSize = 14;
            restartCaption.fontStyle = FontStyle.Normal;
            restartCaption.resizeTextForBestFit = false;
            restartCaption.supportRichText = false;
            restartCaption.text = "Restart level";
            ApplyDefaultFont(restartCaption);
            restartLevelRow.SetActive(false);

            return new BuiltRefs
            {
                contentRoot = contentRoot,
                titleText = titleText,
                detailText = detailText,
                primaryButton = primaryButton,
                primaryButtonCaption = primaryButtonCaption,
                restartLevelRow = restartLevelRow,
                restartLevelButton = restartLevelButton,
            };
        }

        private static Text CreateTextLine(Transform parent, string name, int fontSize, FontStyle style, TextAnchor alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            LayoutElement le = go.GetComponent<LayoutElement>();
            le.minHeight = fontSize + 12f;
            le.preferredHeight = fontSize + 12f;
            Text text = go.GetComponent<Text>();
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = Color.white;
            text.supportRichText = false;
            ApplyDefaultFont(text);
            return text;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        #endregion
    }
}
