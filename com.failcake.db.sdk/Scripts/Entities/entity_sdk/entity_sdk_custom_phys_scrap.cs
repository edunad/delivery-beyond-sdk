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
    ///     PROXY to entity_phys_scrap
    /// </summary>
    [DisallowMultipleComponent]
    public class entity_sdk_custom_phys_scrap : MonoBehaviour
    {
        [LayoutStart("Sounds", ELayout.Background | ELayout.TitleOut)]
        public SoundTypes collisionMaterial = SoundTypes.CUSTOM;

        [ShowIf(nameof(entity_sdk_custom_phys_scrap.UseCustomSounds))]
        public List<AudioClip> collideSounds = new List<AudioClip>();

        [LayoutStart("Settings/Scrap", ELayout.Background | ELayout.TitleOut), Range(1, 100)]
        public int scrap = 10;

        [Required]
        public GameObject viewModel;

        #region PRIVATE

        private bool UseCustomSounds => this.collisionMaterial == SoundTypes.CUSTOM;

        #if UNITY_EDITOR
        public void OnValidate() {
            if (Application.isPlaying) return;
            
            string newName = "entity_sdk_custom_phys_scrap";
            if (this.name == newName) return;
            this.name = newName;
        }
        #endif

        #endregion
    }
}