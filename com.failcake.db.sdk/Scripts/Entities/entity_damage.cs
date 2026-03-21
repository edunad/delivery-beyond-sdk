#region

using System.Collections.Generic;
using System.Linq;
using SaintsField.Playa;
using UnityEngine;

#endregion

namespace HyenaQuest
{
    public class entity_damage : entity_trigger
    {
        #region PUBLIC

        [LayoutStart("Settings", ELayout.Background | ELayout.TitleOut), LayoutStart("Settings/Damage", ELayout.Background | ELayout.TitleOut), Range(0, 100)]
        public byte damage = 1;

        [Range(0, 10)]
        public float damageCooldown = 0.4F;

        public DamageType damageType = DamageType.GENERIC;
        public bool damageOnMove;

        #endregion

        public new void Awake() {
            base.Awake();

            // EVENTS ----------
            this.OnStay += this.Damage;
            // -----------------
        }

        public void OnDestroy() {
            // EVENTS -------------
            this.OnStay -= this.Damage;
            // --------------------
        }

        #region PRIVATE
        private void Damage(Collider col) {
            if (!this.gameObject.activeInHierarchy) return;
            if (col.transform.IsChildOf(this.transform) || col.gameObject == this.gameObject) return;

            SDK.OnDamageRequest?.Invoke(this.damageType, this.damage, this.damageCooldown, this.damageOnMove, col);
        }

        #endregion

        #if UNITY_EDITOR
        public void OnDrawGizmos() {
            Gizmos.DrawIcon(this.transform.position, "bullseye");
        }
        #endif
    }
}