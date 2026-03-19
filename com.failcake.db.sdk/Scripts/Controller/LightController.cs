#region

using System;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;

#endregion

namespace HyenaQuest
{
    [DisallowMultipleComponent, DefaultExecutionOrder((int)ScriptOrder.OTHER_CONTROLLERS), RequireComponent(typeof(NetworkObject))]
    public class LightController : NetController<LightController>
    {
        #region EVENTS

        public GameEvent<PowerGrid, LightCommand, bool> OnLightAreaCommand = new GameEvent<PowerGrid, LightCommand, bool>();

        #endregion

        [Server]
        public void ExecuteAllLightCommand(LightCommand command) {
            foreach (PowerGrid area in Enum.GetValues(typeof(PowerGrid))) this.OnLightAreaCommand?.Invoke(area, command, true);
            this.ExecuteAllLightCommandRPC(command);
        }

        public override void OnNetworkSpawn() {
            // EVENTS ---
            CoreController.WaitFor<PowerController>(powerCtrl => {
                powerCtrl.OnGridUpdate += this.OnGridUpdate;
                powerCtrl.OnGridWarning += this.GridWarning;
            });
            // ---------
        }

        public override void OnNetworkDespawn() {
            if (!PowerController.Instance) return;

            // EVENTS ---
            PowerController.Instance.OnGridUpdate -= this.OnGridUpdate;
            PowerController.Instance.OnGridWarning -= this.GridWarning;
            // ---------
        }

        #region PRIVATE

        [Rpc(SendTo.ClientsAndHost)]
        private void ExecuteAllLightCommandRPC(LightCommand command) {
            foreach (PowerGrid area in Enum.GetValues(typeof(PowerGrid))) this.OnLightAreaCommand?.Invoke(area, command, false);
        }


        [Client]
        private void GridWarning() {
            foreach (PowerGrid area in Enum.GetValues(typeof(PowerGrid))) this.OnLightAreaCommand?.Invoke(area, LightCommand.FLICKER, false);
        }

        private void OnGridUpdate(PowerGrid area, bool on, bool server) {
            this.OnLightAreaCommand?.Invoke(area, on ? LightCommand.ON : area == PowerGrid.BASE ? LightCommand.OFF : LightCommand.FLICKER_OFF, server);
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