#region

using System.Collections.Generic;
using UnityEngine;

#endregion

namespace HyenaQuest
{
    public class entity_led_controller : MonoBehaviour
    {
        #region PUBLIC

        [Header("LEDS")]
        public List<entity_led> LEDS = new List<entity_led>();

        #endregion

        public void SetActive(int active) {
            for (int i = 0; i < this.LEDS.Count; i++) this.LEDS[i].SetActive(i < active);
        }
    }
}