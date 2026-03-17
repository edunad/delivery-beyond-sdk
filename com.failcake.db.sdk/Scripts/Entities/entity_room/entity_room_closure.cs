#region

using System.Reflection;
using SaintsField.Playa;
using Unity.Netcode;
using UnityEngine;

#endregion

namespace HyenaQuest
{
    [RequireComponent(typeof(NetworkObject))]
    public class entity_room_closure : NetworkBehaviour
    {
        [LayoutStart("Settings", ELayout.Background | ELayout.TitleOut)]
        public string biomeID;

        #region PRIVATE

        #if UNITY_EDITOR
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

        #endregion
    }
}