#region

using SaintsField;
using SaintsField.Playa;
using Unity.Netcode;
using UnityEngine;

#endregion

namespace HyenaQuest
{
    [DisallowMultipleComponent]
    public class entity_network_template_base : MonoBehaviour
    {
        [LayoutStart("Settings", ELayout.Background | ELayout.TitleOut), Required, PlayaInfoBox("Ensure template is a NetworkBehaviour", EMessageType.Warning)]
        public GameObject template;

        public bool flipTest; // TEMP FOR NOW
        
        #region PRIVATE

        protected NetworkObject _spawnedGameObject;
        private bool IsServer => NetworkManager.Singleton && NetworkManager.Singleton.IsServer;

        #if UNITY_EDITOR
        private MeshFilter _filter;
        private SkinnedMeshRenderer _skinnedFilter;
        
        private entity_sdk_replacement _scrapSDK;
        #endif

        #endregion

        public void Awake() {
            if (!this.template) throw new UnityException("No templates assigned to entity_network_template!");
        }

        public void OnDestroy() {
            if (!this.IsServer) return;
            if (this._spawnedGameObject && this._spawnedGameObject.IsSpawned) this._spawnedGameObject.Despawn();
        }

        [Server]
        public (GameObject, NetworkObject) NetworkSpawn() {
            if (!this.IsServer) return (null, null);
            if (!this.template) throw new UnityException("No templates assigned to entity_network_template!");

            if (SDK.PreNetworkTemplateSpawn != null) this.template = SDK.PreNetworkTemplateSpawn?.Invoke(this.template);

            GameObject newObj = entity_network_template_base.Instantiate(this.template, this.transform.position, this.transform.rotation);
            if (!newObj) throw new UnityException($"Failed to instantiate template {this.template.name}");

            SDK.PostNetworkTemplateSpawn?.Invoke(newObj);

            // TEMP ----------------
            if (this.flipTest)
            {
                entity_room_interior test = this.GetComponentInParent<entity_room_interior>(false);
                if (test && test.IsRoomFlipped())
                {
                    newObj.transform.localEulerAngles = new Vector3(newObj.transform.localEulerAngles.x,180F, newObj.transform.localEulerAngles.z);
                }
            }
            // --------------------

            newObj.transform.localScale = Vector3.Scale(this.transform.lossyScale, newObj.transform.lossyScale);

            NetworkObject networkObj = newObj.GetComponent<NetworkObject>();
            if (!networkObj) throw new UnityException($"NetworkObject component missing on template {newObj.name}");

            this._spawnedGameObject = networkObj;
            return (newObj, this._spawnedGameObject);
        }

        public virtual bool CanSpawn() { return false; }

        #region PRIVATE
        
        #if UNITY_EDITOR
        public void OnDrawGizmos() {
            if (Application.isPlaying) return;
            Gizmos.DrawIcon(this.transform.position, "ent_network_spawner");

            GameObject preview = this.template;
            if (!preview) return;

            // MODEL PREVIEW -----
            Mesh meshToDraw = null;
            Transform meshTransform = null;

            if (this.template.name.StartsWith("SDK-"))
            {
                this._scrapSDK = preview.GetComponent<entity_sdk_replacement>();
                if (this._scrapSDK)
                {
                    meshToDraw = this._scrapSDK.preview;
                    meshTransform = this._scrapSDK.transform;
                }
            }
            else
            {
                this._filter = preview.GetComponent<MeshFilter>();
                if (!this._filter) this._filter = preview.GetComponentInChildren<MeshFilter>(true);

                if (!this._filter)
                {
                    this._skinnedFilter = preview.GetComponent<SkinnedMeshRenderer>();
                    if (!this._skinnedFilter) this._skinnedFilter = preview.GetComponentInChildren<SkinnedMeshRenderer>(true);
                    if (!this._skinnedFilter) return;
                }

                if (!this._filter && !this._skinnedFilter)
                {
                    MeshCollider cl = preview.GetComponentInChildren<MeshCollider>(true);
                    if (!cl) return;
                    
                    meshToDraw = cl.sharedMesh;
                    meshTransform = cl.transform;
                }
                else if (this._filter)
                {
                    meshToDraw = this._filter.sharedMesh;
                    meshTransform = this._filter.transform;
                }
                else if (this._skinnedFilter)
                {
                    meshToDraw = this._skinnedFilter.sharedMesh;
                    meshTransform = this._skinnedFilter.transform;
                }
            }
            
            if (!meshToDraw || !meshTransform) return;

            Vector3 scale = Vector3.Scale(this.transform.lossyScale, this.template.transform.lossyScale);

            Gizmos.color = this.flipTest ? new Color(1f, 0f, 0f, 0.3f) : new Color(1f, 1f, 0f, 0.3f);
            Gizmos.matrix = Matrix4x4.TRS(this.transform.position, this.transform.rotation * preview.transform.rotation, scale);
            Gizmos.DrawMesh(meshToDraw);
        }
        #endif

        #endregion
    }
}