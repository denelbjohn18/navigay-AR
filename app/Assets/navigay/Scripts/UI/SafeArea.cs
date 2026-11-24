using UnityEngine;

namespace Navigay.UI
{
    public sealed class SafeArea : MonoBehaviour
    {
        Rect _last;

        void Awake()
        {
            Apply();
        }

        void Update()
        {
            if (Screen.safeArea != _last)
            {
                Apply();
            }
        }

        void Apply()
        {
            _last = Screen.safeArea;
            if (Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(_last.xMin / Screen.width, _last.yMin / Screen.height);
            rect.anchorMax = new Vector2(_last.xMax / Screen.width, _last.yMax / Screen.height);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
