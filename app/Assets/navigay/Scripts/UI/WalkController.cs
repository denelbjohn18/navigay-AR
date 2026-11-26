using System.Collections;
using System.IO;
using Navigay.AR;
using Navigay.Config;
using Navigay.Data;
using Navigay.Dev;
using Navigay.Guidance;
using Navigay.Position;
using Navigay.Routing;
using UnityEngine;
using UnityEngine.UI;

namespace Navigay.UI
{
    public sealed class WalkController : MonoBehaviour
    {
        GraphData _graph;
        PlaceRecord _place;
        PhoneSensors _sensors;
        WhereAmI _where;
        LaneSnap _lanes;
        ReadyGate _gate;
        Confidence _confidence;
        WalkRecorder _recorder;
        ReadyScreen _ready;
        GuideScreen _guide;
        ArrivalScreen _arrival;
        PermissionsScreen _permissions;
        Image _background;
        GameObject _list;
        PlaceCard _card;
        NavConfig _config;
        Guide _guideLogic;
        DotTrail _dots;
        bool _walking;
        bool _preciseOn;
        bool _shownGuide;

        public void Bind(
            GraphData graph,
            PhoneSensors sensors,
            WhereAmI where,
            ReadyScreen ready,
            GuideScreen guide,
            ArrivalScreen arrival,
            PermissionsScreen permissions,
            Image background,
            GameObject list,
            PlaceCard card,
            NavConfig config)
        {
            _graph = graph;
            _sensors = sensors;
            _where = where;
            _ready = ready;
            _guide = guide;
            _arrival = arrival;
            _permissions = permissions;
            _background = background;
            _list = list;
            _card = card;
            _config = config;
            _lanes = new LaneSnap();
            _gate = new ReadyGate();
            _confidence = new Confidence();
            _guideLogic = new Guide();
            _dots = gameObject.AddComponent<DotTrail>();
            _sensors.Bind(_where);
            _ready.Closed += Back;
            _guide.Ended += EndWalk;
            _arrival.Closed += EndWalk;
        }

        public void Go(PlaceRecord place)
        {
            _place = place;
            _where.Bind(_graph);
            _lanes.Clear();
            _gate.Begin();
            _confidence = new Confidence();
            _recorder = new WalkRecorder(Path.Combine(Application.persistentDataPath, "walk.jsonl"));
            _sensors.StartWalk();
            _list.SetActive(false);
            _card.Hide();
            _background.color = Color.clear;
            _ready.Show(new ReadyView(ReadyWord.FindingYou, null, false, false, null));
            _guide.Hide();
            _arrival.Hide();
            _shownGuide = false;
            _walking = true;
            StartCoroutine(AskCamera());
        }

        public void Back()
        {
            if (_place == null)
            {
                return;
            }

            var minutes = MinutesLine(_gate.Route);
            StopWalk();
            _list.SetActive(true);
            _card.Show(_place, minutes);
        }

        public void EndWalk()
        {
            StopWalk();
            _card.Hide();
            _list.SetActive(true);
        }

        void StopWalk()
        {
            _walking = false;
            _preciseOn = false;
            _gate.End();
            _sensors.StopWalk();
            _ready.Hide();
            _guide.Hide();
            _arrival.Hide();
            if (_dots != null)
            {
                _dots.HideAll();
            }

            _background.color = Color.black;
        }

        void Update()
        {
            if (!_walking || _place == null)
            {
                return;
            }

            var time = Time.realtimeSinceStartupAsDouble;
            _sensors.Read(time, _config);
            var estimate = _where.Current;
            var inside = LaneMath.Containing(estimate.MapPosition.x, estimate.MapPosition.y, _graph.Blocks);
            var snap = _lanes.Update(
                estimate.MapPosition.x,
                estimate.MapPosition.y,
                estimate.HeadingDeg,
                estimate.HeadingErrorDeg,
                estimate.PositionErrorM,
                _graph,
                _gate.Route,
                _config,
                time);
            var fix = new FixSample(_where.HasFix, estimate.PositionErrorM, inside != null, inside == null ? null : inside.Name, snap);
            var view = _gate.Sample(_graph, _place.PlaceId, fix, _config, time, Calculate);
            ShowPrecise(view.PreciseLocation);
            var aligned = _where.TryGetMapToAr(out var mapToAr);
            _confidence.Update(estimate, _sensors.CameraFine && aligned, false, _config, time);
            if (view.Guiding)
            {
                ShowGuide(view, snap, estimate, inside != null, mapToAr, time);
            }
            else
            {
                _guide.Hide();
                _arrival.Hide();
                if (_dots != null)
                {
                    _dots.HideAll();
                }

                _ready.Show(view);
            }

            _recorder.Capture(LogSample(time, estimate, snap, view.Route));
        }

