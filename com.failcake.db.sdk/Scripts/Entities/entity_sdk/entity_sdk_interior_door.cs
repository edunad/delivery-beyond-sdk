#region

using System.Collections.Generic;
using SaintsField.Playa;
using UnityEngine;

#endregion

namespace HyenaQuest
{
    /// <summary>
    ///     PROXY to entity_door_phys
    /// </summary>
    [DisallowMultipleComponent]
    public class entity_sdk_interior_door : MonoBehaviour
    {
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
            SDK_SETUP.PatchSDKEntity?.Invoke(this.gameObject);
        }

        #if UNITY_EDITOR
        public void OnValidate() {
            if (Application.isPlaying) return;

            string newName = "entity_sdk_interior_door";
            if (this.name == newName) return;
            this.name = newName;
        }
        #endif
    }
}