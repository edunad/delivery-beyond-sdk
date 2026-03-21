#region

using SaintsField.Playa;
using Unity.Netcode;
using UnityEngine;

#endregion

namespace HyenaQuest
{
    public class entity_kill : entity_trigger
    {
        #region PUBLIC

        [LayoutStart("Damage", ELayout.Background | ELayout.TitleOut)]
        public DamageType damageType = DamageType.GENERIC;
        #endregion
        
        public new void Awake() {
            base.Awake();

            // EVENTS ----------
            this.OnStay += this.Kill;
            // -----------------
        }

        public void OnDestroy() {
            // EVENTS -------------
            this.OnStay -= this.Kill;
            // --------------------
        }

        #region PRIVATE

        private void Kill(Collider col) {
            if (!this.gameObject.activeInHierarchy || !col || !col.gameObject) return;
            if (col.transform.IsChildOf(this.transform) || col.gameObject == this.gameObject) return;
            
            SDK.OnKillRequest?.Invoke(this.damageType, col);
        }

        #endregion

        #if UNITY_EDITOR
        public new void OnDrawGizmosSelected() {
            if (!this._trigger) this._trigger = this.GetComponent<Collider>();
            if (!this._trigger) return;

            Color color = Color.red;
            color.a = this.enabled ? 0.6F : 0.01F;

            util_gizmos.DrawCollider(this._trigger, color);
        }

        public void OnDrawGizmos() {
            Gizmos.DrawIcon(this.transform.position, "bullseye");
        }
        #endif
    }
}