        void ShowGuide(ReadyView view, SnapFix snap, PositionEstimate estimate, bool inside, Matrix4x4 mapToAr, double time)
        {
            if (!_shownGuide)
            {
                _guideLogic.Open(view.Route, _config);
                _shownGuide = true;
            }

            _ready.Hide();
            var state = _guideLogic.Update(
                _graph,
                _place.PlaceId,
                snap,
                estimate,
                inside,
                _sensors.CameraFine,
                _confidence.Level,
                _config,
                time,
                Calculate);
            if (_guideLogic.ConsumeVibrate())
            {
                Handheld.Vibrate();
            }

            if (state.Arrived)
            {
                _guide.Hide();
                _dots.HideAll();
                var walked = Mathf.Max(0, (int)Mathf.Round((float)state.AlongM));
                _arrival.Show(_place, _guideLogic.ElapsedMinutes(time), walked);
                return;
            }

            _arrival.Hide();
            _guide.Draw(state, estimate.HeadingDeg);
            _dots.Draw(state, mapToAr, _config);
        }

        string MinutesLine(Route route)
        {
            if (route == null)
            {
                return PlaceCard.TimeUntilStuck;
            }

            var minutes = Pace.Minutes(route.LengthM, _config.walk_speed_mps);
            return "About " + minutes + " min";
        }

        void ShowPrecise(bool precise)
        {
            if (precise)
            {
                _permissions.Show(PermissionsScreen.PreciseSentence);
                _preciseOn = true;
                return;
            }

            if (!_preciseOn)
            {
                return;
            }

            _preciseOn = false;
            _permissions.Hide();
        }

        WalkSample LogSample(double time, PositionEstimate estimate, SnapFix snap, Route route)
        {
            double? along = null;
            if (route?.Polyline != null && snap.Locked)
            {
                along = LaneMath.Along(route.Polyline, snap.X, snap.Y);
            }

            return new WalkSample
            {
                Time = time,
                HasGps = _sensors.HasGps,
                Latitude = _sensors.Latitude,
                Longitude = _sensors.Longitude,
                GpsAccuracyM = _sensors.GpsAccuracyM,
                HasCompass = _sensors.HasCompass,
                HeadingDeg = estimate.HeadingDeg,
                HeadingAccuracyDeg = estimate.HeadingErrorDeg,
                ArX = _sensors.ArPosition.x,
                ArY = _sensors.ArPosition.y,
                ArZ = _sensors.ArPosition.z,
                ArQx = _sensors.ArRotation.x,
                ArQy = _sensors.ArRotation.y,
                ArQz = _sensors.ArRotation.z,
                ArQw = _sensors.ArRotation.w,
                Tracking = _sensors.Tracking.ToString(),
                HasSnap = snap.Locked,
                SnapX = snap.X,
                SnapY = snap.Y,
                SnapEdgeId = snap.EdgeId,
                AlongM = along,
                CountdownM = _shownGuide ? _guideLogic.Current.ShownCountdownM : (int?)null,
                Confidence = _confidence.Level,
            };
        }

        RouteResult Calculate(GraphData graph, string placeId, SnapFix snap)
        {
            var split = RouteStart.WithStart(graph, snap, out var startId);
            return Router.Calculate(split, startId, placeId, _config);
        }

        IEnumerator AskCamera()
        {
            while (_walking && !_sensors.CameraPermissionGranted && !_sensors.CameraDenied)
            {
                yield return null;
            }

            if (!_walking || !_sensors.CameraDenied)
            {
                yield break;
            }

            Back();
            _permissions.Show(PermissionsScreen.CameraSentence);
        }
    }
}
