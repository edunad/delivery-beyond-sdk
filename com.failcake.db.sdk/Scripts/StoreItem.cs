#region

using System;
using System.Collections.Generic;
using SaintsField;
using SaintsField.Playa;
using UnityEngine;
using UnityEngine.Scripting;

#endregion

namespace HyenaQuest
{
    [Preserve, Serializable]
    public struct StoreItemLimit
    {
        public string itemID;

        [Range(0, 5)]
        public byte limit;
    }

    [Preserve, Serializable, CreateAssetMenu(menuName = "HyenaQuest SDK/Store Item Settings")]
    public class StoreItem : ScriptableObject
    {
        [LayoutStart("Settings", ELayout.Background | ELayout.TitleOut)]
        public string itemName;

        [Range(0, 500)]
        public int itemPrice;

        [Required]
        public GameObject itemPrefab;

        [Range(1, 100)]
        public int minRounds = 1;

        [Range(1, 20)]
        public int minPlayers = 1;

        [Range(0, 1)]
        public float priority;

        public StoreItemLimit limit;

        [LayoutStart("Rendering", ELayout.Background | ELayout.TitleOut)]
        public List<Sprite> itemSprites = new List<Sprite>();
    }
}