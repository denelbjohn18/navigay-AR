using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Navigay.UI
{
    public sealed class PermissionsScreen : MonoBehaviour
    {
        public const string CameraSentence = "navigay uses the camera to show directions on the path.";
        public const string PreciseSentence = "iOS is reporting an accuracy worse than 1,000 m. Turn on Precise Location for navigay.";

        TextMeshProUGUI _sentence;

        public void Show(string sentence)
        {
            _sentence.text = sentence;
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        void Awake()
        {
            var background = UiKit.Panel(transform, "background", Color.black, null);
            UiKit.Stretch(background.rectTransform);

            var close = UiKit.Rounded(transform, "close", UiKit.Nested, 22f, false);
            var closeRect = close.rectTransform;
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.sizeDelta = new Vector2(44f, 44f);
            closeRect.anchoredPosition = new Vector2(-16f, -54f);
            var closeButton = close.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = close;
            closeButton.onClick.AddListener(Hide);
            var closeLabel = UiKit.Text(close.transform, "X", 17f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            UiKit.Stretch(closeLabel.rectTransform);

            _sentence = UiKit.Text(transform, string.Empty, 28f, FontStyles.Bold, Color.white, TextAlignmentOptions.TopLeft);
            _sentence.textWrappingMode = TextWrappingModes.Normal;
            _sentence.overflowMode = TextOverflowModes.Overflow;
            var sentenceRect = _sentence.rectTransform;
            sentenceRect.anchorMin = new Vector2(0f, 0.62f);
            sentenceRect.anchorMax = new Vector2(1f, 0.88f);
            sentenceRect.offsetMin = new Vector2(24f, 0f);
            sentenceRect.offsetMax = new Vector2(-24f, 0f);

            var open = UiKit.Rounded(transform, "open", UiKit.Blue, 25f, false);
            var openRect = open.rectTransform;
            openRect.anchorMin = new Vector2(0f, 0f);
            openRect.anchorMax = new Vector2(1f, 0f);
            openRect.pivot = new Vector2(0.5f, 0f);
            openRect.offsetMin = new Vector2(16f, 50f);
            openRect.offsetMax = new Vector2(-16f, 100f);
            var button = open.gameObject.AddComponent<Button>();
            button.targetGraphic = open;
            UiKit.Press(button);
            button.onClick.AddListener(() => Application.OpenURL("app-settings:"));
            var label = UiKit.Text(open.transform, "Open Settings", 17f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            label.fontWeight = FontWeight.SemiBold;
            UiKit.Stretch(label.rectTransform);

            gameObject.SetActive(false);
        }
    }
}
