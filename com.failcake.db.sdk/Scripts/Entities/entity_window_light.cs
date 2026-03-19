#region

using System.Collections.Generic;
using SaintsField.Playa;
using UnityEngine;

#endregion

namespace HyenaQuest
{
    public class entity_window_light : MonoBehaviour
    {
        [LayoutStart("GameObjects", ELayout.Background | ELayout.FoldoutBox)]
        public List<Light> lights = new List<Light>();

        public bool isOutside;

        public void Awake() {
            if (this.lights.Count == 0) throw new UnityException("Missing 'lights' references");
        }

        public void SetColor(Color cl, float outsideIntensity) {
            foreach (Light l in this.lights)
            {
                if (!l) continue;

                l.color = cl;
                if (this.isOutside) l.intensity = outsideIntensity;
            }
        }
    }
}