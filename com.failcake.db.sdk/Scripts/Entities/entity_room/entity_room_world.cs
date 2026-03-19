#region

using System.Collections.Generic;
using SaintsField;
using SaintsField.Playa;
using UnityEngine;

#endregion

namespace HyenaQuest
{
    public class entity_room_world : MonoBehaviour
    {
        [LayoutStart("Settings", ELayout.Background | ELayout.TitleOut), Required]
        public List<GameObject> backgrounds;

        public Vector3 velocity;

        [Required]
        public MeshFilter boundsFilter;

        public void Awake() {
            if (this.backgrounds is not { Count: > 0 }) throw new UnityException("Missing backgrounds");
            if (!this.boundsFilter) throw new UnityException("Missing boundsFilter");
            if (this.backgrounds.Count < 2) throw new UnityException("Not enough backgrounds assigned to entity_world_background, need at least 2");
        }

        public void Update() {
            if (!this.boundsFilter || this.backgrounds == null || this.backgrounds.Count == 0) return;
            if (this.velocity == Vector3.zero) return;

            Bounds bounds = this.boundsFilter.sharedMesh.bounds;
            foreach (GameObject background in this.backgrounds)
            {
                if (!background) continue;

                Vector3 pos = background.transform.position;
                pos += this.velocity * Time.deltaTime;

                if (pos.x <= this.transform.position.x - bounds.size.x) pos.x += bounds.size.x * this.backgrounds.Count;
                if (pos.y <= this.transform.position.y - bounds.size.y) pos.y += bounds.size.y * this.backgrounds.Count;
                if (pos.z <= this.transform.position.z - bounds.size.z) pos.z += bounds.size.z * this.backgrounds.Count;

                background.transform.position = pos;
            }
        }

        #region PRIVATE

        #if UNITY_EDITOR
        public void OnDrawGizmos() {
            if (!this.boundsFilter || this.backgrounds == null || this.backgrounds.Count == 0) return;
            Bounds bounds = this.boundsFilter.sharedMesh.bounds;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(this.transform.position + bounds.center, bounds.size);
        }
        #endif

        #endregion
    }
}