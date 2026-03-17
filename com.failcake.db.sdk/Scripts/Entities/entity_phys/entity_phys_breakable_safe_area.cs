#region

using System.Collections.Generic;
using UnityEngine;

#endregion

namespace HyenaQuest
{
    [RequireComponent(typeof(Collider))]
    public class entity_phys_breakable_safe_area : MonoBehaviour
    {
        public static HashSet<entity_phys_breakable_safe_area> NO_BREAK_AREAS = new HashSet<entity_phys_breakable_safe_area>();

        #region PRIVATE

        private Collider _collider;

        #endregion

        #region CLEANUP - DOMAIN RELOAD

        #if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod]
        private static void InitializeOnLoad() {
            entity_phys_breakable_safe_area.NO_BREAK_AREAS.Clear();
        }
        #endif

        #endregion

        public void Awake() {
            if (!this._collider) this._collider = this.gameObject.GetComponent<Collider>();
            if (!this._collider) this._collider = this.gameObject.GetComponentInChildren<Collider>();
            if (!this._collider) throw new UnityException("Missing collider");

            entity_phys_breakable_safe_area.NO_BREAK_AREAS.Add(this);
        }

        public void OnDestroy() {
            entity_phys_breakable_safe_area.NO_BREAK_AREAS.Remove(this);
        }

        public bool IsInside(Vector3 pos, Bounds bounds) {
            return this._collider.bounds.Contains(pos) || this._collider.bounds.Intersects(bounds);
        }

        #if UNITY_EDITOR
        public void OnValidate() {
            if (Application.isPlaying) return;

            if (!this._collider) this._collider = this.gameObject.GetComponent<Collider>();
            if (!this._collider) this._collider = this.gameObject.GetComponentInChildren<Collider>();
            if (!this._collider) return;

            this.gameObject.layer = LayerMask.NameToLayer("entity_trigger");
            this._collider.isTrigger = true;
        }

        public void OnDrawGizmos() {
            if (!this._collider) this._collider = this.gameObject.GetComponent<Collider>();
            if (!this._collider) this._collider = this.gameObject.GetComponentInChildren<Collider>();
            if (!this._collider) return;

            util_gizmos.DrawCollider(this._collider, new Color(0.8F, 0.5F, 0, 0.2F));
        }
        #endif
    }
}