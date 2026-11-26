using System;
using System.Collections.Generic;
using Navigay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Navigay.UI
{
    public sealed class PlaceListScreen : MonoBehaviour
    {
        const float MaxKeyboardShare = 0.46f;
        const float Gutter = 16f;
        const float SearchTop = 56f;
        const float SearchHeight = 48f;
        const float CategoriesTop = 114f;
        const float CategoryHeight = 44f;

        static readonly Color SelectedFill = new Color32(24, 51, 78, 255);
        static readonly Color SelectedText = new Color32(155, 208, 255, 255);

        PlaceRecord[] _places = Array.Empty<PlaceRecord>();
        readonly List<(RoundedRect Background, TMP_Text Label)> _chips = new();
        Predicate<PlaceRecord> _category;
        Action<PlaceRecord> _onPlace;
        Action _onFocus;
        TMP_InputField _input;
        GameObject _clear;
        RectTransform _scroll;
        RectTransform _content;
        ScrollRect _results;
        ScrollRect _categoryScroll;
        RectTransform _categoryContent;
        float _keyboardInset;

        public void Bind(PlaceRecord[] places, Action<PlaceRecord> onPlace, Action onFocus)
        {
            _places = places ?? Array.Empty<PlaceRecord>();
            _onPlace = onPlace;
            _onFocus = onFocus;
            BuildCategories();
        }

        void Awake()
        {
            var title = UiKit.Text(transform, "Where to", 34f, FontStyles.Bold, Color.white, TextAlignmentOptions.MidlineLeft);
            PlaceAtTop(title.rectTransform, 0f, SearchTop, Gutter);
            BuildSearch();
            BuildCategoryStrip();
            BuildResults();
        }

        void BuildSearch()
        {
            var field = UiKit.Rounded(transform, "destination search", UiKit.Nested, 14f, false);
            PlaceAtTop(field.rectTransform, SearchTop, SearchHeight, Gutter);
            _input = field.gameObject.AddComponent<TMP_InputField>();
            _input.targetGraphic = field;
            _input.transition = Selectable.Transition.None;
            var icon = UiKit.Panel(field.transform, "magnifier", UiKit.Secondary, UiKit.MagnifierSprite);
            icon.type = Image.Type.Simple;
            icon.raycastTarget = false;
            icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            icon.rectTransform.sizeDelta = new Vector2(20f, 20f);
            icon.rectTransform.anchoredPosition = new Vector2(23f, 0f);

            var viewport = new GameObject("viewport", typeof(RectTransform));
            viewport.transform.SetParent(field.transform, false);
            var viewportRect = (RectTransform)viewport.transform;
            UiKit.Stretch(viewportRect);
            viewportRect.offsetMin = new Vector2(42f, 0f);
            viewportRect.offsetMax = new Vector2(-44f, 0f);
            viewport.AddComponent<RectMask2D>();

            var typed = UiKit.Text(viewport.transform, string.Empty, 17f, FontStyles.Normal, Color.white, TextAlignmentOptions.MidlineLeft);
            typed.overflowMode = TextOverflowModes.Overflow;
            UiKit.Stretch(typed.rectTransform);
            var placeholder = UiKit.Text(viewport.transform, "Search destinations", 17f, FontStyles.Normal, UiKit.Secondary, TextAlignmentOptions.MidlineLeft);
            UiKit.Stretch(placeholder.rectTransform);
            _input.textViewport = viewportRect;
            _input.textComponent = typed;
            _input.placeholder = placeholder;
            _input.fontAsset = UiKit.Font;
            _input.pointSize = 17f;
            _input.lineType = TMP_InputField.LineType.SingleLine;
            _input.richText = false;
            _input.customCaretColor = true;
            _input.caretColor = Color.white;
            _input.caretWidth = 2;
            _input.shouldHideMobileInput = true;
            _input.onValueChanged.AddListener(_ => Rebuild());
            _input.onSelect.AddListener(_ => _onFocus?.Invoke());
            _input.onSubmit.AddListener(_ => _input.DeactivateInputField());

            _clear = new GameObject("clear", typeof(RectTransform), typeof(Image), typeof(Button));
            _clear.transform.SetParent(field.transform, false);
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
        }

        void BuildCategoryStrip()
        {
            var strip = UiKit.Panel(transform, "categories", Color.clear, null);
            PlaceAtTop(strip.rectTransform, CategoriesTop, CategoryHeight, 0f);
            strip.gameObject.AddComponent<RectMask2D>();
            _categoryScroll = strip.gameObject.AddComponent<ScrollRect>();
            _categoryScroll.viewport = strip.rectTransform;
            _categoryScroll.vertical = false;
            _categoryScroll.movementType = ScrollRect.MovementType.Clamped;
            _categoryScroll.scrollSensitivity = 24f;

            var content = new GameObject("category content", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(strip.transform, false);
            _categoryContent = (RectTransform)content.transform;
            _categoryContent.anchorMin = Vector2.zero;
            _categoryContent.anchorMax = Vector2.up;
            _categoryContent.pivot = new Vector2(0f, 0.5f);
            _categoryContent.sizeDelta = Vector2.zero;
            _categoryContent.anchoredPosition = Vector2.zero;
            var layout = content.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset((int)Gutter, (int)Gutter, 0, 0);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            content.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            _categoryScroll.content = _categoryContent;
        }

        void BuildCategories()
        {
            RemoveChildren(_categoryContent);
            _chips.Clear();
            AddCategory("All", null);
            foreach (var category in PlaceList.Categories)
            {
                if (Array.Exists(_places, category.Includes))
                {
                    AddCategory(category.Label, category.Includes);
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_categoryContent);
            _categoryScroll.horizontalNormalizedPosition = 0f;
            SelectCategory(0, null);
        }

        void AddCategory(string title, Predicate<PlaceRecord> includes)
        {
            var chip = UiKit.Rounded(_categoryContent, title + " category", UiKit.Nested, CategoryHeight / 2f, false);
            var label = UiKit.Text(chip.transform, title, 14f, FontStyles.Normal, Color.white, TextAlignmentOptions.Center);
            label.fontWeight = FontWeight.SemiBold;
            label.overflowMode = TextOverflowModes.Overflow;
            UiKit.Stretch(label.rectTransform);
            var size = UiKit.Preferred(chip.gameObject, CategoryHeight);
            size.preferredWidth = Mathf.Max(CategoryHeight, Mathf.Ceil(label.GetPreferredValues(title).x) + 24f);
            var button = chip.gameObject.AddComponent<Button>();
            button.targetGraphic = chip;
            UiKit.Press(button);
            var index = _chips.Count;
            _chips.Add((chip, label));
            button.onClick.AddListener(() => SelectCategory(index, includes));
        }

        void SelectCategory(int index, Predicate<PlaceRecord> includes)
        {
            _category = includes;
            for (var i = 0; i < _chips.Count; i++)
            {
                var selected = i == index;
                _chips[i].Background.color = selected ? SelectedFill : UiKit.Nested;
                _chips[i].Label.color = selected ? SelectedText : Color.white;
            }

            Rebuild();
        }

        static void PlaceAtTop(RectTransform rect, float top, float height, float gutter)
        {
            rect.anchorMin = Vector2.up;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(gutter, -top - height);
            rect.offsetMax = new Vector2(-gutter, -top);
        }

        void BuildResults()
        {
            var scrollObject = new GameObject("scroll", typeof(RectTransform), typeof(ScrollRect));
            scrollObject.transform.SetParent(transform, false);
            _scroll = (RectTransform)scrollObject.transform;
            _scroll.anchorMin = Vector2.zero;
            _scroll.anchorMax = Vector2.one;
            _scroll.offsetMin = Vector2.zero;
            _scroll.offsetMax = new Vector2(0f, -(CategoriesTop + CategoryHeight + 10f));
            _results = scrollObject.GetComponent<ScrollRect>();
            _results.horizontal = false;
            _results.movementType = ScrollRect.MovementType.Clamped;
            _results.scrollSensitivity = 24f;

            var view = UiKit.Panel(scrollObject.transform, "view", Color.clear, null);
            view.gameObject.AddComponent<RectMask2D>();
            UiKit.Stretch(view.rectTransform);
            _results.viewport = view.rectTransform;

            var content = new GameObject("content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(view.transform, false);
            _content = (RectTransform)content.transform;
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(1f, 1f);
            _content.pivot = new Vector2(0.5f, 1f);
            _content.offsetMin = Vector2.zero;
            _content.offsetMax = Vector2.zero;
            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset((int)Gutter, (int)Gutter, 0, 24);
            layout.spacing = 0f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _results.content = _content;
        }

        static void RemoveChildren(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
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

            RemoveChildren(_content);
            _clear.SetActive(!string.IsNullOrEmpty(_input.text));
            var sections = PlaceList.Build(_places, _input.text, null, null, null, _category);
            if (sections.Length == 0 || (sections.Length == 1 && sections[0].Places.Length == 0))
            {
                var empty = UiKit.Text(_content, "No destinations found", 15f, FontStyles.Normal, UiKit.Secondary, TextAlignmentOptions.Center);
                UiKit.Preferred(empty.gameObject, 64f);
            }

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
            _results.StopMovement();
            _results.verticalNormalizedPosition = 1f;
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
