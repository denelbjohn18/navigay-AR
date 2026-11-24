using System;
using Navigay.Config;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARKit;

namespace Navigay.Position
{
    public sealed class PhoneSensors : MonoBehaviour
    {
        WhereAmI _where;
        ARCameraManager _manager;
        GameObject _root;
        Camera _camera;
        Camera _hidden;
        float _brightness = 1f;
        bool _haveBrightness;
        bool _started;
        bool _watchingDenial;

        public bool Started => _started;

        public Vector3 ArPosition { get; private set; }

        public Quaternion ArRotation { get; private set; } = Quaternion.identity;

        public TrackingState Tracking { get; private set; }

        public bool CameraFine { get; private set; }

        public bool CameraPermissionGranted =>
            (_manager != null && _manager.permissionGranted)
            || ARSession.state == ARSessionState.SessionInitializing
            || ARSession.state == ARSessionState.SessionTracking;

        public bool CameraDenied { get; private set; }

        public bool HasGps { get; private set; }

        public double Latitude { get; private set; }

        public double Longitude { get; private set; }

        public float GpsAccuracyM { get; private set; }

        public bool HasCompass { get; private set; }

        public void Bind(WhereAmI where)
        {
            _where = where;
        }

        public void StartWalk()
        {
            _started = true;
            CameraDenied = false;
            Input.compass.enabled = true;
            EnsureAr();
            _root.SetActive(true);
            if (Camera.main != null && Camera.main != _camera)
            {
                _hidden = Camera.main;
                _hidden.enabled = false;
            }
        }

        public void StopWalk()
        {
            _started = false;
            if (_root != null)
            {
                _root.SetActive(false);
            }

            if (_hidden != null)
            {
                _hidden.enabled = true;
            }
        }

        public void Read(double time, NavConfig config)
        {
            if (!_started || _where == null)
            {
                return;
            }

            ReadGps(time);
            ReadCompass();
            ReadAr(time);
            WatchCameraDenial();
            CameraFine = Tracking == TrackingState.Tracking && !Lowered(config) && !Dark(config);
        }

        void ReadGps(double time)
        {
            if (Input.location.status != LocationServiceStatus.Running)
            {
                HasGps = false;
                return;
            }

            var data = Input.location.lastData;
            if (data.horizontalAccuracy < 0f)
            {
                HasGps = false;
                return;
            }

            HasGps = true;
            Latitude = data.latitude;
            Longitude = data.longitude;
            GpsAccuracyM = data.horizontalAccuracy;
            _where.PushGps(data.latitude, data.longitude, data.horizontalAccuracy, time);
        }

        void ReadCompass()
        {
            if (!Input.compass.enabled)
            {
                HasCompass = false;
                return;
            }

            HasCompass = true;
            _where.PushCompass(Input.compass.trueHeading, Input.compass.headingAccuracy);
        }

        void ReadAr(double time)
        {
            var position = Vector3.zero;
            var rotation = Quaternion.identity;
            var hmd = InputSystem.GetDevice<XRHMD>();
            if (hmd != null)
            {
                position = hmd.devicePosition.ReadValue();
                rotation = hmd.deviceRotation.ReadValue();
            }
            else if (_camera != null)
            {
                position = _camera.transform.position;
                rotation = _camera.transform.rotation;
            }

            ArPosition = position;
            ArRotation = rotation;
            Tracking = Map(ARSession.state);
            _where.PushAr(position, rotation, Tracking, time);
        }

        bool Lowered(NavConfig config)
        {
            var forward = ArRotation * Vector3.forward;
            var flat = new Vector3(forward.x, 0f, forward.z);
            if (flat.sqrMagnitude < 1e-6f)
            {
                return forward.y < 0f;
            }

            return forward.y < 0f && Vector3.Angle(flat, forward) > config.lowered_deg;
        }

        bool Dark(NavConfig config)
        {
            return _haveBrightness && _brightness < config.dark_brightness;
        }

        void EnsureAr()
        {
            if (_root != null)
            {
                return;
            }

            _root = new GameObject("ar");
            var sessionObject = new GameObject("session");
            sessionObject.transform.SetParent(_root.transform, false);
            sessionObject.AddComponent<ARSession>();
            var originObject = new GameObject("origin");
            originObject.transform.SetParent(_root.transform, false);
            var origin = originObject.AddComponent<XROrigin>();
            var cameraObject = new GameObject("camera");
            cameraObject.transform.SetParent(originObject.transform, false);
            _camera = cameraObject.AddComponent<Camera>();
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.black;
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = 40f;
            _manager = cameraObject.AddComponent<ARCameraManager>();
            _manager.requestedLightEstimation = LightEstimation.AmbientIntensity;
            cameraObject.AddComponent<ARCameraBackground>();
            origin.Camera = _camera;
            var driver = cameraObject.AddComponent<TrackedPoseDriver>();
            driver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            driver.positionInput = new InputActionProperty(new InputAction("Position", InputActionType.Value, "<XRHMD>/devicePosition"));
            driver.rotationInput = new InputActionProperty(new InputAction("Rotation", InputActionType.Value, "<XRHMD>/deviceRotation"));
            _manager.frameReceived += OnFrame;
            WatchCameraDenial();
        }

        void WatchCameraDenial()
        {
            if (_watchingDenial || _root == null)
            {
                return;
            }

            var session = _root.GetComponentInChildren<ARSession>();
            if (session != null && session.subsystem is ARKitSessionSubsystem kit)
            {
                kit.sessionDelegate = new CameraDenialWatch(this);
                _watchingDenial = true;
            }
        }

        sealed class CameraDenialWatch : DefaultARKitSessionDelegate
        {
            readonly PhoneSensors _owner;

            public CameraDenialWatch(PhoneSensors owner)
            {
                _owner = owner;
            }

            protected override void OnSessionDidFailWithError(ARKitSessionSubsystem sessionSubsystem, NSError error)
            {
                if (error.ToErrorDomain() == NSErrorDomain.ARKit && error.AsARKitErrorCode() == ARKitErrorCode.CameraUnauthorized)
                {
                    _owner.CameraDenied = true;
                }

                base.OnSessionDidFailWithError(sessionSubsystem, error);
            }
        }

        void OnFrame(ARCameraFrameEventArgs args)
        {
            if (args.lightEstimation.averageBrightness.HasValue)
            {
                _brightness = args.lightEstimation.averageBrightness.Value;
                _haveBrightness = true;
            }
        }

        static TrackingState Map(ARSessionState state)
        {
            switch (state)
            {
                case ARSessionState.SessionTracking:
                    return TrackingState.Tracking;
                case ARSessionState.SessionInitializing:
                    return TrackingState.Limited;
                case ARSessionState.None:
                case ARSessionState.Unsupported:
                case ARSessionState.CheckingAvailability:
                case ARSessionState.NeedsInstall:
                case ARSessionState.Installing:
                case ARSessionState.Ready:
                    return TrackingState.None;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }
    }
}
