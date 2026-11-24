using System;
using Navigay.Data;
using Navigay.Guidance;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Navigay.UI
{
    public sealed class ArrivalScreen : MonoBehaviour
    {
        public const string DoneLabel = "Done";
        public const string UnderpassLine = Guide.UnderpassLine;

        TextMeshProUGUI _name;
        TextMeshProUGUI _stats;
        TextMeshProUGUI _extra;
        GameObject _card;

        public event Action Closed;

        public void Show(PlaceRecord place, int minutes, int metres)
        {
            gameObject.SetActive(true);
            _name.text = place.Name;
            _stats.text = minutes + " min · " + metres + " m";
            var underpass = place.Kind == PlaceKind.Gate && place.Name != null && place.Name.IndexOf("Underpass", StringComparison.OrdinalIgnoreCase) >= 0;
            _extra.text = underpass ? UnderpassLine : string.Empty;
            _extra.gameObject.SetActive(underpass);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_card.transform);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        void Awake()
        {
            var card = UiKit.Rounded(transform, "card", UiKit.Glass, 22f, false);
            _card = card.gameObject;
            var rect = card.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -16f);
            rect.offsetMin = new Vector2(16f, rect.offsetMin.y);
            rect.offsetMax = new Vector2(-16f, rect.offsetMax.y);
            var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 16, 16);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = card.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _name = UiKit.Text(card.transform, string.Empty, 22f, FontStyles.Bold, Color.white, TextAlignmentOptions.TopLeft);
            _stats = UiKit.Text(card.transform, string.Empty, 15f, FontStyles.Normal, UiKit.Secondary, TextAlignmentOptions.TopLeft);
            _extra = UiKit.Text(card.transform, string.Empty, 15f, FontStyles.Normal, Color.white, TextAlignmentOptions.TopLeft);
            _extra.textWrappingMode = TextWrappingModes.Normal;

            var done = UiKit.Rounded(card.transform, "done", UiKit.Blue, 22f, false);
            UiKit.Preferred(done.gameObject, 44f);
            var button = done.gameObject.AddComponent<Button>();
            button.targetGraphic = done;
            UiKit.Press(button);
            button.onClick.AddListener(() => Closed?.Invoke());
            var label = UiKit.Text(done.transform, DoneLabel, 17f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            label.fontWeight = FontWeight.SemiBold;
            UiKit.Stretch(label.rectTransform);
            gameObject.SetActive(false);
        }
    }
}
