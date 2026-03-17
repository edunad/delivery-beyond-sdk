#region

using System.Collections.Generic;
using SaintsField.Playa;
using UnityEngine;

#endregion

namespace HyenaQuest
{
    [RequireComponent(typeof(Collider)), RequireComponent(typeof(Rigidbody))]
    public class entity_trigger_collider : MonoBehaviour
    {
        #region PUBLIC

        [LayoutStart("Settings", ELayout.Background | ELayout.TitleOut)]
        public LayerMask filters;

        public bool isEnabled = true;

        #region EVENTS

        public GameEvent<Collision> OnEnter = new GameEvent<Collision>();
        public GameEvent<Collision> OnExit = new GameEvent<Collision>();

        #endregion

        #endregion

        #region PRIVATE

        private readonly List<Collision> _colliders = new List<Collision>();

        private Collider _collision;
        private Rigidbody _body;

        #endregion

        public void Awake() {
            this._body = this.GetComponent<Rigidbody>();
            if (!this._body) throw new UnityException("entity_trigger_collider requires Rigidbody component");

            this._collision = this.GetComponent<Collider>();
            if (!this._collision) throw new UnityException("entity_trigger_collider requires Collider component");

            this.SetFilters(this.filters);
        }

        public void SetEnabled(bool enable) {
            this.isEnabled = enable;
        }

        public void SetFilters(LayerMask filter) {
            if (!this._collision || !this._body) return;

            this._collision.includeLayers = ~0; // Everything
            this._collision.excludeLayers = ~filter.value; // Everything except filter
            this._collision.isTrigger = false;


            this._body.includeLayers = ~0; // Everything
            this._body.excludeLayers = ~filter.value; // Everything except filter
            this._body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            this._body.isKinematic = true;
            this._body.useGravity = false;
            this._body.constraints = RigidbodyConstraints.FreezeRotation;

            this.filters = filter;
        }

        public void OnCollisionStay(Collision col) {
            if (!this.isEnabled || this._colliders.Contains(col)) return;
            this._colliders.Add(col);

            this.OnEnter?.Invoke(col);
        }

        public void OnCollisionExit(Collision col) {
            if (!this._colliders.Contains(col)) return;
            this._colliders?.Remove(col);

            this.OnExit?.Invoke(col);
        }

        public List<Collision> GetColliders() {
            return this._colliders;
        }

        #if UNITY_EDITOR
        public void OnValidate() {
            if (Application.isPlaying) return;

            if (!this._collision) this._collision = this.GetComponent<Collider>();
            if (!this._body) this._body = this.GetComponent<Rigidbody>();

            this.SetFilters(this.filters);
        }
        #endif
    }
}