using System;
using Navigay.Guidance;
using Navigay.Routing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Navigay.UI
{
    public sealed class GuideScreen : MonoBehaviour
    {
        RectTransform _arrow;
        RectTransform _edge;
        TextMeshProUGUI _turn;
        TextMeshProUGUI _metres;
        TextMeshProUGUI _minutes;
        TextMeshProUGUI _clock;
        TextMeshProUGUI _inside;
        GameObject _sign;
        TextMeshProUGUI _signLabel;

        const float OffScreenDeg = 50f;

        public event Action Ended;

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Draw(GuidanceState state, float headingDeg)
        {
            if (!gameObject.activeSelf)
            {
                Show();
            }

            var hold = state.Level == ConfidenceLevel.Hold || state.Arrived;
            _arrow.gameObject.SetActive(hold && !state.Arrived);
            _edge.gameObject.SetActive(!hold && CheckpointOffScreen(headingDeg, state));
            if (_arrow.gameObject.activeSelf)
            {
                Aim(_arrow, headingDeg, state);
            }

            if (_edge.gameObject.activeSelf)
            {
                Aim(_edge, headingDeg, state);
            }

            _turn.text = Line(state);
            _metres.text = state.ShownCountdownM.ToString();
            _minutes.text = state.MinutesLeft.ToString();
            _clock.text = Clock(state.MinutesLeft);
            _inside.gameObject.SetActive(state.Inside);
            _inside.text = state.Inside ? Guide.StepOutside : string.Empty;
            var showSign = !hold && state.Checkpoint != null;
            _sign.SetActive(showSign);
            if (showSign)
            {
                var total = state.Route.Checkpoints.Length;
                _signLabel.text = (state.CheckpointIndex + 1) + "/" + total + " · " + Verb(state.Turn) + " · " + state.Landmark + " · in " + state.ShownCountdownM + " m";
            }
        }

        static string Line(GuidanceState state)
        {
            var metres = state.ShownCountdownM + " m";
            var at = string.IsNullOrEmpty(state.Landmark) ? metres : Verb(state.Turn) + " at " + state.Landmark + " · " + metres;
            if (state.Turn == TurnKind.Arrive)
            {
                at = "Arrive at " + state.Landmark + " · " + metres;
            }
            else if (state.Turn == TurnKind.Straight)
            {
                at = "Continue straight · " + metres;
            }

            if (!string.IsNullOrEmpty(state.ExtraSentence) && !state.Inside)
            {
                return at + "\n" + state.ExtraSentence;
            }

            return at;
        }

        static string Verb(TurnKind turn)
        {
            switch (turn)
            {
                case TurnKind.Left:
                    return "Turn left";
                case TurnKind.Right:
                    return "Turn right";
                case TurnKind.Straight:
                    return "Continue straight";
                case TurnKind.Arrive:
                    return "Arrive";
                default:
                    throw new ArgumentOutOfRangeException(nameof(turn), turn, null);
            }
        }

        static string Clock(int minutes)
        {
            var when = DateTime.Now.AddMinutes(minutes);
            return when.ToString("H:mm");
        }

        static void Aim(RectTransform arrow, float headingDeg, GuidanceState state)
        {
            if (!TryBearing(state, out var bearing))
            {
                return;
            }

            arrow.localRotation = Quaternion.Euler(0f, 0f, -Mathf.DeltaAngle(headingDeg, bearing));
        }

        static bool PointAlong(GuidanceState state, out double x, out double y)
        {
            x = 0d;
            y = 0d;
            if (state.Route.Polyline == null || state.Route.Polyline.Length == 0)
            {
                return false;
            }

            var along = 0d;
            for (var i = 0; i < state.Route.Polyline.Length - 1; i++)
            {
                var ax = state.Route.Polyline[i][0];
                var ay = state.Route.Polyline[i][1];
                var bx = state.Route.Polyline[i + 1][0];
                var by = state.Route.Polyline[i + 1][1];
                var dx = bx - ax;
                var dy = by - ay;
                var length = Math.Sqrt((dx * dx) + (dy * dy));
                if (along + length >= state.AlongM)
                {
                    var t = length < 1e-6 ? 0d : (state.AlongM - along) / length;
                    x = ax + (dx * t);
                    y = ay + (dy * t);
                    return true;
                }

                along += length;
            }

            var last = state.Route.Polyline[state.Route.Polyline.Length - 1];
            x = last[0];
            y = last[1];
            return true;
        }

        public static bool CheckpointOffScreen(float headingDeg, GuidanceState state)
        {
            if (!TryBearing(state, out var bearing))
            {
                return false;
            }

            return Mathf.Abs(Mathf.DeltaAngle(headingDeg, bearing)) > OffScreenDeg;
        }

        static bool TryBearing(GuidanceState state, out float bearing)
        {
            bearing = 0f;
            var point = state.Checkpoint;
            if (point == null || state.Route?.Polyline == null)
            {
                return false;
            }

            var fromX = state.Route.Polyline[0][0];
            var fromY = state.Route.Polyline[0][1];
            if (state.AlongM >= 0d && PointAlong(state, out var x, out var y))
            {
                fromX = x;
                fromY = y;
            }

            bearing = Mathf.Atan2((float)(point.X - fromX), (float)(point.Y - fromY)) * Mathf.Rad2Deg;
            return true;
        }

        void Awake()
        {
            var card = UiKit.Rounded(transform, "turn", UiKit.Glass, 22f, false);
            var cardRect = card.rectTransform;
            cardRect.anchorMin = new Vector2(0f, 1f);
            cardRect.anchorMax = new Vector2(1f, 1f);
            cardRect.pivot = new Vector2(0.5f, 1f);
            cardRect.anchoredPosition = new Vector2(0f, -8f);
            cardRect.offsetMin = new Vector2(16f, cardRect.offsetMin.y);
            cardRect.offsetMax = new Vector2(-16f, cardRect.offsetMax.y);
            cardRect.sizeDelta = new Vector2(cardRect.sizeDelta.x, 64f);
            _turn = UiKit.Text(card.transform, string.Empty, 17f, FontStyles.Bold, Color.white, TextAlignmentOptions.MidlineLeft);
            _turn.fontWeight = FontWeight.SemiBold;
            _turn.textWrappingMode = TextWrappingModes.Normal;
            var turnRect = _turn.rectTransform;
            turnRect.anchorMin = Vector2.zero;
            turnRect.anchorMax = Vector2.one;
            turnRect.offsetMin = new Vector2(16f, 8f);
            turnRect.offsetMax = new Vector2(-16f, -8f);

            _inside = UiKit.Text(transform, string.Empty, 17f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            _inside.fontWeight = FontWeight.SemiBold;
            var insideRect = _inside.rectTransform;
            insideRect.anchorMin = new Vector2(0.5f, 0.5f);
            insideRect.anchorMax = new Vector2(0.5f, 0.5f);
            insideRect.sizeDelta = new Vector2(280f, 44f);

            _arrow = MakeArrow("arrow", new Vector2(0f, -150f), 72f);
            _edge = MakeArrow("edge", new Vector2(0f, 80f), 28f);
            _edge.gameObject.SetActive(false);

            _sign = UiKit.Rounded(transform, "sign", UiKit.Glass, 16f, false).gameObject;
            var signRect = (RectTransform)_sign.transform;
            signRect.anchorMin = new Vector2(0.5f, 0.55f);
            signRect.anchorMax = new Vector2(0.5f, 0.55f);
            signRect.sizeDelta = new Vector2(220f, 32f);
            _signLabel = UiKit.Text(_sign.transform, string.Empty, 15f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            _signLabel.fontWeight = FontWeight.SemiBold;
            UiKit.Stretch(_signLabel.rectTransform);
            _sign.SetActive(false);

            var bar = UiKit.Rounded(transform, "trip", UiKit.Glass, 22f, false);
            var barRect = bar.rectTransform;
            barRect.anchorMin = new Vector2(0f, 0f);
            barRect.anchorMax = new Vector2(1f, 0f);
            barRect.pivot = new Vector2(0.5f, 0f);
            barRect.anchoredPosition = new Vector2(0f, 8f);
            barRect.offsetMin = new Vector2(16f, barRect.offsetMin.y);
            barRect.offsetMax = new Vector2(-16f, barRect.offsetMax.y);
            barRect.sizeDelta = new Vector2(barRect.sizeDelta.x, 64f);
            var row = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(16, 8, 8, 8);
            row.spacing = 8f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = true;
            row.childForceExpandHeight = true;
            _metres = Stat(bar.transform, "metres left");
            _minutes = Stat(bar.transform, "minutes left");
            _clock = Stat(bar.transform, "arrival");

            var end = UiKit.Rounded(bar.transform, "end", UiKit.Red, 18f, false);
            UiKit.Preferred(end.gameObject, 36f).preferredWidth = 72f;
            var button = end.gameObject.AddComponent<Button>();
            button.targetGraphic = end;
            UiKit.Press(button);
            button.onClick.AddListener(() => Ended?.Invoke());
            var endLabel = UiKit.Text(end.transform, "End", 15f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            endLabel.fontWeight = FontWeight.SemiBold;
            UiKit.Stretch(endLabel.rectTransform);
            gameObject.SetActive(false);
        }

        TextMeshProUGUI Stat(Transform parent, string caption)
        {
            var column = new GameObject(caption, typeof(RectTransform), typeof(VerticalLayoutGroup));
            column.transform.SetParent(parent, false);
            var layout = column.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            var number = UiKit.Text(column.transform, "0", 22f, FontStyles.Bold, Color.white, TextAlignmentOptions.BottomLeft);
            number.fontWeight = FontWeight.SemiBold;
            UiKit.Text(column.transform, caption, 11f, FontStyles.Normal, UiKit.Secondary, TextAlignmentOptions.TopLeft);
            return number;
        }

        RectTransform MakeArrow(string name, Vector2 position, float size)
        {
            var arrow = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            arrow.SetParent(transform, false);
            arrow.anchorMin = new Vector2(0.5f, 0.5f);
            arrow.anchorMax = new Vector2(0.5f, 0.5f);
            arrow.pivot = new Vector2(0.5f, 0.5f);
            arrow.anchoredPosition = position;
            arrow.sizeDelta = new Vector2(size, size);
            var image = arrow.GetComponent<Image>();
            image.sprite = Arrow();
            image.color = Color.white;
            image.raycastTarget = false;
            return arrow;
        }

        static Sprite Arrow()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var nx = (x + 0.5f) / size;
                    var ny = (y + 0.5f) / size;
                    var half = 0.5f * ny;
                    var inside = ny > 0.12f && ny < 0.92f && Mathf.Abs(nx - 0.5f) < half;
                    pixels[(y * size) + x] = inside ? Color.white : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}
