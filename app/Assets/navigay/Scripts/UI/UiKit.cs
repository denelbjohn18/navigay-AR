using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Navigay.UI
{
    public static class UiKit
    {
        public const float ReferenceWidth = 402f;
        public const float ReferenceHeight = 874f;

        public static readonly Color Glass = new Color(28f / 255f, 28f / 255f, 30f / 255f, 0.92f);
        public static readonly Color Nested = new Color(44f / 255f, 44f / 255f, 46f / 255f, 1f);
        public static readonly Color Secondary = new Color(142f / 255f, 142f / 255f, 147f / 255f, 1f);
        public static readonly Color Blue = new Color(10f / 255f, 132f / 255f, 255f / 255f, 1f);
        public static readonly Color Red = new Color(1f, 0.231f, 0.188f, 1f);

        static TMP_FontAsset _font;
        static Sprite _chevron;
        static Sprite _magnifier;

        public static TMP_FontAsset Font => _font ??= LoadFont();

        public static Sprite ChevronSprite => _chevron ??= Chevron();

        public static Sprite MagnifierSprite => _magnifier ??= Magnifier();

        public static Canvas BuildCanvas()
        {
            var canvasObject = new GameObject(
                "canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
            canvas.sortingOrder = 10;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.matchWidthOrHeight = 0f;

            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("events");
                events.AddComponent<EventSystem>();
                events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }

            Stretch(Panel(canvasObject.transform, "background", Color.black, null).rectTransform);
            var safe = new GameObject("safe", typeof(RectTransform));
            safe.transform.SetParent(canvasObject.transform, false);
            Stretch((RectTransform)safe.transform);
            safe.AddComponent<SafeArea>();
            return canvas;
        }

        public static RoundedRect Rounded(Transform parent, string name, Color color, float radius, bool topOnly)
        {
            var panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            var image = panel.AddComponent<RoundedRect>();
            image.color = color;
            image.cornerRadius = radius;
            image.topOnly = topOnly;
            image.raycastTarget = true;
            image.SetVerticesDirty();
            return image;
        }

        public static Image Panel(Transform parent, string name, Color color, Sprite sprite)
        {
            var panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            var image = panel.AddComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            if (sprite != null)
            {
                image.type = Image.Type.Sliced;
            }

            return image;
        }

        public static TextMeshProUGUI Text(
            Transform parent,
            string value,
            float size,
            FontStyles style,
            Color color,
            TextAlignmentOptions alignment)
        {
            var label = new GameObject("text", typeof(RectTransform));
            label.transform.SetParent(parent, false);
            var text = label.AddComponent<TextMeshProUGUI>();
            text.font = Font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        public static LayoutElement Preferred(GameObject target, float height)
        {
            var element = target.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
            return element;
        }

        public static void Press(Button button, float pressed = 0.85f, float fade = 0.1f)
        {
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(pressed, pressed, pressed, 1f);
            colors.selectedColor = Color.white;
            colors.fadeDuration = fade;
            button.colors = colors;
        }

        static TMP_FontAsset LoadFont()
        {
            try
            {
                var os = UnityEngine.Font.CreateDynamicFontFromOSFont(
                    new[] { ".AppleSystemUIFont", "Helvetica Neue", "Arial" },
                    90);
                if (os != null)
                {
                    var asset = TMP_FontAsset.CreateFontAsset(os);
                    var fallback = TMP_Settings.defaultFontAsset;
                    if (asset != null && asset.material != null && asset.material.shader != null)
                    {
                        if (fallback != null && fallback.material != null && fallback.material.shader != null)
                        {
                            asset.material.shader = fallback.material.shader;
                        }

                        return asset;
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("navigay system font failed. " + exception.Message);
            }

            if (TMP_Settings.defaultFontAsset != null)
            {
                return TMP_Settings.defaultFontAsset;
            }

            throw new InvalidOperationException("navigay has no text font.");
        }

        static Sprite Chevron()
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var upper = DistanceToSegment(x, y, 9f, 7f, 23f, 16f);
                    var lower = DistanceToSegment(x, y, 23f, 16f, 9f, 25f);
                    var alpha = Mathf.Clamp01(2.4f - Mathf.Min(upper, lower));
                    pixels[(y * size) + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 1f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        static Sprite Magnifier()
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var lens = Mathf.Abs(new Vector2(x - 13f, y - 19f).magnitude - 8f);
                    var handle = DistanceToSegment(x, y, 19f, 13f, 26f, 6f);
                    var alpha = Mathf.Clamp01(2f - Mathf.Min(lens, handle));
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 1f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        static float DistanceToSegment(float x, float y, float ax, float ay, float bx, float by)
        {
            var dx = bx - ax;
            var dy = by - ay;
            var length = (dx * dx) + (dy * dy);
            var t = length <= 0f ? 0f : Mathf.Clamp01((((x - ax) * dx) + ((y - ay) * dy)) / length);
            var px = ax + (t * dx);
            var py = ay + (t * dy);
            return Mathf.Sqrt(((x - px) * (x - px)) + ((y - py) * (y - py)));
        }
    }
}
