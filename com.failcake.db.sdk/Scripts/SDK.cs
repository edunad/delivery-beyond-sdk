using System;
using UnityEngine;

namespace HyenaQuest
{
    /// <summary>
    /// THESE ARE USED BY THE GAME TO TRANSMIT DATA TO THE SDK WITHOUT DEPENDENCIES
    /// DO NOT TOUCH THIS, YOU MIGHT BREAK THE GAME.
    /// </summary>
    
    public static class SDK
    {
        #region CLEANUP - DOMAIN RELOAD

        #if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod]
        private static void InitializeOnLoad() {
            SDK.GetCurrentRound = null;
            
            SDK.PreNetworkTemplateSpawn = null;
            SDK.PostNetworkTemplateSpawn = null;
            
            SDK.GetSeed = null;
            SDK.OnRoomSpawn = null;
            
            SDK.PatchSDKEntity = null;
            
            SDK.OnDeliverySpotRegister = null;
            SDK.OnDeliverySpotUnregister = null;
            
            SDK.OnKillRequest = null;
            SDK.OnDamageRequest = null;
            
            SDK.Play3DSound = null;
            SDK.Play3DSoundClip = null;
            SDK.Play2DSoundClip = null;
            SDK.Play2DSound = null;
        }
        #endif

        #endregion
        
        private static Camera _mainCamera;
        public static Camera MainCamera {
            get {
                if (!SDK._mainCamera) SDK._mainCamera = Camera.main;
                return SDK._mainCamera;
            }
        }
        
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
        
        // DAMAGE ---
        public static Action<DamageType, Collider> OnKillRequest;
        public static Action<DamageType, byte, float, bool, Collider> OnDamageRequest;
        // ---------
            
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
