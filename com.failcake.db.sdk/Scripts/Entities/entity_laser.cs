#region

using SaintsField;
using SaintsField.Playa;
using UnityEngine;

#endregion

namespace HyenaQuest
{
    [RequireComponent(typeof(LineRenderer)), ExecuteInEditMode]
    public class entity_laser : MonoBehaviour
    {
        [LayoutStart("Settings", ELayout.Background | ELayout.TitleOut), Required]
        public GameObject hitEnd;

        #region PRIVATE

        private LineRenderer _lineRenderer;
        private int _layerMask;

        #endregion

        public void Awake() {
            if (!this.hitEnd) throw new UnityException("entity_laser requires a hitEnd GameObject to work.");

            this._lineRenderer = this.GetComponent<LineRenderer>();
            if (!this._lineRenderer) throw new UnityException("entity_laser requires a LineRenderer component to work.");

            this._lineRenderer.tag = "OCCLUDER/IGNORE";
            
            this._lineRenderer.useWorldSpace = true;
            this._lineRenderer.positionCount = 2;
            
            this._layerMask = LayerMask.GetMask("entity_phys", "entity_ground", "entity_player", "entity_enemy", "entity_phys_item");
        }
        
        public void Update() {
            if (!this._lineRenderer || !this.hitEnd) return;
            
            bool isActive = (SDK_SETUP.GetCurrentRound?.Invoke() ?? 1) >= 2;
            
            this._lineRenderer.enabled = isActive;
            this.hitEnd.SetActive(isActive);
            
            if (!isActive) return;
            
            Vector3 start = this.transform.position;
            Vector3 end = start + this.transform.forward * 1000f;

            if (Physics.Linecast(start, end, out RaycastHit hit, this._layerMask))
            {
                end = hit.point;

                if (hit.rigidbody && hit.collider.gameObject.CompareTag("Player"))
                    SDK_SETUP.OnKillRequest?.Invoke(DamageType.ELECTRIC_ASHES, hit.collider);
            }
            
            this._lineRenderer.SetPosition(0, start);
            this._lineRenderer.SetPosition(1, end);

            this.hitEnd.transform.position = end + Vector3.left * 0.01F;
        }

        #region PRIVATE

        private void OnRoundUpdate(byte round, bool server) {
            bool isActive = round >= 2;

            this._lineRenderer.enabled = isActive;
            this.hitEnd.SetActive(isActive);
        }

        #endregion
    }
}