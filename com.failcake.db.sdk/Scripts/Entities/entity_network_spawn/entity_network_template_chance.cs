#region

using SaintsField.Playa;
using UnityEngine;
using Random = UnityEngine.Random;

#endregion

namespace HyenaQuest
{
    public class entity_network_template_chance : entity_network_template_base
    {
        [LayoutStart("Settings", ELayout.Background | ELayout.TitleOut), Range(0, 1)]
        public float chance;

        public override bool CanSpawn() {
            return Mathf.Approximately(this.chance, 1) || Random.value < Mathf.Clamp01(this.chance);
        }
    }
}