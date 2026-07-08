#region

using System;
using SaintsField;
using SaintsField.Playa;
using UnityEngine;
using UnityEngine.Scripting;

#endregion

namespace HyenaQuest
{
    [Serializable]
    public enum ACCESSORY_TYPE
    {
        HAT = 0,
        GOOGLES,
        NECK,
        CHEST,
        PANTS,
        TAIL,
        MASK
    }

    [Preserve, Serializable, CreateAssetMenu(menuName = "HyenaQuest SDK/Accessory")]
    public class PlayerAccessory : ScriptableObject
    {
        [LayoutStart("Settings", ELayout.Background | ELayout.TitleOut)]
        public ACCESSORY_TYPE type;

        [LayoutStart("Settings/Achievement", ELayout.Background | ELayout.TitleOut)]
        public STEAM_ACHIEVEMENTS achievement;

        [LayoutStart("Settings/Rendering", ELayout.Background | ELayout.TitleOut), ShowIf(nameof(PlayerAccessory.__CAN_HIDE_GOOGLES__))]
        #if UNITY_EDITOR
        #endif
        public bool hideGoogles;

        #if UNITY_EDITOR
        [ShowIf(nameof(PlayerAccessory.__CAN_HIDE_HAIR__))]
        #endif
        public bool hideHair;

        #if UNITY_EDITOR
        [ShowIf(nameof(PlayerAccessory.__CAN_HIDE_HAT__))]
        #endif
        public bool hideHat;

        [LayoutStart("Model", ELayout.Background | ELayout.TitleOut), Required, Tooltip("Make sure it's the model's object, not the mesh!")]
        public GameObject obj;

        [Required]
        public Sprite preview;

        #if UNITY_EDITOR
        private bool __CAN_HIDE_HAT__ => this.type is ACCESSORY_TYPE.MASK;
        private bool __CAN_HIDE_GOOGLES__ => this.type is ACCESSORY_TYPE.HAT or ACCESSORY_TYPE.MASK;
        private bool __CAN_HIDE_HAIR__ => this.type is ACCESSORY_TYPE.HAT or ACCESSORY_TYPE.MASK;
        #endif
    }
}