#region

using System.Collections.Generic;
using SaintsField.Playa;
using UnityEngine;
using Random = System.Random;

#endregion

namespace HyenaQuest
{
    public class entity_room_window : MonoBehaviour
    {
        [LayoutStart("Settings", ELayout.Background | ELayout.TitleOut)]
        public List<GameObject> openLayers = new List<GameObject>();

        public List<GameObject> closedLayers = new List<GameObject>();

        public void SetStatus(string seed, bool forceClosed = false) {
            List<GameObject> layers = this.closedLayers;
            if (!forceClosed) layers.AddRange(this.openLayers);

            Random rnd = new Random(int.Parse(seed));

            byte pickedLayer = (byte)rnd.Next(0, layers.Count);
            for (int i = 0; i < layers.Count; i++)
            {
                if (!layers[i]) continue;

                if (i == pickedLayer)
                    layers[i].SetActive(true);
                else
                    entity_room_window.Destroy(layers[i]);
            }
        }
    }
}