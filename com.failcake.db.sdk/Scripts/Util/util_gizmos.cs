#region

using UnityEngine;

#endregion

namespace HyenaQuest
{
    public static class util_gizmos
    {
        #if UNITY_EDITOR
        public static void DrawCollider(Collider collider, Color gizmoColor) {
            if (!collider) return;

            Bounds bTrigger = collider.bounds;
            Matrix4x4 originalMatrix = Gizmos.matrix;

            Gizmos.matrix = Matrix4x4.TRS(
                collider.transform.position,
                collider.transform.rotation,
                collider.transform.lossyScale
            );

            Gizmos.color = gizmoColor;

            if (collider is SphereCollider sphereCollider)
                Gizmos.DrawSphere(sphereCollider.center, sphereCollider.radius);
            else if (collider is BoxCollider boxCollider)
                Gizmos.DrawCube(boxCollider.center, boxCollider.size);
            else if (collider is CapsuleCollider capsuleCollider)
            {
                Vector3 center = capsuleCollider.center;
                float radius = capsuleCollider.radius;
                float cylinderHalf = Mathf.Max(0f, capsuleCollider.height / 2f - radius);

                Vector3 axis = capsuleCollider.direction switch {
                    0 => Vector3.right,
                    2 => Vector3.forward,
                    _ => Vector3.up
                };

                Vector3 cap0 = center + axis * cylinderHalf;
                Vector3 cap1 = center - axis * cylinderHalf;

                Gizmos.DrawWireSphere(cap0, radius);
                Gizmos.DrawWireSphere(cap1, radius);

                Vector3 perp0 = capsuleCollider.direction == 0 ? Vector3.up : Vector3.right;
                Vector3 perp1 = Vector3.Cross(axis, perp0).normalized;

                Gizmos.DrawLine(cap0 + perp0 * radius, cap1 + perp0 * radius);
                Gizmos.DrawLine(cap0 - perp0 * radius, cap1 - perp0 * radius);
                Gizmos.DrawLine(cap0 + perp1 * radius, cap1 + perp1 * radius);
                Gizmos.DrawLine(cap0 - perp1 * radius, cap1 - perp1 * radius);
            }
            else if (collider is MeshCollider meshCollider && meshCollider.sharedMesh)
            {
                Mesh mesh = meshCollider.sharedMesh;
                if (mesh.vertices.Length <= 0 || mesh.normals.Length <= 0) return;

                Gizmos.DrawWireMesh(mesh, mesh.bounds.center, meshCollider.transform.localRotation, Vector3.one);
            }
            else
            {
                Vector3 localCenter = collider.transform.InverseTransformPoint(bTrigger.center);
                Vector3 localSize = Vector3.Scale(bTrigger.size, new Vector3(
                    1f / collider.transform.lossyScale.x,
                    1f / collider.transform.lossyScale.y,
                    1f / collider.transform.lossyScale.z
                ));

                Gizmos.DrawCube(localCenter, localSize);
            }

            Gizmos.matrix = originalMatrix;
        }
        #endif
    }
}