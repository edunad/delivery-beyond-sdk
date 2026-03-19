using System;
using UnityEngine;

namespace HyenaQuest
{
    /// <summary>
    /// THESE ARE USED BY THE GAME TO TRANSMIT DATA TO THE SDK WITHOUT DEPENDENCIES
    /// DO NOT TOUCH THIS, YOU MIGHT BREAK THE GAME.
    /// </summary>
    
    public static class SDK_SETUP
    {
        #region CLEANUP - DOMAIN RELOAD

        #if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod]
        private static void InitializeOnLoad() {
            SDK_SETUP.GetCurrentRound = null;
            
            SDK_SETUP.PreNetworkTemplateSpawn = null;
            SDK_SETUP.PostNetworkTemplateSpawn = null;
            
            SDK_SETUP.GetSeed = null;
            SDK_SETUP.OnRoomSpawn = null;
            
            SDK_SETUP.PatchSDKEntity = null;
            
            SDK_SETUP.OnDeliverySpotRegister = null;
            SDK_SETUP.OnDeliverySpotUnregister = null;
            
            SDK_SETUP.Play3DSound = null;
            SDK_SETUP.Play3DSoundClip = null;
            SDK_SETUP.Play2DSoundClip = null;
            SDK_SETUP.Play2DSound = null;
        }
        #endif

        #endregion
        
        // IngameController ---
        public static Func<byte> GetCurrentRound;
        // ---------------------------
        
        // entity_network_template_base ---
        public static Func<GameObject, GameObject> PreNetworkTemplateSpawn;
        public static Action<GameObject> PostNetworkTemplateSpawn;
        // ---------------------------
        
        // Other ---
        public static Action<GameObject> PatchSDKEntity;
        // ---------------------------
        
        // MapController ---
        public static Func<int> GetSeed;
        public static Action<entity_room_base> OnRoomSpawn;
        // ---------------------------
        
        // SoundController ---
        public static Action<string, Vector3, AudioData, bool> Play3DSound;
        public static Action<AudioClip, Vector3, AudioData, bool> Play3DSoundClip;
        public static Action<AudioClip, AudioData, bool> Play2DSoundClip;
        public static Action<string, AudioData, bool> Play2DSound;
        // ---------------------------
        
        // DeliveryController ---
        public static Action<entity_delivery_spot> OnDeliverySpotRegister;
        public static Action<entity_delivery_spot> OnDeliverySpotUnregister;
        // ---------------------------
    }
}
