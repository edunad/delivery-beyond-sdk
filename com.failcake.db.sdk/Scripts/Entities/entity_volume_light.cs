#region

using SaintsField.Playa;
using UnityEngine;
using VolumetricFogAndMist2;
using VolumetricLights;

#endregion

namespace HyenaQuest
{
    public class entity_volume_light : MonoBehaviour
    {
        [LayoutStart("GameObjects", ELayout.Background | ELayout.FoldoutBox)]
        public VolumetricFog fogVolume;

        public VolumetricLight lightVolume;

        public void Awake() {
            if (!this.fogVolume && !this.lightVolume) throw new UnityException("Requires either a 'fogVolume' or a 'lightVolume'!");
        }

        public void SetColor(Color cl) {
            if (this.fogVolume) this.fogVolume.settings.albedo = cl;
            if (this.lightVolume) this.lightVolume.lightComp.color = cl;
        }
    }
}