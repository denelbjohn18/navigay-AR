using System;
using System.IO;
using Navigay.Config;
using Navigay.Data;
using Navigay.Position;
using UnityEngine;
using UnityEngine.UI;

namespace Navigay.UI
{
    public sealed class AppRoot : MonoBehaviour
    {
        public const string LocationSentence = "navigay uses your location to guide you around GHS.";

        PermissionsScreen _permissions;
        bool _asked;
        bool _denied;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<AppRoot>() != null)
            {
                return;
            }

            var host = new GameObject("navigay");
            host.AddComponent<AppRoot>();
        }

        void Awake()
        {
            GraphData graph;
            try
            {
                graph = GraphLoader.LoadFile(Path.Combine(Application.streamingAssetsPath, "graph.json"));
            }
            catch (Exception exception)
            {
                Debug.LogError("navigay stopped. graph.json failed to load. " + exception.Message);
                enabled = false;
                Application.Quit();
                return;
            }

            var camera = Camera.main;
            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
            }

            var canvas = UiKit.BuildCanvas();
            var background = canvas.transform.Find("background").GetComponent<Image>();
            var safe = canvas.transform.Find("safe");
            var list = NewScreen("place list", safe).AddComponent<PlaceListScreen>();
            var card = NewScreen("place card", safe).AddComponent<PlaceCard>();
            _permissions = NewScreen("permissions", canvas.transform).AddComponent<PermissionsScreen>();
            var ready = NewScreen("ready", safe).AddComponent<ReadyScreen>();
            var guide = NewScreen("guide", safe).AddComponent<GuideScreen>();
            var arrival = NewScreen("arrival", safe).AddComponent<ArrivalScreen>();
            var sensors = gameObject.AddComponent<PhoneSensors>();
            var where = new WhereAmI();
            var config = NavConfig.CreateDefault();
            var walk = gameObject.AddComponent<WalkController>();
            walk.Bind(graph, sensors, where, ready, guide, arrival, _permissions, background, list.gameObject, card, config);
            PlaceRecord chosen = null;
            list.Bind(graph.Places, place =>
            {
                chosen = place;
                card.Show(place, PlaceCard.TimeUntilStuck);
            }, AskLocation);
            card.Closed += card.Hide;
            card.GoPressed += () =>
            {
                if (chosen == null)
                {
                    return;
                }

                AskLocation();
                if (_denied)
                {
                    _permissions.Show(LocationSentence);
                    return;
                }

                walk.Go(chosen);
            };
        }

        void Update()
        {
            if (!_asked)
            {
                return;
            }

            if (Input.location.status == LocationServiceStatus.Failed)
            {
                if (!_denied)
                {
                    _denied = true;
                    _permissions.Show(LocationSentence);
                }

                return;
            }

            if (_denied && Input.location.status == LocationServiceStatus.Running)
            {
                _denied = false;
                _permissions.Hide();
            }
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus || !_denied)
            {
                return;
            }

            Input.location.Start(1f, 0.1f);
            _asked = true;
        }

        void AskLocation()
        {
            if (_denied)
            {
                _permissions.Show(LocationSentence);
                return;
            }

            var status = Input.location.status;
            if (status == LocationServiceStatus.Running || status == LocationServiceStatus.Initializing)
            {
                return;
            }

            Input.location.Start(1f, 0.1f);
            _asked = true;
        }

        static GameObject NewScreen(string name, Transform parent)
        {
            var screen = new GameObject(name, typeof(RectTransform));
            screen.transform.SetParent(parent, false);
            UiKit.Stretch((RectTransform)screen.transform);
            return screen;
        }
    }
}
