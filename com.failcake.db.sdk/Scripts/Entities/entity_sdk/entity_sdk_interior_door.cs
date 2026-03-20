#region

using System.Collections.Generic;
using System.Reflection;
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
    [DisallowMultipleComponent, RequireComponent(typeof(NetworkObject))]
    public class entity_sdk_interior_door : NetworkBehaviour
    {
        [LayoutStart("Door"), LayoutStart("Door/Layers", ELayout.Background | ELayout.TitleOut)]
        public List<GameObject> layers = new List<GameObject>();

        [LayoutStart("Trap", ELayout.Background | ELayout.TitleOut)]
        public GameObject trap;

        [LayoutStart("Sounds", ELayout.Background | ELayout.TitleOut)]
        public SoundTypes collisionMaterial  = SoundTypes.CUSTOM;
        
        [ShowIf(nameof(UseCustomSounds))]
        public List<AudioClip> collideSounds = new List<AudioClip>();
        
        [ShowIf(nameof(UseCustomSounds))]
        public List<AudioClip> damageSounds = new List<AudioClip>();

        #region PRIVATE
        private bool UseCustomSounds => this.collisionMaterial == SoundTypes.CUSTOM;
        #endregion

        public void Awake() {
            SDK_SETUP.PatchSDKEntity?.Invoke(this.gameObject);
        }
        
        #if UNITY_EDITOR
        public void OnValidate() {
            if (Application.isPlaying) return;
            this.name = $"entity_sdk_interior_door";
        }
        #endif
    }
}