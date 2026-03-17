#region

using SaintsField.Playa;
using UnityEngine;
using UnityEngine.Scripting;
#if UNITY_EDITOR
using UnityEditor;
#endif

#endregion

namespace HyenaQuest
{
    [Preserve]
    public class entity_room_exit : MonoBehaviour
    {
        [LayoutStart("Settings", ELayout.Background | ELayout.TitleOut)]
        public Vector3 direction;

        public string biomeID;

        public int order = -1;

        #region PRIVATE

        private entity_room _owner;

        #endregion

        public entity_room GetOwner() {
            if (!this._owner) this._owner = this.GetComponentInParent<entity_room>(true);
            return this._owner;
        }

        #if UNITY_EDITOR
        private void OnDrawGizmos() {
            if (Application.isPlaying) return;

            Handles.color = this is entity_interior_exit ? Color.orangeRed : Color.gray;
            Handles.ArrowHandleCap(0, this.transform.position, Quaternion.Euler(this.direction), 0.5F, EventType.Repaint);
            if (this.order != -1) Handles.Label(this.transform.position, this.order.ToString());
        }

        private void OnDrawGizmosSelected() {
            if (Application.isPlaying) return;

            Gizmos.color = Color.aquamarine;
            Gizmos.DrawWireCube(this.transform.position, Vector3.one * 0.25F);
        }

        #endif
    }
}