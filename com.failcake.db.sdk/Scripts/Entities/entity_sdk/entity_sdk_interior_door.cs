#region

using System.Collections.Generic;
using System.Reflection;
using SaintsField;
using SaintsField.Playa;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

#endregion

namespace HyenaQuest
{
    /// <summary>
    ///     PROXY to entity_door_phys
    /// </summary>
    [DisallowMultipleComponent, RequireComponent(typeof(NetworkObject), typeof(NetworkTransform)), RequireComponent(typeof(NetworkRigidbody), typeof(Rigidbody))]
    public class entity_sdk_interior_door : NetworkBehaviour
    {
        [InfoBox("This entity template should be placed inside a entity_network_spawn")]
        [LayoutStart("Door"), LayoutStart("Door/Layers", ELayout.Background | ELayout.TitleOut)]
        public List<GameObject> layers = new List<GameObject>();

        [LayoutStart("Trap", ELayout.Background | ELayout.TitleOut)]
        public GameObject trap;

        [LayoutStart("Sounds", ELayout.Background | ELayout.TitleOut)]
        public SoundTypes collisionMaterial = SoundTypes.CUSTOM;

        [ShowIf(nameof(entity_sdk_interior_door.UseCustomSounds))]
        public List<AudioClip> collideSounds = new List<AudioClip>();

        [ShowIf(nameof(entity_sdk_interior_door.UseCustomSounds))]
        public List<AudioClip> damageSounds = new List<AudioClip>();

        #region PRIVATE

        private bool UseCustomSounds => this.collisionMaterial == SoundTypes.CUSTOM;

        #endregion

        public void Awake() {
            SDK_SETUP.PatchSDKEntity?.Invoke(this.gameObject); // Run it for clients
        }

        #region PRIVATE
        #if UNITY_EDITOR
         /// <summary>
        /// USED TO MIMIC entity_door
        /// </summary>
        
        private NetworkTransform _networkTransform;
        private Rigidbody _rigidbody;
        
        public void OnValidate() {
            if (Application.isPlaying) return;
            
            string newName = "entity_sdk_interior_door";
            if (this.name == newName) return;
            this.name = newName;

            // NETWORK --
            NetworkObject net = this.NetworkObject;
            if (net)
            {
                FieldInfo ownershipField = typeof(NetworkObject).GetField("Ownership", BindingFlags.NonPublic | BindingFlags.Instance);
                if (ownershipField != null) ownershipField.SetValue(net, NetworkObject.OwnershipStatus.Transferable);

                net.SynchronizeTransform = true;
                net.SceneMigrationSynchronization = false;
                net.ActiveSceneSynchronization = false;
                net.SpawnWithObservers = true;
                net.DontDestroyWithOwner = true;

                net.AllowOwnerToParent = true;
                net.AutoObjectParentSync = true;
                net.SyncOwnerTransformWhenParented = true;
            }

            // ----------

            if (!this._networkTransform) this._networkTransform = this.GetComponent<NetworkTransform>();
            if (this._networkTransform)
            {
                this._networkTransform.SyncScaleX = false;
                this._networkTransform.SyncScaleY = false;
                this._networkTransform.SyncScaleZ = false;

                this._networkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
                this._networkTransform.UseUnreliableDeltas = false;
                this._networkTransform.UseHalfFloatPrecision = true;

                this._networkTransform.Interpolate = true;
                this._networkTransform.PositionThreshold = 0.001F;
                this._networkTransform.PositionInterpolationType = NetworkTransform.InterpolationTypes.SmoothDampening;
                this._networkTransform.PositionMaxInterpolationTime = 0.55F;

                this._networkTransform.RotationInterpolationType = NetworkTransform.InterpolationTypes.SmoothDampening;
                this._networkTransform.RotationMaxInterpolationTime = 0.35F;
            }

            if (!this._rigidbody) this._rigidbody = this.GetComponent<Rigidbody>();
            if (this._rigidbody)
            {
                //this._rigidbody.interpolation = RigidbodyInterpolation.Interpolate; // Crashes editor
                this._rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

                this._rigidbody.maxLinearVelocity = 10;
                this._rigidbody.maxAngularVelocity = 10;
                this._rigidbody.useGravity = true;
                this._rigidbody.mass = 10;
                this._rigidbody.linearDamping = 1;
                this._rigidbody.angularDamping = 0.25F;
            }
        }
        #endif
        #endregion
    }
}