using UnityEngine;
using UnityEngine.UI;

namespace Navigay.UI
{
    public sealed class RoundedRect : Image
    {
        static Material _material;

        public float cornerRadius = 14f;
        public bool topOnly;

        public override Material defaultMaterial => _material ??= new Material(Resources.Load<Shader>("NavigayRoundedRect"))
        {
            hideFlags = HideFlags.HideAndDontSave,
        };

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            var rect = GetPixelAdjustedRect();
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            var half = rect.size * 0.5f;
            var radius = Mathf.Clamp(cornerRadius, 0f, Mathf.Min(half.x, half.y));
            var shape = new Vector4(half.x, half.y, radius, topOnly ? 0f : radius);
            // Leave room for the shader's edge coverage outside the rect.
            var padding = 1f / Mathf.Max(canvas != null ? canvas.scaleFactor : 1f, 0.001f);
            var x = half.x + padding;
            var y = half.y + padding;
            var vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.uv1 = shape;
            AddVertex(helper, vertex, rect.center, -x, -y);
            AddVertex(helper, vertex, rect.center, -x, y);
            AddVertex(helper, vertex, rect.center, x, y);
            AddVertex(helper, vertex, rect.center, x, -y);
            helper.AddTriangle(0, 1, 2);
            helper.AddTriangle(2, 3, 0);
        }

        static void AddVertex(VertexHelper helper, UIVertex vertex, Vector2 center, float x, float y)
        {
            vertex.position = new Vector3(center.x + x, center.y + y, 0f);
            vertex.uv0 = new Vector4(x, y, 0f, 0f);
            helper.AddVert(vertex);
        }
    }
}
