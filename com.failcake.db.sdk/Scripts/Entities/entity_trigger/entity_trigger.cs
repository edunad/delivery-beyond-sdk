#region

using SaintsField.Playa;
using UnityEngine;

#endregion

namespace HyenaQuest
{
    [RequireComponent(typeof(Collider))]
    public class entity_trigger : MonoBehaviour
    {
        #region PUBLIC

        [LayoutStart("Settings", ELayout.Background | ELayout.TitleOut)]
        public LayerMask filters;

        public bool triggerOnce;

        public LayerMask LODMask = 0;

        #region EVENTS

        public GameEvent<Collider> OnEnter = new GameEvent<Collider>();
        public GameEvent<Collider> OnStay = new GameEvent<Collider>();
        public GameEvent<Collider> OnExit = new GameEvent<Collider>();

        #endregion

        #endregion

        #region PRIVATE

        protected Collider _trigger;

        #endregion

        public void Awake() {
            this._trigger = this.GetComponent<Collider>();
            if (!this._trigger) this._trigger = this.GetComponentInChildren<Collider>(true);
            if (!this._trigger) throw new UnityException("entity_trigger requires Collider component");

            this.SetFilters(this.filters);
        }

        public void OnTriggerEnter(Collider col) {
            if (this.OnEnter == null || !this.isActiveAndEnabled) return;
            if (!this.CheckLOD(col)) return;

            if (this.triggerOnce) this.enabled = false;
            this.OnEnter.Invoke(col);
        }

        public void OnTriggerStay(Collider col) {
            if (this.OnEnter == null || !this.isActiveAndEnabled) return;
            if (!this.CheckLOD(col)) return;

            this.OnStay.Invoke(col);
        }

        public void OnTriggerExit(Collider col) {
            if (this.OnExit == null || !this.isActiveAndEnabled) return;
            if (!this.CheckLOD(col)) return;

            this.OnExit.Invoke(col);
        }

        public Bounds GetBounds() {
            if (!this._trigger) return new Bounds();

            Bounds bounds = this._trigger.bounds;
            if (this._trigger is SphereCollider sphereCollider) bounds.size = Vector3.one * sphereCollider.radius * 2f;

            return bounds;
        }

        #region PRIVATE

        private void SetFilters(LayerMask filter) {
            this._trigger.includeLayers = ~0; // Everything
            this._trigger.excludeLayers = ~filter.value; // Everything except filter
            this._trigger.isTrigger = true;

            this.filters = filter;
        }

        private bool CheckLOD(Collider col) {
            if (this.LODMask == 0) return true;

            Vector3 startPoint = this._trigger.transform.position;
            Vector3 targetPoint = col.ClosestPoint(startPoint);

            Vector3 direction = targetPoint - startPoint;
            float distance = direction.magnitude;
            direction.Normalize();

            Vector3 boxHalfExtents = new Vector3(0.1f, 0.1f, 0.1f);
            return !Physics.BoxCast(
                startPoint,
                boxHalfExtents,
                direction,
                out RaycastHit boxHit,
                Quaternion.LookRotation(direction),
                distance, this.LODMask,
                QueryTriggerInteraction.Ignore
            ) || boxHit.collider == col;
        }

        #if UNITY_EDITOR
        public void OnValidate() {
            if (Application.isPlaying) return;
            this.gameObject.layer = LayerMask.NameToLayer("entity_trigger");

            if (!this._trigger) this._trigger = this.GetComponent<Collider>();
            if (!this._trigger) this._trigger = this.GetComponentInChildren<Collider>(true);
            if (!this._trigger) return;

            this._trigger.isTrigger = true;
            this.SetFilters(this.filters);
        }

        public void OnDrawGizmosSelected() {
            if (!this._trigger) this._trigger = this.GetComponent<Collider>();
            if (!this._trigger) return;

            util_gizmos.DrawCollider(this._trigger, new Color(1f, 0.5f, 0, 0.02f));
        }
        #endif

        #endregion
    }
}