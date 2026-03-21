#region

using SaintsField;
using SaintsField.Playa;
using UnityEditor;
using UnityEngine;

#endregion

namespace HyenaQuest
{
    /// <summary>
    ///     Mimics A* Pathfinding project link2
    /// </summary>
    public class entity_sdk_nav_link : MonoBehaviour
    {
        [LayoutStart("Link", ELayout.Background | ELayout.TitleOut), Required]
        public Transform target;

        [LayoutStart("Settings", ELayout.Background | ELayout.TitleOut), Range(0, 1)]
        public float jumpTime = 0.5F;

        public float jumpOffset = 0.5F;
        public float jumpDelay = 0.25F;

        public bool effect = true;

        public void Awake() {
            SDK.PatchSDKEntity?.Invoke(this.gameObject);
        }

        #if UNITY_EDITOR
        public void OnValidate() {
            if (Application.isPlaying) return;

            string newName = "entity_sdk_nav_link";
            if (this.name == newName) return;
            this.name = newName;
        }

        public void OnDrawGizmos() {
            Vector3 start = this.transform.position;

            Gizmos.color = Color.blueViolet;
            Gizmos.matrix = Matrix4x4.TRS(start, this.transform.rotation, this.transform.localScale);
            Gizmos.DrawCube(Vector3.zero, new Vector3(1, 0.001F, 1));
            Gizmos.matrix = Matrix4x4.identity;

            if (!this.target) return;
            Vector3 end = this.target.position;

            Gizmos.color = new Color(0.54F, 0.17F, 0.89F, 0.5F);
            Gizmos.matrix = Matrix4x4.TRS(end, this.target.rotation, this.target.localScale);
            Gizmos.DrawCube(Vector3.zero, new Vector3(1, 0.001F, 1));
            Gizmos.matrix = Matrix4x4.identity;

            Vector3 p0 = start;
            Vector3 p1 = start + Vector3.up * this.jumpOffset;
            Vector3 p2 = end + Vector3.up * this.jumpOffset;
            Vector3 p3 = end;

            const int segments = 32;
            Gizmos.color = Color.blueViolet;
            Vector3 prev = p0;
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                float u = 1f - t;
                Vector3 point = u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3;
                Gizmos.DrawLine(prev, point);
                prev = point;
            }

            float midT = 0.5f;
            float midU = 0.5f;

            Vector3 midPoint = midU * midU * midU * p0 + 3f * midU * midU * midT * p1 + 3f * midU * midT * midT * p2 + midT * midT * midT * p3;
            Vector3 tangent = (3f * midU * midU * (p1 - p0) + 6f * midU * midT * (p2 - p1) + 3f * midT * midT * (p3 - p2)).normalized;

            Gizmos.DrawRay(midPoint, tangent * 0.3f);
            Gizmos.DrawRay(midPoint + tangent * 0.3f, Quaternion.LookRotation(tangent) * new Vector3(-0.08f, 0, -0.15f));
            Gizmos.DrawRay(midPoint + tangent * 0.3f, Quaternion.LookRotation(tangent) * new Vector3(0.08f, 0, -0.15f));

            Gizmos.color = new Color(1f, 1f, 1f, 0.15f);

            Gizmos.DrawLine(start, p1);
            Gizmos.DrawLine(end, p2);
            Gizmos.DrawSphere(p1, 0.05f);
            Gizmos.DrawSphere(p2, 0.05f);
        }

        public void OnDrawGizmosSelected() {
            if (!this.target) return;
            Handles.color = new Color(0.54F, 0.17F, 0.89F, 0.25F);

            Vector3 start = this.transform.position;
            Vector3 end = this.target.position;
            Vector3 p1 = start + Vector3.up * this.jumpOffset;
            Vector3 p2 = end + Vector3.up * this.jumpOffset;

            Handles.DrawBezier(start, end, p1, p2, Color.blueViolet, null, 3f);

            if (this.jumpDelay > 0)
            {
                Handles.color = new Color(1f, 0.6f, 0f, 0.3f);
                Handles.DrawSolidDisc(start + Vector3.up * 0.01f, Vector3.up, this.jumpDelay);
            }

            Vector3 mid = 0.125f * start + 0.375f * p1 + 0.375f * p2 + 0.125f * end;
            Handles.Label(mid + Vector3.up * 0.15f, $"t:{this.jumpTime}s  d:{this.jumpDelay}s  h:{this.jumpOffset}");
        }
        #endif
    }
}