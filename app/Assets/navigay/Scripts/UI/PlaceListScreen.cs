using System;
using Navigay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Navigay.UI
{
    public sealed class PlaceListScreen : MonoBehaviour
    {
        const float MaxKeyboardShare = 0.46f;

        PlaceRecord[] _places = Array.Empty<PlaceRecord>();
        Action<PlaceRecord> _onPlace;
        Action _onFocus;
        TMP_InputField _input;
        GameObject _clear;
        RectTransform _scroll;
        RectTransform _content;
        float _keyboardInset;

        public void Bind(PlaceRecord[] places, Action<PlaceRecord> onPlace, Action onFocus)
        {
            _places = places ?? Array.Empty<PlaceRecord>();
            _onPlace = onPlace;
            _onFocus = onFocus;
            Rebuild();
        }

        void Awake()
        {
            var header = new GameObject("header", typeof(RectTransform));
            header.transform.SetParent(transform, false);
            var headerRect = (RectTransform)header.transform;
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.offsetMin = new Vector2(16f, -64f);
            headerRect.offsetMax = new Vector2(-16f, 0f);

            var field = new GameObject("where", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            field.transform.SetParent(header.transform, false);
            UiKit.Stretch((RectTransform)field.transform);
            var hit = field.GetComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            hit.raycastTarget = true;
            _input = field.GetComponent<TMP_InputField>();
            _input.targetGraphic = hit;

            var viewport = new GameObject("viewport", typeof(RectTransform));
            viewport.transform.SetParent(field.transform, false);
            var viewportRect = (RectTransform)viewport.transform;
            UiKit.Stretch(viewportRect);
            viewportRect.offsetMax = new Vector2(-44f, 0f);

            var typed = UiKit.Text(viewport.transform, string.Empty, 34f, FontStyles.Bold, Color.white, TextAlignmentOptions.MidlineLeft);
            UiKit.Stretch(typed.rectTransform);
            var placeholder = UiKit.Text(viewport.transform, "Where to?", 34f, FontStyles.Bold, Color.white, TextAlignmentOptions.MidlineLeft);
            UiKit.Stretch(placeholder.rectTransform);
            _input.textViewport = viewportRect;
            _input.textComponent = typed;
            _input.placeholder = placeholder;
            _input.fontAsset = UiKit.Font;
            _input.pointSize = 34f;
            _input.lineType = TMP_InputField.LineType.SingleLine;
            _input.richText = false;
            _input.customCaretColor = true;
            _input.caretColor = Color.white;
            _input.caretWidth = 2;
            _input.onValueChanged.AddListener(_ => Rebuild());
            _input.onSelect.AddListener(_ => _onFocus?.Invoke());
            _input.onSubmit.AddListener(_ => _input.DeactivateInputField());

            _clear = new GameObject("clear", typeof(RectTransform), typeof(Image), typeof(Button));
            _clear.transform.SetParent(header.transform, false);
            var clearRect = (RectTransform)_clear.transform;
            clearRect.anchorMin = new Vector2(1f, 0.5f);
            clearRect.anchorMax = new Vector2(1f, 0.5f);
            clearRect.pivot = new Vector2(1f, 0.5f);
            clearRect.sizeDelta = new Vector2(44f, 44f);
            clearRect.anchoredPosition = Vector2.zero;
            var clearImage = _clear.GetComponent<Image>();
            clearImage.color = new Color(0f, 0f, 0f, 0f);
            var clearButton = _clear.GetComponent<Button>();
            clearButton.targetGraphic = clearImage;
            var clearLabel = UiKit.Text(_clear.transform, "\u00D7", 28f, FontStyles.Normal, UiKit.Secondary, TextAlignmentOptions.Center);
            UiKit.Stretch(clearLabel.rectTransform);
            clearButton.onClick.AddListener(() => _input.text = string.Empty);
            _clear.SetActive(false);

            var scrollObject = new GameObject("scroll", typeof(RectTransform), typeof(ScrollRect));
            scrollObject.transform.SetParent(transform, false);
            _scroll = (RectTransform)scrollObject.transform;
            _scroll.anchorMin = Vector2.zero;
            _scroll.anchorMax = Vector2.one;
            _scroll.offsetMin = Vector2.zero;
            _scroll.offsetMax = new Vector2(0f, -64f);
            var scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;

            var view = new GameObject("view", typeof(RectTransform), typeof(RectMask2D));
            view.transform.SetParent(scrollObject.transform, false);
            UiKit.Stretch((RectTransform)view.transform);
            scroll.viewport = (RectTransform)view.transform;

            var content = new GameObject("content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(view.transform, false);
            _content = (RectTransform)content.transform;
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(1f, 1f);
            _content.pivot = new Vector2(0.5f, 1f);
            _content.offsetMin = Vector2.zero;
            _content.offsetMax = Vector2.zero;
            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 0, 24);
            layout.spacing = 0f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = _content;
        }

        void Update()
        {
            var inset = 0f;
            if (TouchScreenKeyboard.visible)
            {
                var canvas = GetComponentInParent<Canvas>();
                var scale = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
                var canvasHeight = canvas != null ? ((RectTransform)canvas.transform).rect.height : UiKit.ReferenceHeight;
                inset = Mathf.Min(TouchScreenKeyboard.area.height / scale, canvasHeight * MaxKeyboardShare);
            }

            if (Mathf.Approximately(inset, _keyboardInset))
            {
                return;
            }

            _keyboardInset = inset;
            var offset = _scroll.offsetMin;
            offset.y = inset;
            _scroll.offsetMin = offset;
        }

        void Rebuild()
        {
            if (_content == null)
            {
                return;
            }

            for (var i = _content.childCount - 1; i >= 0; i--)
            {
                Destroy(_content.GetChild(i).gameObject);
            }

            _clear.SetActive(!string.IsNullOrEmpty(_input.text));
            var sections = PlaceList.Build(_places, _input.text, null, null, null);
            var firstHeading = true;
            foreach (var section in sections)
            {
                if (!string.IsNullOrEmpty(section.Heading))
                {
                    Spacer(firstHeading ? 8f : 20f);
                    var heading = UiKit.Text(_content, section.Heading, 13f, FontStyles.Normal, UiKit.Secondary, TextAlignmentOptions.MidlineLeft);
                    UiKit.Preferred(heading.gameObject, 22f);
                    firstHeading = false;
                }

                var rows = new GameObject("rows", typeof(RectTransform), typeof(VerticalLayoutGroup));
                rows.transform.SetParent(_content, false);
                var rowLayout = rows.GetComponent<VerticalLayoutGroup>();
                rowLayout.spacing = 8f;
                rowLayout.childAlignment = TextAnchor.UpperLeft;
                rowLayout.childControlWidth = true;
                rowLayout.childControlHeight = true;
                rowLayout.childForceExpandWidth = true;
                rowLayout.childForceExpandHeight = false;
                foreach (var place in section.Places)
                {
                    AddRow(rows.transform, place);
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        }

        void Spacer(float height)
        {
            var spacer = new GameObject("gap", typeof(RectTransform));
            spacer.transform.SetParent(_content, false);
            UiKit.Preferred(spacer, height);
        }

        void AddRow(Transform parent, PlaceRecord place)
        {
            var row = UiKit.Rounded(parent, "row", UiKit.Nested, 14f, false);
            UiKit.Preferred(row.gameObject, 52f);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(16, 12, 0, 0);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            var button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = row;
            UiKit.Press(button, 0.82f, 0.05f);
            var chosen = place;
            button.onClick.AddListener(() =>
            {
                _input.DeactivateInputField();
                _onPlace?.Invoke(chosen);
            });

            var name = UiKit.Text(row.transform, place.Name, 17f, FontStyles.Normal, Color.white, TextAlignmentOptions.MidlineLeft);
            var nameLayout = name.gameObject.AddComponent<LayoutElement>();
            nameLayout.flexibleWidth = 1f;
            nameLayout.minWidth = 40f;
            var mark = new GameObject("chevron", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            mark.transform.SetParent(row.transform, false);
            var markImage = mark.GetComponent<Image>();
            markImage.sprite = UiKit.ChevronSprite;
            markImage.color = UiKit.Secondary;
            markImage.preserveAspect = true;
            markImage.raycastTarget = false;
            var markLayout = mark.GetComponent<LayoutElement>();
            markLayout.preferredWidth = 18f;
            markLayout.minWidth = 18f;
            markLayout.flexibleWidth = 0f;
        }
    }
}
