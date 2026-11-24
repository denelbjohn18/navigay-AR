using Navigay.Data;
using UnityEngine;

namespace Navigay.Position
{
    public sealed class WhereAmI
    {
        const float GpsBlendMetres = 25f;
        const float MinGpsBlend = 0.05f;
        const float UnfixedErrorM = 1000f;

        MapFrame _frame;
        bool _haveFrame;
        bool _haveGps;
        float _gpsError = UnfixedErrorM;
        Vector3 _lastAr;
        bool _haveAr;
        float _yaw;
        bool _haveYaw;
        float _heading;
        float _headingError = 180f;
        Vector2 _map;
        bool _haveMap;

        public bool HasFix => _haveGps;

        public PositionEstimate Current { get; private set; }

        public void Bind(GraphData graph)
        {
            _frame = MapFrame.From(graph);
            _haveFrame = true;
        }

        public void PushGps(double latitude, double longitude, float accuracyM, double time)
        {
            if (!_haveFrame || accuracyM < 0f)
            {
                return;
            }

            _frame.ToMap(latitude, longitude, out var x, out var y);
            var gps = new Vector2((float)x, (float)y);
            if (!_haveMap)
            {
                _map = gps;
            }
            else
            {
                var weight = Mathf.Clamp(GpsBlendMetres / Mathf.Max(accuracyM, 1f), MinGpsBlend, 1f);
                _map = Vector2.Lerp(_map, gps, weight);
            }

            _gpsError = accuracyM;
            _haveGps = true;
            _haveMap = true;
            Publish(time, Current.Tracking);
        }

        public void PushCompass(float trueHeading, float headingAccuracyDeg)
        {
            _heading = trueHeading;
            _headingError = headingAccuracyDeg;
            _haveYaw = false;
        }

        public void PushAr(Vector3 worldPosition, Quaternion rotation, TrackingState tracking, double time)
        {
            var forward = rotation * Vector3.forward;
            var flat = new Vector2(forward.x, forward.z);
            if (flat.sqrMagnitude > 1e-6f && _headingError < 180f)
            {
                var arHeading = Mathf.Atan2(flat.x, flat.y) * Mathf.Rad2Deg;
                _yaw = _heading - arHeading;
                _haveYaw = true;
            }

            if (_haveAr && _haveMap && _haveYaw)
            {
                var delta = worldPosition - _lastAr;
                var rad = _yaw * Mathf.Deg2Rad;
                var cos = Mathf.Cos(rad);
                var sin = Mathf.Sin(rad);
                _map.x += (delta.x * cos) + (delta.z * sin);
                _map.y += (-delta.x * sin) + (delta.z * cos);
            }

            _lastAr = worldPosition;
            _haveAr = true;
            Publish(time, tracking);
        }

        void Publish(double time, TrackingState tracking)
        {
            Current = new PositionEstimate(
                _haveMap ? _map : Vector2.zero,
                _heading,
                _haveGps ? _gpsError : UnfixedErrorM,
                _headingError,
                PositionSource.Gps,
                tracking,
                time);
        }
    }
}
