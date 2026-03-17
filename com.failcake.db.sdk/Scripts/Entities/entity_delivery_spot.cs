#region

using System.Reflection;
using SaintsField.Playa;
using TMPro;
using Unity.Netcode;
using UnityEngine;

#endregion

namespace HyenaQuest
{
    [RequireComponent(typeof(NetworkObject))]
    public class entity_delivery_spot : NetworkBehaviour
    {
        [LayoutStart("Settings", ELayout.Background | ELayout.TitleOut)]
        public entity_trigger trigger;

        #region PRIVATE

        private TextMeshPro _deliveryText;

        #region NET

        private readonly NetVar<int> _deliveryAddress = new NetVar<int>(-1);

        #endregion

        #endregion

        public void Awake() {
            if (!this.trigger) throw new UnityException("entity_delivery_spot requires Collider component");

            this._deliveryText = this.GetComponentInChildren<TextMeshPro>(true);
            if (!this._deliveryText) throw new UnityException("entity_delivery_spot requires TextMeshPro component");

            this._deliveryText.text = "";
        }

        #region HOOKS

        // https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/issues/3186
        protected override void OnNetworkPostSpawn() {
            base.OnNetworkPostSpawn();
            if (!this.IsClient) return;

            // HOOKS ---
            this._deliveryAddress.RegisterOnValueChanged((_, newValue) => { this._deliveryText.text = newValue == -1 ? "" : newValue.ToString(); });
            // ---------
        }

        public override void OnNetworkPreDespawn() {
            base.OnNetworkPreDespawn();
            if (!this.IsClient) return;

            // HOOKS ---
            this._deliveryAddress.OnValueChanged = null;
            // ---------
        }

        #endregion

        public override void OnNetworkSpawn() {
            SDK_SETUP.OnDeliverySpotRegister?.Invoke(this);
            if (!this.IsServer) return;

            // EVENTS ---
            this.trigger.OnEnter += this.OnEnter;
            this.trigger.OnExit += this.OnExit;
            //  ----
        }

        public override void OnNetworkDespawn() {
            SDK_SETUP.OnDeliverySpotUnregister?.Invoke(this);
            if (!this.IsServer) return;

            // EVENTS 
            if (this.trigger)
            {
                this.trigger.OnEnter -= this.OnEnter;
                this.trigger.OnExit -= this.OnExit;
            }
            //  ----
        }

        [Server]
        public void SetDeliveryAddress(int address) {
            if (this.IsSpawned && !this.IsServer) throw new UnityException("SetDeliveryAddress can only be called on the server");
            this._deliveryAddress.SetSpawnValue(address);
        }

        public int GetDeliveryAddress() {
            return this._deliveryAddress.Value;
        }

        #region PRIVATE

        private void OnEnter(Collider other) {
            if (!this.IsServer || !other) return;
            other.SendMessage("OnDeliverySpotEnter", this._deliveryAddress.Value, SendMessageOptions.DontRequireReceiver);
        }

        private void OnExit(Collider other) {
            if (!this.IsServer || !other) return;
            other.SendMessage("OnDeliverySpotExit", this._deliveryAddress.Value, SendMessageOptions.DontRequireReceiver);
        }

        #endregion

        #if UNITY_EDITOR
        public void OnDrawGizmos() {
            if (!this.trigger) return;
            Bounds bTrigger = this.trigger.GetBounds();

            Gizmos.matrix = Matrix4x4.TRS(bTrigger.center, this.transform.rotation, this.transform.lossyScale);
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(Vector3.zero, bTrigger.size);

            Gizmos.color = new Color(0, 1, 0, 0.5F);
            Gizmos.DrawCube(Vector3.zero, bTrigger.size);
        }

        private void OnValidate() {
            if (Application.isPlaying) return;

            NetworkObject net = this.NetworkObject;
            if (!net) return;

            PropertyInfo ownershipProperty = typeof(NetworkObject).GetProperty("Ownership", BindingFlags.NonPublic | BindingFlags.Instance);
            if (ownershipProperty != null) ownershipProperty.SetValue(net, NetworkObject.OwnershipStatus.SessionOwner);

            net.SynchronizeTransform = true;
            net.SpawnWithObservers = true;

            net.ActiveSceneSynchronization = false;
            net.SceneMigrationSynchronization = false;
            net.DontDestroyWithOwner = false;

            net.DestroyWithScene = true;

            net.SyncOwnerTransformWhenParented = true;
            net.AutoObjectParentSync = true;
        }
        #endif
    }
}