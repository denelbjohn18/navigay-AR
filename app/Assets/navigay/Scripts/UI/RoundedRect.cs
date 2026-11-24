using UnityEngine;
using UnityEngine.UI;

namespace Navigay.UI
{
    public sealed class RoundedRect : Image
    {
        public float cornerRadius = 14f;
        public bool topOnly;

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            var rect = GetPixelAdjustedRect();
            var limit = Mathf.Min(rect.width, rect.height) * 0.5f;
            var radius = Mathf.Clamp(cornerRadius, 0f, limit);
            var bottom = topOnly ? 0f : radius;
            if (radius <= 0.5f && bottom <= 0.5f)
            {
                AddQuad(helper, rect.xMin, rect.yMin, rect.xMax, rect.yMax);
                return;
            }

            var innerLeft = rect.xMin + radius;
            var innerRight = rect.xMax - radius;
            var innerBottom = rect.yMin + bottom;
            var innerTop = rect.yMax - radius;
            AddQuad(helper, innerLeft, innerBottom, innerRight, innerTop);
            AddQuad(helper, innerLeft, rect.yMin, innerRight, innerBottom);
            AddQuad(helper, innerLeft, innerTop, innerRight, rect.yMax);
            AddQuad(helper, rect.xMin, innerBottom, innerLeft, innerTop);
            AddQuad(helper, innerRight, innerBottom, rect.xMax, innerTop);
            AddCorner(helper, new Vector2(innerLeft, innerTop), radius, 90f, 180f);
            AddCorner(helper, new Vector2(innerRight, innerTop), radius, 0f, 90f);
            if (bottom > 0.5f)
            {
                AddCorner(helper, new Vector2(innerLeft, innerBottom), bottom, 180f, 270f);
                AddCorner(helper, new Vector2(innerRight, innerBottom), bottom, 270f, 360f);
            }
            else
            {
                AddQuad(helper, rect.xMin, rect.yMin, innerLeft, innerBottom);
                AddQuad(helper, innerRight, rect.yMin, rect.xMax, innerBottom);
            }
        }

        void AddQuad(VertexHelper helper, float xMin, float yMin, float xMax, float yMax)
        {
            if (xMax <= xMin || yMax <= yMin)
            {
                return;
            }

            var index = helper.currentVertCount;
            var tint = color;
            helper.AddVert(new Vector3(xMin, yMin), tint, Vector2.zero);
            helper.AddVert(new Vector3(xMin, yMax), tint, Vector2.zero);
            helper.AddVert(new Vector3(xMax, yMax), tint, Vector2.zero);
            helper.AddVert(new Vector3(xMax, yMin), tint, Vector2.zero);
            helper.AddTriangle(index, index + 1, index + 2);
            helper.AddTriangle(index + 2, index + 3, index);
        }

        void AddCorner(VertexHelper helper, Vector2 center, float radius, float startDegrees, float endDegrees)
        {
            const int segments = 6;
            var index = helper.currentVertCount;
            var tint = color;
            helper.AddVert(center, tint, Vector2.zero);
            for (var step = 0; step <= segments; step++)
            {
                var degrees = Mathf.Lerp(startDegrees, endDegrees, step / (float)segments) * Mathf.Deg2Rad;
                var point = center + new Vector2(Mathf.Cos(degrees), Mathf.Sin(degrees)) * radius;
                helper.AddVert(point, tint, Vector2.zero);
            }

            for (var step = 0; step < segments; step++)
            {
                helper.AddTriangle(index, index + step + 1, index + step + 2);
            }
        }
    }
}
