using System;
using Navigay.Position;
using Navigay.Routing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Navigay.UI
{
    public sealed class ReadyScreen : MonoBehaviour
    {
        public const string FindingYou = "Finding you…";
        public const string StepIntoTheOpen = "Step into the open.";
        public const string InsideBuilding = "You're inside a building. Step outside to start.";
        public const string NoPath = RouteResult.NoPathMessage;

        TextMeshProUGUI _label;

        public event Action Closed;

        public static string InsideNamed(string name)
        {
            return "You're inside " + name + ". Step outside to start.";
        }

        public void Show(ReadyView view)
        {
            gameObject.SetActive(true);
            _label.text = Words(view);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        void Awake()
        {
            var back = UiKit.Rounded(transform, "back", UiKit.Glass, 18f, false);
            var backRect = back.rectTransform;
            backRect.anchorMin = new Vector2(0f, 1f);
            backRect.anchorMax = new Vector2(0f, 1f);
            backRect.pivot = new Vector2(0f, 1f);
            backRect.anchoredPosition = new Vector2(16f, -8f);
            backRect.sizeDelta = new Vector2(96f, 36f);
            var backButton = back.gameObject.AddComponent<Button>();
            backButton.targetGraphic = back;
            UiKit.Press(backButton);
            backButton.onClick.AddListener(() => Closed?.Invoke());
            var row = back.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(10, 12, 0, 0);
            row.spacing = 4f;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = true;
            var mark = new GameObject("chevron", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            mark.transform.SetParent(back.transform, false);
            var markImage = mark.GetComponent<Image>();
            markImage.sprite = UiKit.ChevronSprite;
            markImage.color = Color.white;
            markImage.preserveAspect = true;
            markImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            var markLayout = mark.GetComponent<LayoutElement>();
            markLayout.preferredWidth = 14f;
            markLayout.minWidth = 14f;
            var backLabel = UiKit.Text(back.transform, "Back", 17f, FontStyles.Bold, Color.white, TextAlignmentOptions.MidlineLeft);
            backLabel.fontWeight = FontWeight.SemiBold;
            backLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 48f;

            var pill = UiKit.Rounded(transform, "pill", UiKit.Glass, 22f, false);
            var pillRect = pill.rectTransform;
            pillRect.anchorMin = new Vector2(0.5f, 0.5f);
            pillRect.anchorMax = new Vector2(0.5f, 0.5f);
            pillRect.pivot = new Vector2(0.5f, 0.5f);
            pillRect.anchoredPosition = Vector2.zero;
            pillRect.sizeDelta = new Vector2(0f, 44f);
            var fitter = pill.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            var layout = pill.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 0, 0);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = true;
            _label = UiKit.Text(pill.transform, FindingYou, 17f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            _label.fontWeight = FontWeight.SemiBold;
            var labelLayout = _label.gameObject.AddComponent<LayoutElement>();
            labelLayout.preferredHeight = 44f;
            gameObject.SetActive(false);
        }

        static string Words(ReadyView view)
        {
            switch (view.Word)
            {
                case ReadyWord.FindingYou:
                    return FindingYou;
                case ReadyWord.InsideNamed:
                    return InsideNamed(view.BlockName);
                case ReadyWord.InsideBuilding:
                    return InsideBuilding;
                case ReadyWord.StepIntoTheOpen:
                    return StepIntoTheOpen;
                case ReadyWord.NoPath:
                    return NoPath;
                default:
                    throw new ArgumentOutOfRangeException(nameof(view.Word), view.Word, null);
            }
        }
    }
}
