#region

using UnityEngine;
using UnityEngine.Scripting;

#endregion

namespace HyenaQuest
{
    [Preserve]
    public static class util_bounds
    {
        public static Bounds GetWorldBounds(Bounds bounds, Transform world) {
            if (!world) return bounds;

            Vector3[] corners = util_bounds.GetLocalCorners(bounds);

            Vector3 min = Vector3.positiveInfinity;
            Vector3 max = Vector3.negativeInfinity;

            foreach (Vector3 corner in corners)
            {
                Vector3 worldCorner = world.TransformPoint(corner);
                min = Vector3.Min(min, worldCorner);
                max = Vector3.Max(max, worldCorner);
            }

            return new Bounds((min + max) * 0.5f, max - min);
        }

        private static Vector3[] GetLocalCorners(Bounds bounds) {
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;

            return new[] {
                center + new Vector3(-extents.x, -extents.y, -extents.z),
                center + new Vector3(extents.x, -extents.y, -extents.z),
                center + new Vector3(-extents.x, extents.y, -extents.z),
                center + new Vector3(extents.x, extents.y, -extents.z),
                center + new Vector3(-extents.x, -extents.y, extents.z),
                center + new Vector3(extents.x, -extents.y, extents.z),
                center + new Vector3(-extents.x, extents.y, extents.z),
                center + new Vector3(extents.x, extents.y, extents.z)
            };
        }
    }
}