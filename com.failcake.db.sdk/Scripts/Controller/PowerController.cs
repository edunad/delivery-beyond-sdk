#region

using System;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;

#endregion

namespace HyenaQuest
{
    [DisallowMultipleComponent, DefaultExecutionOrder((int)ScriptOrder.CONTROLLER - 1), RequireComponent(typeof(NetworkObject))]
    public class PowerController : NetController<PowerController>
    {
        #region EVENTS

        public GameEvent<PowerGrid, bool, bool> OnGridUpdate = new GameEvent<PowerGrid, bool, bool>();
        public GameEvent OnGridWarning = new GameEvent();

        #endregion

        #region PRIVATE

        private bool _flickerWarning;

        #region NET

        private readonly NetVar<bool> _basePowered = new NetVar<bool>(true);
        private readonly NetVar<bool> _mapPowered = new NetVar<bool>(true);

        #endregion

        #endregion

        #region HOOKS

        protected override void OnNetworkPostSpawn() {
            base.OnNetworkPostSpawn();
            if (!this.IsClient) return;

            // HOOKS ---
            this._basePowered.RegisterOnValueChanged((oldValue, newValue) => {
                if (oldValue == newValue) return;
                this.OnGridUpdate.Invoke(PowerGrid.BASE, newValue, false);
            });


            this._mapPowered.RegisterOnValueChanged((oldValue, newValue) => {
                if (oldValue == newValue) return;
                this.OnGridUpdate.Invoke(PowerGrid.MAP, newValue, false);
            });

            // ---------
        }

        public override void OnNetworkPreDespawn() {
            base.OnNetworkPreDespawn();
            if (!this.IsClient) return;

            // HOOKS ---
            this._basePowered.OnValueChanged = null;
            this._mapPowered.OnValueChanged = null;
            // ---------
        }

        #endregion

        #region POWER

        [Server]
        public bool IsAreaPowered(PowerGrid grid) {
            if (grid == PowerGrid.UNCONTROLLED) throw new ArgumentException("Invalid grid");
            return grid == PowerGrid.BASE ? this._basePowered.Value : this._mapPowered.Value;
        }

        #endregion

        #region PRIVATE

        [Server]
        public void SetPoweredArea(PowerGrid grid, bool hasPower) {
            switch (grid)
            {
                case PowerGrid.UNCONTROLLED:
                    throw new ArgumentException("Invalid grid");
                case PowerGrid.BASE:
                    this._basePowered.Value = hasPower;
                    break;
                default:
                    this._mapPowered.Value = hasPower;
                    break;
            }

            if (!hasPower && grid == PowerGrid.MAP) SDK.Play2DSound?.Invoke("Ingame/Cycle/power_off.ogg", new AudioData { volume = 0.1f }, true);
            this.OnGridUpdate.Invoke(grid, hasPower, true);
        }

        [Server]
        public void SetPoweredArea(bool hasPower) {
            foreach (PowerGrid grid in Enum.GetValues(typeof(PowerGrid)))
            {
                if (grid == PowerGrid.UNCONTROLLED) continue;
                this.SetPoweredArea(grid, hasPower);
            }
        }

        #if UNITY_EDITOR
        public void OnValidate() {
            if (Application.isPlaying) return;

            NetworkObject net = this.NetworkObject;
            if (!net) return;

            // NETWORK --
            PropertyInfo ownershipProperty = typeof(NetworkObject).GetProperty("Ownership", BindingFlags.NonPublic | BindingFlags.Instance);
            if (ownershipProperty != null) ownershipProperty.SetValue(net, NetworkObject.OwnershipStatus.SessionOwner);

            net.SynchronizeTransform = false;
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