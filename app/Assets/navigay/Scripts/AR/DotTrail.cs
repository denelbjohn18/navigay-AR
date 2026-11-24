using Navigay.Config;
using Navigay.Guidance;
using Navigay.Routing;
using UnityEngine;

namespace Navigay.AR
{
    public sealed class DotTrail : MonoBehaviour
    {
        static readonly Color Blue = new Color(10f / 255f, 132f / 255f, 255f / 255f, 1f);

        Transform[] _dots = System.Array.Empty<Transform>();
        Material _material;

        public void Draw(GuidanceState state, Vector3 cameraPosition, Quaternion cameraRotation, NavConfig config)
        {
            if (state.Level == ConfidenceLevel.Hold || state.Arrived || state.Route?.Polyline == null)
            {
                HideAll();
                return;
            }

            var span = state.Level == ConfidenceLevel.Full ? config.full_dots_m : config.short_dots_m;
            var points = Sample(state.Route, state.AlongM, span, config);
            Ensure(points.Length);
            var groundY = cameraPosition.y - 1.5f;
            var yaw = Quaternion.Euler(0f, cameraRotation.eulerAngles.y, 0f);
            for (var i = 0; i < _dots.Length; i++)
            {
                if (i >= points.Length)
                {
                    _dots[i].gameObject.SetActive(false);
                    continue;
                }

                var t = points.Length == 1 ? 0f : i / (float)(points.Length - 1);
                var size = Mathf.Lerp(0.4f, 0.12f, t);
                var fade = Mathf.Lerp(1f, 0.25f, t);
                _dots[i].gameObject.SetActive(true);
                _dots[i].position = yaw * new Vector3((float)points[i][0], 0f, (float)points[i][1]) + new Vector3(cameraPosition.x, groundY, cameraPosition.z);
                _dots[i].localScale = Vector3.one * size;
                var renderer = _dots[i].GetComponent<MeshRenderer>();
                renderer.material.color = new Color(Blue.r, Blue.g, Blue.b, fade);
            }
        }

        public void HideAll()
        {
            foreach (var dot in _dots)
            {
                if (dot != null)
                {
                    dot.gameObject.SetActive(false);
                }
            }
        }

        void Ensure(int count)
        {
            if (_dots.Length >= count)
            {
                return;
            }

            var next = new Transform[count];
            for (var i = 0; i < _dots.Length; i++)
            {
                next[i] = _dots[i];
            }

            if (_material == null)
            {
                _material = new Material(Shader.Find("Sprites/Default"));
                _material.color = Blue;
            }

            for (var i = _dots.Length; i < count; i++)
            {
                var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = "dot";
                sphere.transform.SetParent(transform, false);
                UnityEngine.Object.Destroy(sphere.GetComponent<Collider>());
                var renderer = sphere.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = _material;
                next[i] = sphere.transform;
            }

            _dots = next;
        }

        static double[][] Sample(Route route, double along, float span, NavConfig config)
        {
            var line = route.Polyline;
            var points = new System.Collections.Generic.List<double[]>();
            var cursor = along + config.dot_spacing_m;
            var end = along + span;
            var checkpoint = route.Checkpoints != null && route.Checkpoints.Length > 0
                ? route.Checkpoints[System.Math.Min(route.Checkpoints.Length - 1, 0)]
                : null;
            if (route.Checkpoints != null)
            {
                foreach (var item in route.Checkpoints)
                {
                    if (item != null && item.AlongM >= along)
                    {
                        checkpoint = item;
                        break;
                    }
                }
            }

            if (checkpoint != null)
            {
                end = System.Math.Min(end, checkpoint.AlongM);
            }

            var travelled = 0d;
            for (var i = 0; i < line.Length - 1 && points.Count < config.max_dots; i++)
            {
                var ax = line[i][0];
                var ay = line[i][1];
                var bx = line[i + 1][0];
                var by = line[i + 1][1];
                var dx = bx - ax;
                var dy = by - ay;
                var length = System.Math.Sqrt((dx * dx) + (dy * dy));
                if (length < 1e-6)
                {
                    continue;
                }

                var next = travelled + length;
                while (cursor <= end && cursor <= next && points.Count < config.max_dots)
                {
                    var t = (cursor - travelled) / length;
                    points.Add(new[] { ax + (dx * t), ay + (dy * t) });
                    cursor += config.dot_spacing_m;
                }

                travelled = next;
            }

            return points.ToArray();
        }
    }
}
