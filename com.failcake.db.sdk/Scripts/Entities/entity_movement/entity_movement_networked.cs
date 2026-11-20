#region

using System;
using System.Reflection;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

#endregion

namespace HyenaQuest
{
    [RequireComponent(typeof(NetworkObject))]
    public class entity_movement_networked : entity_movement
    {
        #region PRIVATE

        protected NetworkTransform _networkTransform;
        protected NetworkObject _networkObject;

        private bool IsServer => NetworkManager.Singleton && NetworkManager.Singleton.IsServer;

        #endregion

        public new void Awake() {
            if (!this.obj) throw new UnityException("Missing game object");
            if (this.points.Count < 2) throw new UnityException("At least 2 points are needed");

            this._networkTransform = this.obj.GetComponent<NetworkTransform>();
            if (!this._networkTransform) throw new UnityException("Missing NetworkTransform on target object");

            this._networkObject = this.GetComponent<NetworkObject>();
            if (!this._networkObject) throw new UnityException("Missing NetworkObject on target object");

            if (this.IsServer && this.startActive) this.StartMovement();
        }

        public new void Update() {
            if (!this.IsServer) return;
            base.Update();
        }

        [Server]
        public override void StartMovement(bool reset = true, Action onComplete = null) {
            if (!this.IsServer) throw new UnityException("Server only");
            base.StartMovement(reset, onComplete);
        }

        [Server]
        public override void StopMovement() {
            if (!this.IsServer) throw new UnityException("Server only");
            base.StopMovement();
        }

        #region PRIVATE

        [Server]
        protected override void ResetMovement() {
            if ((this._networkObject?.IsSpawned ?? false) && !this.IsServer) throw new UnityException("Server only");
            base.ResetMovement();
        }

        protected override void ForcePosition(Point point) {
            if (!this._networkTransform || !this._networkTransform.IsSpawned)
            {
                base.ForcePosition(point);
                return;
            }

            this._networkTransform.SetState(this.transform.TransformPoint(point.pos), this.transform.rotation * Quaternion.Euler(point.angle), this.obj.transform.localScale, false);
        }

        protected override void OnPointReached(Point dest) {
            if (!this.IsServer) return;
            if (!this._networkTransform || !this._networkTransform.IsSpawned) return;

            this._networkTransform.SetState(
                this.transform.TransformPoint(dest.pos),
                this.transform.rotation * Quaternion.Euler(dest.angle),
                this.obj.transform.localScale, false
            );
        }

        protected override bool ShouldBroadcastSound() { return true; }

        #if UNITY_EDITOR
        protected new void OnValidate() {
            if (Application.isPlaying) return;
            if (!this.obj) return;

            this.ResetMovement();

            NetworkObject net = this._networkObject;
            if (!net) net = this.GetComponent<NetworkObject>();
            if (!net) return;

            // NETWORK --
            FieldInfo ownershipField = typeof(NetworkObject).GetField("Ownership", BindingFlags.NonPublic | BindingFlags.Instance);
            if (ownershipField != null) ownershipField.SetValue(net, NetworkObject.OwnershipStatus.SessionOwner);

            net.SynchronizeTransform = true;
            net.ActiveSceneSynchronization = false;
            net.SpawnWithObservers = true;
            net.AllowOwnerToParent = false;
            net.AutoObjectParentSync = false;
            net.SyncOwnerTransformWhenParented = false;
            net.SceneMigrationSynchronization = false;
            net.DontDestroyWithOwner = false;
            net.DestroyWithScene = true;
            // -----------------
        }
        #endif

        #endregion
    }
}