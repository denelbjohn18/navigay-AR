using System;
using TMPro;
using Navigay.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Navigay.UI
{
    public sealed class PlaceCard : MonoBehaviour
    {
        public const string TimeUntilStuck = "Step outside to see the time.";
        public const string NearestEntrance = "Nearest entrance";

        TextMeshProUGUI _name;
        TextMeshProUGUI _type;
        TextMeshProUGUI _time;

        public event Action GoPressed;

        public event Action Closed;

        public void Show(PlaceRecord place, string timeLine)
        {
            _name.text = place.Name;
            _type.text = PlaceTypeWord.Word(place.Kind);
            _time.text = timeLine;
            gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
            var sheet = transform.Find("sheet") as RectTransform;
            if (sheet != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(sheet);
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        void Awake()
        {
            var dim = UiKit.Panel(transform, "dim", new Color(0f, 0f, 0f, 0.6f), null);
            UiKit.Stretch(dim.rectTransform);
            dim.raycastTarget = true;
            var dimButton = dim.gameObject.AddComponent<Button>();
            dimButton.targetGraphic = dim;
            dimButton.transition = Selectable.Transition.None;
            dimButton.onClick.AddListener(() => Closed?.Invoke());

            var sheet = UiKit.Rounded(transform, "sheet", UiKit.Glass, 36f, true);
            var sheetRect = sheet.rectTransform;
            sheetRect.anchorMin = new Vector2(0f, 0f);
            sheetRect.anchorMax = new Vector2(1f, 0f);
            sheetRect.pivot = new Vector2(0.5f, 0f);
            sheetRect.anchoredPosition = Vector2.zero;
            sheetRect.sizeDelta = new Vector2(0f, 340f);
            var layout = sheet.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 8, 20);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = sheet.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var grabberRow = new GameObject("grabber", typeof(RectTransform));
            grabberRow.transform.SetParent(sheet.transform, false);
            UiKit.Preferred(grabberRow, 5f);
            var grabber = UiKit.Rounded(grabberRow.transform, "bar", UiKit.Secondary, 2.5f, false);
            var grabberRect = grabber.rectTransform;
            grabberRect.anchorMin = new Vector2(0.5f, 0.5f);
            grabberRect.anchorMax = new Vector2(0.5f, 0.5f);
            grabberRect.sizeDelta = new Vector2(36f, 5f);

            var title = new GameObject("title", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            title.transform.SetParent(sheet.transform, false);
            UiKit.Preferred(title, 44f);
            var titleLayout = title.GetComponent<HorizontalLayoutGroup>();
            titleLayout.padding = new RectOffset(0, 40, 0, 0);
            titleLayout.childAlignment = TextAnchor.MiddleLeft;
            titleLayout.childControlWidth = true;
            titleLayout.childControlHeight = true;
            titleLayout.childForceExpandWidth = false;
            titleLayout.childForceExpandHeight = true;
            _name = UiKit.Text(title.transform, string.Empty, 22f, FontStyles.Bold, Color.white, TextAlignmentOptions.MidlineLeft);
            _name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var close = UiKit.Rounded(sheet.transform, "close", UiKit.Nested, 22f, false);
            var closeRect = close.rectTransform;
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.sizeDelta = new Vector2(44f, 44f);
            closeRect.anchoredPosition = new Vector2(-12f, -12f);
            var closeIgnore = close.gameObject.AddComponent<LayoutElement>();
            closeIgnore.ignoreLayout = true;
            var closeButton = close.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = close;
            closeButton.onClick.AddListener(() => Closed?.Invoke());
            var closeLabel = UiKit.Text(close.transform, "X", 17f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            UiKit.Stretch(closeLabel.rectTransform);

            _type = UiKit.Text(sheet.transform, string.Empty, 13f, FontStyles.Normal, UiKit.Secondary, TextAlignmentOptions.MidlineLeft);
            UiKit.Preferred(_type.gameObject, 18f);

            var timeBox = UiKit.Rounded(sheet.transform, "time", UiKit.Nested, 14f, false);
            UiKit.Preferred(timeBox.gameObject, 126f);
            _time = UiKit.Text(timeBox.transform, TimeUntilStuck, 22f, FontStyles.Bold, Color.white, TextAlignmentOptions.TopLeft);
            _time.fontWeight = FontWeight.SemiBold;
            _time.textWrappingMode = TextWrappingModes.Normal;
            _time.overflowMode = TextOverflowModes.Overflow;
            var timeRect = _time.rectTransform;
            timeRect.anchorMin = new Vector2(0f, 1f);
            timeRect.anchorMax = new Vector2(1f, 1f);
            timeRect.pivot = new Vector2(0f, 1f);
            timeRect.offsetMin = new Vector2(16f, -88f);
            timeRect.offsetMax = new Vector2(-16f, -16f);
            var entrance = UiKit.Text(timeBox.transform, NearestEntrance, 13f, FontStyles.Normal, UiKit.Secondary, TextAlignmentOptions.MidlineLeft);
            var entranceRect = entrance.rectTransform;
            entranceRect.anchorMin = new Vector2(0f, 0f);
            entranceRect.anchorMax = new Vector2(1f, 0f);
            entranceRect.pivot = new Vector2(0f, 0f);
            entranceRect.offsetMin = new Vector2(16f, 16f);
            entranceRect.offsetMax = new Vector2(-16f, 36f);

            var go = UiKit.Rounded(sheet.transform, "go", UiKit.Blue, 25f, false);
            UiKit.Preferred(go.gameObject, 50f);
            var goButton = go.gameObject.AddComponent<Button>();
            goButton.targetGraphic = go;
            UiKit.Press(goButton);
            goButton.onClick.AddListener(() => GoPressed?.Invoke());
            var goLabel = UiKit.Text(go.transform, "Go", 17f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            goLabel.fontWeight = FontWeight.SemiBold;
            UiKit.Stretch(goLabel.rectTransform);

            gameObject.SetActive(false);
        }
    }
}
