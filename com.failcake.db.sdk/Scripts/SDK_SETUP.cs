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
            
            SDK_SETUP.NetworkObjectProxy = null;
            
            SDK_SETUP.GetSeed = null;
            SDK_SETUP.OnRoomSpawn = null;
            
            SDK_SETUP.OnDeliverySpotRegister = null;
            SDK_SETUP.OnDeliverySpotUnregister = null;
        }
        #endif

        #endregion
        
        // IngameController ---
        public static Func<byte> GetCurrentRound;
        // ---------------------------
        
        // SDKProxyController ---
        public static Func<GameObject, GameObject> NetworkObjectProxy;
        // ---------------------------
        
        // MapController ---
        public static Func<int> GetSeed;
        public static Action<entity_room_base> OnRoomSpawn;
        // ---------------------------
        
        // DeliveryController ---
        public static Action<entity_delivery_spot> OnDeliverySpotRegister;
        public static Action<entity_delivery_spot> OnDeliverySpotUnregister;
        // ---------------------------
    }
}
